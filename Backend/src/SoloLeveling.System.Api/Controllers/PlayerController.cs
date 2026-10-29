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

    private int? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return int.TryParse(sub, out var id) ? id : null;
    }
}
