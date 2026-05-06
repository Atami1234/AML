using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmlDetectionApi.Data;
using AmlDetectionApi.DTOs;
using AmlDetectionApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmlDetectionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly AmlDbContext _context;
        private readonly Services.IGraphAnalysisService _graphAnalysisService;

        public AccountsController(AmlDbContext context, Services.IGraphAnalysisService graphAnalysisService)
        {
            _context = context;
            _graphAnalysisService = graphAnalysisService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponseDto<Account>>> GetAccounts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var totalCount = await _context.Accounts.CountAsync();
            var items = await _context.Accounts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);

            return Ok(new PagedResponseDto<Account>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();
            return Ok(account);
        }

        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccount([FromBody] Account account)
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAccount), new { id = account.AccountId }, account);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] Account account)
        {
            if (id != account.AccountId) return BadRequest();

            _context.Entry(account).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Accounts.AnyAsync(e => e.AccountId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) return NotFound();

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("{id}/network")]
        public async Task<ActionResult<NetworkGraphDto>> GetAccountNetwork(int id)
        {
            var accountExists = await _context.Accounts.AnyAsync(a => a.AccountId == id);
            if (!accountExists) return NotFound();

            var transactions = await _context.Transactions
                .Where(t => t.FromAccountId == id || t.ToAccountId == id)
                .ToListAsync();

            var connectedAccountIds = transactions.Select(t => t.FromAccountId)
                .Union(transactions.Select(t => t.ToAccountId))
                .Distinct()
                .ToList();

            var accounts = await _context.Accounts
                .Include(a => a.Customer)
                .Where(a => connectedAccountIds.Contains(a.AccountId))
                .ToListAsync();

            var nodes = accounts.Select(a => new NetworkNodeDto
            {
                Id = a.AccountId,
                Label = a.AccountNumber,
                RiskLevel = a.Customer?.RiskLevel ?? "Low"
            }).ToList();

            var edges = transactions.Select(t => new NetworkEdgeDto
            {
                From = t.FromAccountId,
                To = t.ToAccountId,
                Amount = t.Amount,
                TransactionDate = t.TransactionDate
            }).ToList();

            return Ok(new NetworkGraphDto
            {
                Nodes = nodes,
                Edges = edges
            });
        }
        [HttpGet("{id}/metrics")]
        public async Task<ActionResult<AccountMetricsDto>> GetAccountMetrics(int id)
        {
            var accountExists = await _context.Accounts.AnyAsync(a => a.AccountId == id);
            if (!accountExists) return NotFound();

            var incomingTx = await _context.Transactions.Where(t => t.ToAccountId == id).ToListAsync();
            var outgoingTx = await _context.Transactions.Where(t => t.FromAccountId == id).ToListAsync();

            var allTxForGraph = await _context.Transactions.ToListAsync();
            var cycles = _graphAnalysisService.FindCycles(allTxForGraph);
            bool isCircular = cycles.Any(c => c.Contains(id));

            var uniqueSenders = incomingTx.Select(t => t.FromAccountId).Distinct().Count();
            var uniqueReceivers = outgoingTx.Select(t => t.ToAccountId).Distinct().Count();

            return Ok(new AccountMetricsDto
            {
                IncomingCount = incomingTx.Count,
                OutgoingCount = outgoingTx.Count,
                UniqueSenders = uniqueSenders,
                UniqueReceivers = uniqueReceivers,
                IsFunnelAccount = uniqueSenders >= 4,
                IsCircularPatternDetected = isCircular
            });
        }
    }
}
