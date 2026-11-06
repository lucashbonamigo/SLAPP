using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoloLeveling.System.Api.Contracts;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Services;

namespace SoloLeveling.System.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/player")]
public class PlayerController : ControllerBase
{
    private readonly SystemDbContext _db;

    public PlayerController(SystemDbContext db)
    {
        _db = db;
    }

    /// <summary>Retorna a ficha atualizada do personagem.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(PlayerStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return NotFound(new { message = "Jogador não encontrado." });
        }

        var nextLevelXp = user.Level * LevelingService.XpPerLevel;
        var xpToNext = Math.Max(0, nextLevelXp - user.CurrentXp);

        var response = new PlayerStatusResponse(
            user.Id,
            user.Username,
            user.Level,
            user.CurrentXp,
            xpToNext,
            new AttributesDto(user.StatStr, user.StatInt, user.StatWis, user.StatVit),
            user.DailyDeadline.ToString(@"hh\:mm\:ss"));

        return Ok(response);
    }

    /// <summary>
    /// Retorna os cinco traços Big Five atuais e o histórico diário (para o radar
    /// de evolução). Use ?days=N para limitar a janela do histórico (padrão 90).
    /// </summary>
    [HttpGet("personality")]
    [ProducesResponseType(typeof(PersonalityResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPersonality([FromQuery] int days = 90, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        days = Math.Clamp(days, 1, 365);
        var since = DateOnly.FromDateTime(DateTime.Now).AddDays(-days);

        var traits = await _db.PersonalityTraits.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.TraitCode)
            .Select(t => new TraitDto(
                t.TraitCode.ToString(),
                t.Value,
                t.UpdatedAt.ToString("yyyy-MM-dd")))
            .ToListAsync(ct);

        var history = await _db.PersonalityTraitHistory.AsNoTracking()
            .Where(h => h.UserId == userId && h.RecordedAt >= since)
            .OrderBy(h => h.RecordedAt)
            .Select(h => new TraitHistoryPointDto(
                h.TraitCode.ToString(),
                h.Value,
                h.Delta,
                h.RecordedAt.ToString("yyyy-MM-dd")))
            .ToListAsync(ct);

        return Ok(new PersonalityResponse(traits, history));
    }

    /// <summary>
    /// Retorna as séries temporais das métricas numéricas das missões aprovadas,
    /// agrupadas por nome de métrica. Filtre por objetivo com ?goalId=N e limite
    /// a janela com ?days=N (padrão 90).
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(MetricsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(
        [FromQuery] int? goalId = null,
        [FromQuery] int days = 90,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        days = Math.Clamp(days, 1, 365);
        var since = DateOnly.FromDateTime(DateTime.Now).AddDays(-days);

        var query = _db.QuestMetrics.AsNoTracking()
            .Where(m => m.UserId == userId && m.RecordedAt >= since);

        if (goalId is not null)
        {
            query = query.Where(m => m.GoalId == goalId);
        }

        var rows = await query
            .OrderBy(m => m.RecordedAt)
            .Select(m => new { m.MetricName, m.Unit, m.RecordedAt, m.MetricValue })
            .ToListAsync(ct);

        var series = rows
            .GroupBy(r => r.MetricName)
            .Select(g => new MetricSeriesDto(
                g.Key,
                g.Select(x => x.Unit).FirstOrDefault(u => !string.IsNullOrWhiteSpace(u)),
                g.Select(x => new MetricPointDto(x.RecordedAt.ToString("yyyy-MM-dd"), x.MetricValue)).ToList()))
            .OrderBy(s => s.MetricName)
            .ToList();

        return Ok(new MetricsResponse(series));
    }

    private int? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }
}
