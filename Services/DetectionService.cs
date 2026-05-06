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

            void AddViolation(int accountId, string ruleName, string desc, int score)
            {
                if (!accountViolations.ContainsKey(accountId))
                {
                    accountViolations[accountId] = new List<AlertReason>();
                }
                
                if (!accountViolations[accountId].Any(r => r.RuleName == ruleName))
                {
                    accountViolations[accountId].Add(new AlertReason { RuleName = ruleName, Description = desc, Score = score });
                }
            }

            // Rule 1: High Amount Transaction
            var highAmountThreshold = 10000m;
            foreach (var tx in transactions)
            {
                if (tx.Amount > highAmountThreshold)
                {
                    AddViolation(tx.FromAccountId, "High Amount", $"Transaction of {tx.Amount} exceeds threshold {highAmountThreshold}", 50);
                }
            }

            // Rule 2: Multiple Transactions in Short Time
            var groupsFrom = transactions.GroupBy(t => t.FromAccountId);
            foreach (var group in groupsFrom)
            {
                var sorted = group.OrderBy(t => t.TransactionDate).ToList();
                for (int i = 0; i < sorted.Count - 4; i++)
                {
                    if ((sorted[i + 4].TransactionDate - sorted[i].TransactionDate).TotalHours <= 24)
                    {
                        AddViolation(group.Key, "Rapid Transactions", "More than 5 transactions in 24 hours", 40);
                        break;
                    }
                }
            }

            // Rule 3: Many-to-One Pattern
            var groupsTo = transactions.GroupBy(t => t.ToAccountId);
            foreach (var group in groupsTo)
            {
                var uniqueSenders = group.Select(t => t.FromAccountId).Distinct().Count();
                if (uniqueSenders >= 3)
                {
                    AddViolation(group.Key, "Many-to-One", $"Received transactions from {uniqueSenders} different accounts", 60);
                }
            }

            // Rule 4 & 5: Graph Patterns (Chains and Cycles)
            var cycles = _graphService.FindCycles(transactions);
            foreach (var cycle in cycles)
            {
                foreach (var accountId in cycle)
                {
                    AddViolation(accountId, "Circular Pattern", $"Account involved in a circular transaction path", 80);
                }
            }

            var chains = _graphService.FindChains(transactions);
            foreach (var chain in chains)
            {
                foreach (var accountId in chain)
                {
                    AddViolation(accountId, "Chain Pattern", $"Account involved in a chain transaction path", 70);
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
                {
                    AddViolation(group.Key, "ML Unavailable", "ML model unavailable, rule-based detection only", 0);
                }
                else if (mlTriggered)
                {
                    AddViolation(group.Key, "ML Model", "Machine learning model flagged this transaction as suspicious", 20);
                }
            }

            var alertsCreated = new List<Alert>();

            foreach (var kvp in accountViolations)
            {
                var accountId = kvp.Key;
                var reasons = kvp.Value;

                int finalScore = reasons.Sum(r => r.Score);
                if (finalScore > 100) finalScore = 100;

                if (finalScore >= 50)
                {
                    string riskLevel = finalScore >= 75 ? "High" : "Medium";

                    var alert = new Alert
                    {
                        AccountId = accountId,
                        RiskScore = finalScore,
                        RiskLevel = riskLevel,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        AlertReasons = reasons
                    };

                    _context.Alerts.Add(alert);
                    alertsCreated.Add(alert);
                }
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
