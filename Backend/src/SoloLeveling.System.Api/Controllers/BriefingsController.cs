using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoloLeveling.System.Api.Contracts;
using SoloLeveling.System.Core.Data;

namespace SoloLeveling.System.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/briefings")]
public class BriefingsController : ControllerBase
{
    private readonly SystemDbContext _db;

    public BriefingsController(SystemDbContext db)
    {
        _db = db;
    }

    /// <summary>Retorna o briefing de hoje, se já gerado.</summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(BriefingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var briefing = await _db.DailyBriefings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.BriefingDate == today, ct);

        if (briefing is null)
        {
            return NoContent();
        }

        return Ok(new BriefingResponse(
            briefing.BriefingDate.ToString("yyyy-MM-dd"),
            briefing.SummaryMd,
            briefing.SourceCount));
    }

    /// <summary>Lista os briefings recentes (padrão 14 dias), mais recentes primeiro.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BriefingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] int days = 14, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 180);
        var since = DateOnly.FromDateTime(DateTime.Now).AddDays(-days);

        var items = await _db.DailyBriefings.AsNoTracking()
            .Where(b => b.BriefingDate >= since)
            .OrderByDescending(b => b.BriefingDate)
            .Select(b => new BriefingResponse(
                b.BriefingDate.ToString("yyyy-MM-dd"),
                b.SummaryMd,
                b.SourceCount))
            .ToListAsync(ct);

        return Ok(items);
    }
}
