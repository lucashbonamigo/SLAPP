using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoloLeveling.System.Api.Contracts;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Domain;

namespace SoloLeveling.System.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/briefings")]
public class BriefingsController : ControllerBase
{
    private readonly SystemDbContext _db;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

        return Ok(ToResponse(briefing));
    }

    /// <summary>Lista os briefings recentes (padrão 14 dias), mais recentes primeiro.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BriefingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] int days = 14, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 180);
        var since = DateOnly.FromDateTime(DateTime.Now).AddDays(-days);

        var briefings = await _db.DailyBriefings.AsNoTracking()
            .Where(b => b.BriefingDate >= since)
            .OrderByDescending(b => b.BriefingDate)
            .ToListAsync(ct);

        return Ok(briefings.Select(ToResponse));
    }

    private static BriefingResponse ToResponse(DailyBriefing b) => new(
        b.BriefingDate.ToString("yyyy-MM-dd"),
        b.SummaryMd,
        b.SourceCount,
        ParseItems(b.ItemsJson));

    private static IReadOnlyList<BriefingItemDto> ParseItems(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<BriefingItemDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<BriefingItemDto>>(json, JsonOpts)
                ?? new List<BriefingItemDto>();
        }
        catch (JsonException)
        {
            return Array.Empty<BriefingItemDto>();
        }
    }
}
