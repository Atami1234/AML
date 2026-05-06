using System;
using System.Threading.Tasks;
using AmlDetectionApi.Data;
using AmlDetectionApi.Helpers;
using AmlDetectionApi.Models;

namespace AmlDetectionApi.Services
{
    public interface IBlockchainLogService
    {
        Task<BlockchainLog> CreateLogForAlertAsync(Alert alert);
        Task<BlockchainLog> CreateLogForTransactionAsync(Transaction transaction);
    }

    public class BlockchainLogService : IBlockchainLogService
    {
        private readonly AmlDbContext _context;
        private readonly Microsoft.Extensions.Logging.ILogger<BlockchainLogService> _logger;

        public BlockchainLogService(AmlDbContext context, Microsoft.Extensions.Logging.ILogger<BlockchainLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BlockchainLog> CreateLogForAlertAsync(Alert alert)
        {
            string inputString = HashHelper.BuildAlertInputString(alert);
            string hash = HashHelper.ComputeSha256Hash(inputString);

            var blockchainLog = new BlockchainLog
            {
                AlertId = alert.AlertId,
                LogType = "Alert",
                DataHash = hash,
                BlockReference = "BLOCK-" + DateTime.UtcNow.Ticks,
                CreatedAt = DateTime.UtcNow
            };

            _context.BlockchainLogs.Add(blockchainLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Blockchain hash created for Alert {alert.AlertId}");

            return blockchainLog;
        }

        public async Task<BlockchainLog> CreateLogForTransactionAsync(Transaction transaction)
        {
            string inputString = HashHelper.BuildTransactionInputString(transaction);
            
            _logger.LogWarning($"[DEBUG HASH SAVING] Data String: {inputString}");
            
            string hash = HashHelper.ComputeSha256Hash(inputString);

            var blockchainLog = new BlockchainLog
            {
                TransactionId = transaction.TransactionId,
                LogType = "Transaction",
                DataHash = hash,
                BlockReference = "BLOCK-" + DateTime.UtcNow.Ticks,
                CreatedAt = DateTime.UtcNow
            };

            _context.BlockchainLogs.Add(blockchainLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Blockchain hash created for Transaction {transaction.TransactionId}");

            return blockchainLog;
        }
    }
}
