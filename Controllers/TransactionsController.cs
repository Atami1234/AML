using AmlDetectionApi.Data;
using AmlDetectionApi.DTOs;
using AmlDetectionApi.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
        [RequestSizeLimit(52428800)]
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        public async Task<IActionResult> UploadCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a CSV file.");

            if (!System.IO.Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file format. Only .csv is allowed.");

            if (file.Length > 52428800)
                return BadRequest(new { error = "File size exceeds the 50MB limit." });

            // ── Detect CSV format by peeking at the header row ──────────────────
            string? firstLine;
            using (var peekReader = new StreamReader(file.OpenReadStream(), leaveOpen: false))
            {
                firstLine = await peekReader.ReadLineAsync();
            }

            if (firstLine == null)
                return BadRequest("CSV file is empty.");

            bool isFormat2 = firstLine.Contains("Timestamp", StringComparison.OrdinalIgnoreCase);
            bool isFormat1 = firstLine.Contains("FromAccountNumber", StringComparison.OrdinalIgnoreCase);

            if (!isFormat2 && !isFormat1)
                return BadRequest(new { error = "Unrecognized CSV format. Expected 'FromAccountNumber' or 'Timestamp' in header." });

            // ── Parse ────────────────────────────────────────────────────────────
            var sw = Stopwatch.StartNew();
            var skippedRows = new List<object>();
            int importedCount = 0;
            int skippedCount  = 0;
            int totalRowsInFile = 0;

            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,          // tolerate missing optional columns
                BadDataFound = null                // skip bad data gracefully
            };

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, csvConfig);

            var transactionsToAdd = new List<Transaction>();
            var accountCache = await _context.Accounts.ToDictionaryAsync(a => a.AccountNumber);
            int accountsCacheSizeAtStart = accountCache.Count;  // snapshot before import
            int rowNumber = 1;

            if (isFormat1)
            {
                // ── FORMAT 1 ─────────────────────────────────────────────────────
                var dtOptions = new CsvHelper.TypeConversion.TypeConverterOptions
                {
                    Formats = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss" }
                };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dtOptions);

                await csv.ReadAsync();
                csv.ReadHeader();

                try { csv.ValidateHeader<TransactionCsvDto>(); }
                catch (HeaderValidationException)
                {
                    return BadRequest("Missing required columns in CSV header.");
                }

                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    totalRowsInFile++;
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

                        var (fromAcc, toAcc) = EnsureAccounts(accountCache, record.FromAccountNumber, record.ToAccountNumber);

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
                    catch (Exception)
                    {
                        skippedCount++;
                        skippedRows.Add(new { rowNumber, reason = "Invalid row format or missing data" });
                    }
                }
            }
            else
            {
                // ── FORMAT 2 ─────────────────────────────────────────────────────
                await csv.ReadAsync();
                csv.ReadHeader();

                // Supported timestamp formats in HI-Small dataset
                var dtOptions = new CsvHelper.TypeConversion.TypeConverterOptions
                {
                    Formats = new[]
                    {
                        "yyyy/MM/dd HH:mm",
                        "yyyy-MM-dd HH:mm:ss",
                        "yyyy-MM-ddTHH:mm:ss",
                        "M/d/yyyy H:mm"
                    }
                };
                csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dtOptions);

                while (await csv.ReadAsync())
                {
                    rowNumber++;
                    totalRowsInFile++;
                    try
                    {
                        var record = csv.GetRecord<TransactionCsvFormat2Dto>();
                        if (record == null)
                        {
                            skippedCount++;
                            skippedRows.Add(new { rowNumber, reason = "Empty or invalid record" });
                            continue;
                        }

                        // Map Format 2 → internal model fields
                        string fromAccountNumber = record.Account?.Trim() ?? string.Empty;
                        string toAccountNumber = record.Account1?.Trim() ?? string.Empty;
                        decimal amount = record.AmountPaid;
                        string currency = record.PaymentCurrency?.Trim() ?? string.Empty;
                        string channel = record.PaymentFormat?.Trim() ?? string.Empty;

                        // Parse timestamp — stored as string in Format 2
                        if (!DateTime.TryParseExact(
                                record.Timestamp,
                                new[] { "yyyy/MM/dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss", "M/d/yyyy H:mm" },
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out DateTime transactionDate))
                        {
                            // Try general parsing as fallback
                            if (!DateTime.TryParse(record.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out transactionDate))
                            {
                                skippedCount++;
                                skippedRows.Add(new { rowNumber, reason = $"Could not parse Timestamp: '{record.Timestamp}'" });
                                continue;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(fromAccountNumber) || string.IsNullOrWhiteSpace(toAccountNumber))
                        {
                            skippedCount++;
                            skippedRows.Add(new { rowNumber, reason = "Missing Account or Account.1" });
                            continue;
                        }

                        if (amount <= 0)
                        {
                            skippedCount++;
                            skippedRows.Add(new { rowNumber, reason = "Invalid Amount Paid" });
                            continue;
                        }

                        var (fromAcc, toAcc) = EnsureAccounts(accountCache, fromAccountNumber, toAccountNumber);

                        transactionsToAdd.Add(new Transaction
                        {
                            FromAccount = fromAcc,
                            ToAccount = toAcc,
                            Amount = amount,
                            Currency = currency,
                            TransactionDate = transactionDate,
                            Channel = channel
                        });

                        importedCount++;
                    }
                    catch (Exception)
                    {
                        skippedCount++;
                        skippedRows.Add(new { rowNumber, reason = "Invalid row format or missing data" });
                    }
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

            sw.Stop();

            // Count how many new accounts (and matching customers) were created during this import
            int newAccountsCreated  = accountCache.Count - accountsCacheSizeAtStart;
            int newCustomersCreated = newAccountsCreated;  // one customer per auto-created account

            return Ok(new
            {
                importedCount,
                skippedCount,
                totalRowsInFile,
                newAccountsCreated,
                newCustomersCreated,
                timeTakenMs = sw.ElapsedMilliseconds,
                skippedRows
            });
        }

        // ── Helper: create accounts on-the-fly if they don't exist ──────────────
        private (Account fromAcc, Account toAcc) EnsureAccounts(
            Dictionary<string, Account> cache,
            string fromNumber,
            string toNumber)
        {
            if (!cache.TryGetValue(fromNumber, out var fromAcc))
            {
                var customer = new Customer
                {
                    FullName = "Dummy " + fromNumber,
                    NationalId = "AUTO-" + Guid.NewGuid().ToString()[..8],
                    RiskLevel = "Low"
                };
                _context.Customers.Add(customer);
                fromAcc = new Account { AccountNumber = fromNumber, Customer = customer, AccountType = "Checking" };
                _context.Accounts.Add(fromAcc);
                cache[fromNumber] = fromAcc;
            }

            if (!cache.TryGetValue(toNumber, out var toAcc))
            {
                var customer = new Customer
                {
                    FullName = "Dummy " + toNumber,
                    NationalId = "AUTO-" + Guid.NewGuid().ToString()[..8],
                    RiskLevel = "Low"
                };
                _context.Customers.Add(customer);
                toAcc = new Account { AccountNumber = toNumber, Customer = customer, AccountType = "Checking" };
                _context.Accounts.Add(toAcc);
                cache[toNumber] = toAcc;
            }

            return (fromAcc, toAcc);
        }
    }
}
