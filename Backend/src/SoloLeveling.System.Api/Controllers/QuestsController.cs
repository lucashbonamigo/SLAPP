using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoloLeveling.System.Api.Contracts;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Domain;
using SoloLeveling.System.Core.Services;

namespace SoloLeveling.System.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/quests")]
public class QuestsController : ControllerBase
{
    private readonly SystemDbContext _db;
    private readonly IProofStorage _storage;
    private readonly ILogger<QuestsController> _logger;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/heic", "image/gif"
    };

    public QuestsController(SystemDbContext db, IProofStorage storage, ILogger<QuestsController> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    /// <summary>Lista as missões de hoje, marcando quais já têm evidência.</summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(IEnumerable<QuestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var today = DateOnly.FromDateTime(DateTime.Now);

        var quests = await _db.Quests
            .AsNoTracking()
            .Where(q => q.UserId == userId && q.TargetDate == today)
            .OrderBy(q => q.Id)
            .Select(q => new QuestResponse(
                q.Id,
                q.GoalId,
                q.Title,
                q.Description,
                q.MetricTarget,
                q.ProofType.ToString(),
                q.EstimatedMinutes,
                q.AttributeType.ToString(),
                q.XpReward,
                q.Status.ToString(),
                q.Kind.ToString(),
                q.IsPenalty,
                q.TargetDate.ToString("yyyy-MM-dd"),
                q.Proofs.Any()))
            .ToListAsync(ct);

        return Ok(quests);
    }

    /// <summary>
    /// Lista todas as missões do jogador (histórico completo), para montar as abas
    /// de pendentes e concluídas no front. Mais recentes primeiro.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<QuestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var quests = await _db.Quests
            .AsNoTracking()
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.TargetDate)
            .ThenBy(q => q.Id)
            .Select(q => new QuestResponse(
                q.Id,
                q.GoalId,
                q.Title,
                q.Description,
                q.MetricTarget,
                q.ProofType.ToString(),
                q.EstimatedMinutes,
                q.AttributeType.ToString(),
                q.XpReward,
                q.Status.ToString(),
                q.Kind.ToString(),
                q.IsPenalty,
                q.TargetDate.ToString("yyyy-MM-dd"),
                q.Proofs.Any()))
            .ToListAsync(ct);

        return Ok(quests);
    }

    /// <summary>Recebe a evidência (multipart/form-data). Recusa após o daily_deadline.</summary>
    [HttpPost("{id:int}/proof")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(ProofResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadProof(int id, IFormFile file, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Arquivo de evidência ausente." });
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = $"Tipo de arquivo não permitido: {file.ContentType}" });
        }

        var quest = await _db.Quests
            .Include(q => q.User)
            .FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId, ct);

        if (quest is null)
        {
            return NotFound(new { message = "Missão não encontrada." });
        }

        // Trava do ambiente: nada é aceito após o horário limite inflexível.
        var deadline = quest.User!.DailyDeadline;
        var now = DateTime.Now.TimeOfDay;
        if (now > deadline)
        {
            _logger.LogWarning("Upload recusado para quest {QuestId}: {Now} após deadline {Deadline}.", id, now, deadline);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "O prazo do Sistema expirou. Evidência recusada.",
                deadline = deadline.ToString(@"hh\:mm\:ss")
            });
        }

        string savedPath;
        await using (var stream = file.OpenReadStream())
        {
            savedPath = await _storage.SaveAsync(quest.Id, file.FileName, stream, ct);
        }

        var proof = new Proof
        {
            QuestId = quest.Id,
            FilePath = savedPath,
            UploadedAt = DateTime.UtcNow,
            AiVerified = false
        };

        _db.Proofs.Add(proof);
        await _db.SaveChangesAsync(ct);

        return Ok(new ProofResponse(proof.Id, quest.Id, "Evidência anexada, aguardando julgamento do Sistema."));
    }

    private int? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }
}
