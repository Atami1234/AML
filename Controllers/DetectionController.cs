using System.Linq;
using System.Threading.Tasks;
using AmlDetectionApi.Data;
using AmlDetectionApi.DTOs;
using AmlDetectionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmlDetectionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DetectionController : ControllerBase
    {
        private readonly IDetectionService _detectionService;
        private readonly IBlockchainLogService _blockchainLogService;
        private readonly AmlDbContext _context;

        public DetectionController(
            IDetectionService detectionService,
            IBlockchainLogService blockchainLogService,
            AmlDbContext context)
        {
            _detectionService = detectionService;
            _blockchainLogService = blockchainLogService;
            _context = context;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunDetection()
        {
            try
            {
                var newAlerts = await _detectionService.RunDetectionAsync();

                foreach (var alert in newAlerts)
                {
                    // Create blockchain log for each newly generated alert
                    var log = await _blockchainLogService.CreateLogForAlertAsync(alert);
                    alert.BlockchainLog = log;
                }

                int totalTransactions = await _context.Transactions.CountAsync();
                int totalAlertsCreated = newAlerts.Count;
                int highRiskAlerts = newAlerts.Count(a => a.RiskLevel == "High");
                int mediumRiskAlerts = newAlerts.Count(a => a.RiskLevel == "Medium");

                var alertDtos = newAlerts.Select(a => new AlertResultDto
                {
                    AlertId = a.AlertId,
                    AccountId = a.AccountId,
                    AccountNumber = _context.Accounts.Where(acc => acc.AccountId == a.AccountId).Select(acc => acc.AccountNumber).FirstOrDefault() ?? string.Empty,
                    RiskScore = a.RiskScore,
                    RiskLevel = a.RiskLevel,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt,
                    BlockchainHash = a.BlockchainLog != null ? a.BlockchainLog.DataHash : string.Empty,
                    BlockReference = a.BlockchainLog != null ? a.BlockchainLog.BlockReference : string.Empty,
                    Reasons = a.AlertReasons.Select(r => new AlertReasonDto
                    {
                        RuleName = r.RuleName,
                        Description = r.Description,
                        Score = r.Score
                    }).ToList()
                }).ToList();

                var summary = new
                {
                    totalTransactions,
                    totalAlertsCreated,
                    highRiskAlerts,
                    mediumRiskAlerts,
                    alerts = alertDtos
                };

                return Ok(summary);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
