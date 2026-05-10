using AmlDetectionApi.Data;
using AmlDetectionApi.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmlDetectionApi.Services
{
    public interface IDetectionService
    {
        Task<List<Alert>> RunDetectionAsync();
    }

    public class DetectionService : IDetectionService
    {
        private readonly AmlDbContext _context;
        private readonly IGraphAnalysisService _graphService;
        private readonly IAiDetectionService _aiService;
        private readonly Microsoft.Extensions.Logging.ILogger<DetectionService> _logger;
        private readonly IBlockchainLogService _blockchainLogService;

        public DetectionService(AmlDbContext context, IGraphAnalysisService graphService, IAiDetectionService aiService, Microsoft.Extensions.Logging.ILogger<DetectionService> logger, IBlockchainLogService blockchainLogService)
        {
            _context = context;
            _graphService = graphService;
            _aiService = aiService;
            _logger = logger;
            _blockchainLogService = blockchainLogService;
        }

        public async Task<List<Alert>> RunDetectionAsync()
        {
            _logger.LogInformation("Detection run started.");

            var transactions = await _context.Transactions
                .Include(t => t.FromAccount)
                .Include(t => t.ToAccount)
                .ToListAsync();
                
            var accountViolations = new Dictionary<int, List<AlertReason>>();

            void AddViolation(int accountId, string ruleName, string desc, decimal score)
            {
                if (!accountViolations.ContainsKey(accountId))
                    accountViolations[accountId] = new List<AlertReason>();

                if (!accountViolations[accountId].Any(r => r.RuleName == ruleName))
                    accountViolations[accountId].Add(new AlertReason { RuleName = ruleName, Description = desc, Score = (int)score });
            }

            // Rule 1: High Amount Transaction — tiered by amount
            //   > 500,000 → 45 pts  |  > 100,000 → 30 pts  |  > 50,000 → 20 pts
            foreach (var tx in transactions)
            {
                decimal pts = tx.Amount switch
                {
                    > 500_000m => 45.0m,
                    > 100_000m => 30.0m,
                    > 50_000m  => 20.0m,
                    _          => 0.0m
                };
                if (pts > 0)
                    AddViolation(tx.FromAccountId, "High Amount",
                        $"Transaction of {tx.Amount:N2} exceeds high-value threshold", pts);
            }

            // Rule 2: Rapid Transactions — 5+ transactions within a SINGLE hour (25 pts)
            var groupsFrom = transactions.GroupBy(t => t.FromAccountId);
            foreach (var group in groupsFrom)
            {
                var sorted = group.OrderBy(t => t.TransactionDate).ToList();
                for (int i = 0; i < sorted.Count - 4; i++)
                {
                    if ((sorted[i + 4].TransactionDate - sorted[i].TransactionDate).TotalHours <= 1)
                    {
                        AddViolation(group.Key, "Rapid Transactions",
                            "5 or more transactions sent within a single hour", 25.0m);
                        break;
                    }
                }
            }

            // Rule 3: Funnel Account — 4+ unique senders (30 pts)
            var groupsTo = transactions.GroupBy(t => t.ToAccountId);
            foreach (var group in groupsTo)
            {
                var uniqueSenders = group.Select(t => t.FromAccountId).Distinct().Count();
                if (uniqueSenders >= 4)
                    AddViolation(group.Key, "Funnel Account",
                        $"Received funds from {uniqueSenders} different accounts", 30.0m);
            }

            // Rule 4: Circular Pattern (40 pts)
            var cycles = _graphService.FindCycles(transactions);
            foreach (var cycle in cycles)
                foreach (var accountId in cycle)
                    AddViolation(accountId, "Circular Pattern",
                        "Account participates in a circular transaction loop", 40.0m);

            // Rule 5: Chain Pattern (35 pts)
            var chains = _graphService.FindChains(transactions);
            foreach (var chain in chains)
                foreach (var accountId in chain)
                    AddViolation(accountId, "Chain Pattern",
                        "Account is a relay node in a layering chain", 35.0m);

            // Rule 6: Fast Money Movement — received then forwarded within 24 h (25 pts)
            var allAccountIds = transactions.Select(t => t.FromAccountId)
                .Concat(transactions.Select(t => t.ToAccountId)).Distinct();
            foreach (var accId in allAccountIds)
            {
                var sent     = transactions.Where(t => t.FromAccountId == accId).OrderBy(t => t.TransactionDate).ToList();
                var received = transactions.Where(t => t.ToAccountId   == accId).OrderBy(t => t.TransactionDate).ToList();
                if (sent.Count > 0 && received.Count > 0)
                {
                    bool fastMove = received.Any(r =>
                        sent.Any(s => s.TransactionDate > r.TransactionDate &&
                                      (s.TransactionDate - r.TransactionDate).TotalHours <= 24));
                    if (fastMove)
                        AddViolation(accId, "Fast Money Movement",
                            "Received funds were forwarded to another account within 24 hours", 25.0m);
                }
            }

            // ML Model Addition
            foreach (var group in groupsFrom)
            {
                bool mlTriggered = false;
                bool mlFailed = false;

                foreach (var tx in group)
                {
                    var prediction = await _aiService.PredictAsync(tx);
                    if (prediction == null)
                    {
                        mlFailed = true;
                        break; // Stop querying for this account if it's down
                    }
                    else if (prediction == true)
                    {
                        mlTriggered = true;
                        break;
                    }
                }

                if (mlFailed)
                    AddViolation(group.Key, "ML Unavailable", "ML model unavailable, rule-based detection only", 0.0m);
                else if (mlTriggered)
                    AddViolation(group.Key, "ML Model", "Machine learning model flagged this transaction as suspicious", 20.0m);
            }

            var alertsCreated = new List<Alert>();

            foreach (var kvp in accountViolations)
            {
                var accountId = kvp.Key;
                var reasons = kvp.Value;

                // Check if alert already exists for this account to prevent duplicates
                bool alertExists = await _context.Alerts.AnyAsync(a => a.AccountId == accountId);
                if (alertExists) continue;

                // Decimal scoring: sum individual rule contributions, cap at 100
                decimal finalScore = reasons.Sum(r => (decimal)r.Score);
                if (finalScore > 100m) finalScore = 100m;

                // Only generate alert if score >= 60
                if (finalScore < 60.0m) continue;

                string riskLevel = finalScore >= 85.7m ? "High" : "Medium";

                var alert = new Alert
                {
                    AccountId = accountId,
                    RiskScore = Math.Round(finalScore, 2),
                    RiskLevel = riskLevel,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    AlertReasons = reasons
                };

                _context.Alerts.Add(alert);
                alertsCreated.Add(alert);
            }

            if (alertsCreated.Any())
            {
                await _context.SaveChangesAsync();

                foreach (var alert in alertsCreated)
                {
                    await _blockchainLogService.CreateLogForAlertAsync(alert);
                }
            }

            _logger.LogInformation($"Detection run finished with {alertsCreated.Count} alerts created.");

            return alertsCreated;
        }
    }
}
