using AmlDetectionApi.Data;
using AmlDetectionApi.DTOs;
using AmlDetectionApi.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AmlDetectionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly AmlDbContext _context;
        private readonly Services.IBlockchainLogService _blockchainLogService;

        public TransactionsController(AmlDbContext context, Services.IBlockchainLogService blockchainLogService)
        {
            _context = context;
            _blockchainLogService = blockchainLogService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponseDto<Transaction>>> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var totalCount = await _context.Transactions.CountAsync();
            var items = await _context.Transactions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new PagedResponseDto<Transaction>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await _blockchainLogService.CreateLogForTransactionAsync(transaction);

            return CreatedAtAction(nameof(GetTransactions), new { id = transaction.TransactionId }, transaction);
        }

        [HttpPost("upload-csv")]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Please upload a CSV file.");
            if (!System.IO.Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file format. Only .csv is allowed.");
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("File size exceeds 5MB limit.");

            var skippedRows = new List<object>();
            int importedCount = 0;
            int skippedCount = 0;

            using (var reader = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true }))
            {
                var options = new CsvHelper.TypeConversion.TypeConverterOptions { Formats = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss" } };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);

                await csv.ReadAsync();
                csv.ReadHeader();
                try
                {
                    csv.ValidateHeader<TransactionCsvDto>();
                }
                catch (HeaderValidationException)
                {
                    return BadRequest("Missing required columns in CSV header.");
                }

                var transactionsToAdd = new List<Transaction>();
                var accountCache = await _context.Accounts.ToDictionaryAsync(a => a.AccountNumber);
                int rowNumber = 1;

                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    try
                    {
                        var record = csv.GetRecord<TransactionCsvDto>();
                        if (record == null)
                        {
                            skippedCount++;
                            skippedRows.Add(new { rowNumber, reason = "Empty or invalid record" });
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(record.FromAccountNumber) || string.IsNullOrWhiteSpace(record.ToAccountNumber))
                        {
                            skippedCount++;
                            skippedRows.Add(new { rowNumber, reason = "Missing From or To account number" });
                            continue;
                        }

                        if (record.Amount <= 0)
                        {
                            skippedCount++;
                            skippedRows.Add(new { rowNumber, reason = "Invalid amount" });
                            continue;
                        }

                        if (!accountCache.TryGetValue(record.FromAccountNumber, out var fromAcc))
                        {
                            var customer = new Customer 
                            { 
                                FullName = "Dummy " + record.FromAccountNumber, 
                                NationalId = "AUTO-" + Guid.NewGuid().ToString().Substring(0, 8),
                                RiskLevel = "Low"
                            };
                            _context.Customers.Add(customer);

                            fromAcc = new Account 
                            { 
                                AccountNumber = record.FromAccountNumber, 
                                Customer = customer, 
                                AccountType = "Checking" 
                            };
                            _context.Accounts.Add(fromAcc);
                            accountCache[record.FromAccountNumber] = fromAcc;
                        }

                        if (!accountCache.TryGetValue(record.ToAccountNumber, out var toAcc))
                        {
                            var customer = new Customer 
                            { 
                                FullName = "Dummy " + record.ToAccountNumber, 
                                NationalId = "AUTO-" + Guid.NewGuid().ToString().Substring(0, 8),
                                RiskLevel = "Low"
                            };
                            _context.Customers.Add(customer);

                            toAcc = new Account 
                            { 
                                AccountNumber = record.ToAccountNumber, 
                                Customer = customer, 
                                AccountType = "Checking" 
                            };
                            _context.Accounts.Add(toAcc);
                            accountCache[record.ToAccountNumber] = toAcc;
                        }

                        transactionsToAdd.Add(new Transaction
                        {
                            FromAccount = fromAcc,
                            ToAccount = toAcc,
                            Amount = record.Amount,
                            Currency = record.Currency,
                            TransactionDate = record.TransactionDate,
                            Channel = record.Channel
                        });
                        
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        skippedCount++;
                        skippedRows.Add(new { rowNumber, reason = "Invalid row format or missing data" });
                    }
                }

                if (transactionsToAdd.Any())
                {
                    _context.Transactions.AddRange(transactionsToAdd);
                    await _context.SaveChangesAsync();

                    foreach (var tx in transactionsToAdd)
                    {
                        await _blockchainLogService.CreateLogForTransactionAsync(tx);
                    }
                }

                return Ok(new { importedCount, skippedCount, skippedRows });
            }
        }
    }
}
