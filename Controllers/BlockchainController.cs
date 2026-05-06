using AmlDetectionApi.Data;
using AmlDetectionApi.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AmlDetectionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlockchainController : ControllerBase
    {
        private readonly AmlDbContext _context;
        private readonly Microsoft.Extensions.Logging.ILogger<BlockchainController> _logger;

        public BlockchainController(AmlDbContext context, Microsoft.Extensions.Logging.ILogger<BlockchainController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("verify-transaction/{transactionId}")]
        public async Task<IActionResult> VerifyTransaction(int transactionId)
        {
            var transaction = await _context.Transactions.FirstOrDefaultAsync(t => t.TransactionId == transactionId);
            if (transaction == null) return NotFound("Transaction not found.");

            var log = await _context.BlockchainLogs
                .FirstOrDefaultAsync(l => l.TransactionId == transactionId && l.LogType == "Transaction");

            if (log == null) return NotFound("Blockchain log for this transaction not found.");

            string inputString = HashHelper.BuildTransactionInputString(transaction);
            
            _logger.LogWarning($"[DEBUG HASH VERIFYING] Data String: {inputString}");
            
            string computedHash = HashHelper.ComputeSha256Hash(inputString);
            bool isValid = computedHash == log.DataHash;

            if (!isValid)
            {
                _logger.LogWarning($"Hash verification failed for Transaction {transactionId}. Stored: {log.DataHash}, Computed: {computedHash}");
            }

            return Ok(new
            {
                isValid = isValid,
                storedHash = log.DataHash,
                computedHash = computedHash,
                logType = log.LogType,
                message = isValid ? "Hash is valid" : "Hash mismatch! Data might have been tampered with."
            });
        }

        [HttpGet("verify-alert/{alertId}")]
        public async Task<IActionResult> VerifyAlert(int alertId)
        {
            var alert = await _context.Alerts.FirstOrDefaultAsync(a => a.AlertId == alertId);
            if (alert == null) return NotFound("Alert not found.");

            var log = await _context.BlockchainLogs
                .FirstOrDefaultAsync(l => l.AlertId == alertId && l.LogType == "Alert");

            if (log == null) return NotFound("Blockchain log for this alert not found.");

            string inputString = HashHelper.BuildAlertInputString(alert);
            string computedHash = HashHelper.ComputeSha256Hash(inputString);
            bool isValid = computedHash == log.DataHash;

            if (!isValid)
            {
                _logger.LogWarning($"Hash verification failed for Alert {alertId}. Stored: {log.DataHash}, Computed: {computedHash}");
            }

            return Ok(new
            {
                isValid = isValid,
                storedHash = log.DataHash,
                computedHash = computedHash,
                logType = log.LogType,
                message = isValid ? "Hash is valid" : "Hash mismatch! Data might have been tampered with."
            });
        }
    }
}
