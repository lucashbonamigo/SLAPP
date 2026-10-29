using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoloLeveling.System.Api.Contracts;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Domain;

namespace SoloLeveling.System.Api.Controllers;

/// <summary>CRUD dos objetivos do jogador (a direção que a IA usa para gerar tarefas).</summary>
[ApiController]
[Authorize]
[Route("api/v1/goals")]
public class GoalsController : ControllerBase
{
    private readonly SystemDbContext _db;

    public GoalsController(SystemDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GoalResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var items = await _db.Goals
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.Status == GoalStatus.ACTIVE)
            .ThenBy(g => g.Title)
            .Select(g => ToResponse(g))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var g = await _db.Goals.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        return g is null ? NotFound() : Ok(ToResponse(g));
    }

    [HttpPost]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] GoalRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (!TryParse(request, out var attribute, out var category, out var status, out var startDate, out var targetDate, out var error))
        {
            return BadRequest(new { message = error });
        }

        var entity = new Goal
        {
            UserId = userId.Value,
            Title = request.Title.Trim(),
            Description = Trim(request.Description),
            AttributeType = attribute,
            Category = category,
            TargetMetric = Trim(request.TargetMetric),
            ContentBacklog = Trim(request.ContentBacklog),
            Status = status,
            ProgressNotes = Trim(request.ProgressNotes),
            StartDate = startDate ?? DateOnly.FromDateTime(DateTime.Now),
            TargetDate = targetDate,
            CreatedAt = DateTime.UtcNow
        };

        _db.Goals.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToResponse(entity));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(GoalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] GoalRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var entity = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct);
        if (entity is null) return NotFound();

        if (!TryParse(request, out var attribute, out var category, out var status, out var startDate, out var targetDate, out var error))
        {
            return BadRequest(new { message = error });
        }

        entity.Title = request.Title.Trim();
        entity.Description = Trim(request.Description);
        entity.AttributeType = attribute;
        entity.Category = category;
        entity.TargetMetric = Trim(request.TargetMetric);
        entity.ContentBacklog = Trim(request.ContentBacklog);
        entity.Status = status;
        entity.ProgressNotes = Trim(request.ProgressNotes);
        if (startDate.HasValue) entity.StartDate = startDate.Value;
        entity.TargetDate = targetDate;

        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(entity));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var entity = await _db.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct);
        if (entity is null) return NotFound();

        _db.Goals.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static bool TryParse(
        GoalRequest request,
        out AttributeType attribute,
        out GoalCategory category,
        out GoalStatus status,
        out DateOnly? startDate,
        out DateOnly? targetDate,
        out string error)
    {
        attribute = default;
        category = default;
        status = default;
        startDate = null;
        targetDate = null;
        error = string.Empty;

        if (!Enum.TryParse(request.AttributeType, true, out attribute))
        {
            error = $"Atributo inválido: '{request.AttributeType}'. Use STR, INT, WIS ou VIT.";
            return false;
        }
        if (!Enum.TryParse(request.Category, true, out category))
        {
            error = $"Categoria inválida: '{request.Category}'. Use RUNNING, STUDY, READING ou GENERIC.";
            return false;
        }
        if (!Enum.TryParse(request.Status, true, out status))
        {
            error = $"Status inválido: '{request.Status}'. Use ACTIVE, COMPLETED ou PAUSED.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.StartDate))
        {
            if (!DateOnly.TryParse(request.StartDate, CultureInfo.InvariantCulture, out var sd))
            {
                error = $"Data de início inválida: '{request.StartDate}'. Use yyyy-MM-dd.";
                return false;
            }
            startDate = sd;
        }

        if (!string.IsNullOrWhiteSpace(request.TargetDate))
        {
            if (!DateOnly.TryParse(request.TargetDate, CultureInfo.InvariantCulture, out var td))
            {
                error = $"Prazo inválido: '{request.TargetDate}'. Use yyyy-MM-dd.";
                return false;
            }
            targetDate = td;
        }

        return true;
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static GoalResponse ToResponse(Goal g) => new(
        g.Id,
        g.Title,
        g.Description,
        g.AttributeType.ToString(),
        g.Category.ToString(),
        g.TargetMetric,
        g.ContentBacklog,
        g.Status.ToString(),
        g.ProgressNotes,
        g.StartDate.ToString("yyyy-MM-dd"),
        g.TargetDate?.ToString("yyyy-MM-dd"));

    private int? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }
}
