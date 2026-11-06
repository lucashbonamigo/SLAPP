using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoloLeveling.System.Api.Contracts;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Domain;

namespace SoloLeveling.System.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reflections")]
public class ReflectionsController : ControllerBase
{
    private readonly SystemDbContext _db;

    public ReflectionsController(SystemDbContext db)
    {
        _db = db;
    }

    /// <summary>Retorna a reflexão de hoje, se já registrada.</summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(ReflectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var reflection = await _db.DailyReflections.AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ReflectionDate == today, ct);

        if (reflection is null)
        {
            return NoContent();
        }

        return Ok(ToResponse(reflection));
    }

    /// <summary>Lista as reflexões recentes (padrão 30 dias), mais recentes primeiro.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReflectionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] int days = 30, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        days = Math.Clamp(days, 1, 365);
        var since = DateOnly.FromDateTime(DateTime.Now).AddDays(-days);

        var items = await _db.DailyReflections.AsNoTracking()
            .Where(r => r.UserId == userId && r.ReflectionDate >= since)
            .OrderByDescending(r => r.ReflectionDate)
            .Select(r => new ReflectionResponse(
                r.Id,
                r.ReflectionDate.ToString("yyyy-MM-dd"),
                r.AutopilotText,
                r.IdealText,
                r.MentorFeedback))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>
    /// Registra ou atualiza a reflexão noturna de hoje (upsert). O feedback do
    /// mentor é preenchido depois, pelo Worker, na avaliação noturna.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReflectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upsert([FromBody] ReflectionRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var reflection = await _db.DailyReflections
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ReflectionDate == today, ct);

        if (reflection is null)
        {
            reflection = new DailyReflection
            {
                UserId = userId.Value,
                ReflectionDate = today,
                AutopilotText = request.AutopilotText.Trim(),
                IdealText = string.IsNullOrWhiteSpace(request.IdealText) ? null : request.IdealText.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.DailyReflections.Add(reflection);
        }
        else
        {
            reflection.AutopilotText = request.AutopilotText.Trim();
            reflection.IdealText = string.IsNullOrWhiteSpace(request.IdealText) ? null : request.IdealText.Trim();
            reflection.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(reflection));
    }

    private static ReflectionResponse ToResponse(DailyReflection r) => new(
        r.Id,
        r.ReflectionDate.ToString("yyyy-MM-dd"),
        r.AutopilotText,
        r.IdealText,
        r.MentorFeedback);

    private int? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }
}
