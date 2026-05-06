using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmlDetectionApi.Data;
using AmlDetectionApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmlDetectionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly AmlDbContext _context;

        public AlertsController(AmlDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponseDto<AlertResultDto>>> GetAlerts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Alerts;
            var totalCount = await query.CountAsync();
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .Include(a => a.Account)
                .Include(a => a.AlertReasons)
                .Include(a => a.BlockchainLog)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AlertResultDto
                {
                    AlertId = a.AlertId,
                    AccountId = a.AccountId,
                    AccountNumber = a.Account != null ? a.Account.AccountNumber : string.Empty,
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
                })
                .ToListAsync();

            return Ok(new PagedResponseDto<AlertResultDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AlertResultDto>> GetAlert(int id)
        {
            var alert = await _context.Alerts
                .Include(a => a.Account)
                .Include(a => a.AlertReasons)
                .Include(a => a.BlockchainLog)
                .Where(a => a.AlertId == id)
                .Select(a => new AlertResultDto
                {
                    AlertId = a.AlertId,
                    AccountId = a.AccountId,
                    AccountNumber = a.Account != null ? a.Account.AccountNumber : string.Empty,
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
                })
                .FirstOrDefaultAsync();

            if (alert == null) return NotFound();
            return Ok(alert);
        }

        [HttpGet("high-risk")]
        public async Task<ActionResult<IEnumerable<AlertResultDto>>> GetHighRiskAlerts()
        {
            var alerts = await _context.Alerts
                .Include(a => a.Account)
                .Include(a => a.AlertReasons)
                .Include(a => a.BlockchainLog)
                .Where(a => a.RiskLevel == "High")
                .Select(a => new AlertResultDto
                {
                    AlertId = a.AlertId,
                    AccountId = a.AccountId,
                    AccountNumber = a.Account != null ? a.Account.AccountNumber : string.Empty,
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
                })
                .ToListAsync();

            return Ok(alerts);
        }
    }
}
