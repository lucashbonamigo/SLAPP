using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoloLeveling.System.Api.Contracts;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Domain;

namespace SoloLeveling.System.Api.Controllers;

/// <summary>CRUD do catálogo de punições que o Sistema sorteia nas falhas.</summary>
[ApiController]
[Authorize]
[Route("api/v1/punishments")]
public class PunishmentsController : ControllerBase
{
    private readonly SystemDbContext _db;

    public PunishmentsController(SystemDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PunishmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await _db.Punishments
            .AsNoTracking()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Title)
            .Select(p => ToResponse(p))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PunishmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var p = await _db.Punishments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? NotFound() : Ok(ToResponse(p));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PunishmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] PunishmentRequest request, CancellationToken ct)
    {
        if (!TryParseEnums(request, out var category, out var proof, out var attribute, out var error))
        {
            return BadRequest(new { message = error });
        }

        var codeExists = await _db.Punishments.AnyAsync(p => p.Code == request.Code, ct);
        if (codeExists)
        {
            return Conflict(new { message = $"Já existe uma punição com o código '{request.Code}'." });
        }

        var entity = new Punishment
        {
            Code = request.Code.Trim(),
            Category = category,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ProofType = proof,
            EstimatedMinutes = request.EstimatedMinutes,
            XpReward = request.XpReward,
            AttributeType = attribute,
            Weight = request.Weight,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Punishments.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToResponse(entity));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PunishmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] PunishmentRequest request, CancellationToken ct)
    {
        var entity = await _db.Punishments.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        if (!TryParseEnums(request, out var category, out var proof, out var attribute, out var error))
        {
            return BadRequest(new { message = error });
        }

        var codeTaken = await _db.Punishments.AnyAsync(p => p.Code == request.Code && p.Id != id, ct);
        if (codeTaken)
        {
            return Conflict(new { message = $"Já existe outra punição com o código '{request.Code}'." });
        }

        entity.Code = request.Code.Trim();
        entity.Category = category;
        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.ProofType = proof;
        entity.EstimatedMinutes = request.EstimatedMinutes;
        entity.XpReward = request.XpReward;
        entity.AttributeType = attribute;
        entity.Weight = request.Weight;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(entity));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.Punishments.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null)
        {
            return NotFound();
        }

        _db.Punishments.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static bool TryParseEnums(
        PunishmentRequest request,
        out PunishmentCategory category,
        out ProofType proof,
        out AttributeType attribute,
        out string error)
    {
        category = default;
        proof = default;
        attribute = default;
        error = string.Empty;

        if (!Enum.TryParse(request.Category, true, out category))
        {
            error = $"Categoria inválida: '{request.Category}'. Use FISICO, ROTINA ou FINANCEIRO.";
            return false;
        }
        if (!Enum.TryParse(request.ProofType, true, out proof))
        {
            error = $"Tipo de prova inválido: '{request.ProofType}'.";
            return false;
        }
        if (!Enum.TryParse(request.AttributeType, true, out attribute))
        {
            error = $"Atributo inválido: '{request.AttributeType}'. Use STR, INT, WIS ou VIT.";
            return false;
        }
        return true;
    }

    private static PunishmentResponse ToResponse(Punishment p) => new(
        p.Id,
        p.Code,
        p.Category.ToString(),
        p.Title,
        p.Description,
        p.ProofType.ToString(),
        p.EstimatedMinutes,
        p.XpReward,
        p.AttributeType.ToString(),
        p.Weight,
        p.IsActive);
}
