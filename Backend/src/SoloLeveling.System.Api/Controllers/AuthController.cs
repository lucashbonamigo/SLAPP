using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SoloLeveling.System.Api.Auth;
using SoloLeveling.System.Api.Contracts;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Domain;

namespace SoloLeveling.System.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly SystemDbContext _db;
    private readonly TokenService _tokenService;
    private readonly ApiAuthOptions _authOptions;

    public AuthController(SystemDbContext db, TokenService tokenService, IOptions<ApiAuthOptions> authOptions)
    {
        _db = db;
        _tokenService = tokenService;
        _authOptions = authOptions.Value;
    }

    /// <summary>Cadastra um novo jogador com senha própria (hash BCrypt).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (exists)
        {
            return Conflict(new { message = "Já existe um jogador com este e-mail." });
        }

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            // Demais campos (level, xp, atributos, deadline) usam os defaults do banco.
            DailyDeadline = new TimeSpan(21, 30, 0),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var (token, expiresIn) = _tokenService.CreateToken(user);
        return StatusCode(StatusCodes.Status201Created, new LoginResponse(token, "Bearer", expiresIn));
    }

    /// <summary>Autentica o jogador e devolve um token JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !IsPasswordValid(user, request.Password))
        {
            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        var (token, expiresIn) = _tokenService.CreateToken(user);
        return Ok(new LoginResponse(token, "Bearer", expiresIn));
    }

    /// <summary>
    /// Valida a senha. Usuários com hash próprio usam BCrypt; usuários antigos
    /// (password_hash null) caem no fallback da senha compartilhada do MVP.
    /// </summary>
    private bool IsPasswordValid(User user, string password)
    {
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch
            {
                return false;
            }
        }

        // Fallback legado: senha compartilhada (ApiAuth:Password).
        return !string.IsNullOrWhiteSpace(_authOptions.Password)
            && password == _authOptions.Password;
    }
}
