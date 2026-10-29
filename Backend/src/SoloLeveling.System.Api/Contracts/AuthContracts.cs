using System.ComponentModel.DataAnnotations;

namespace SoloLeveling.System.Api.Contracts;

/// <summary>
/// Login do MVP: autentica pelo e-mail cadastrado do jogador.
/// A senha compartilhada (ApiAuth:Password) protege o acesso enquanto não há hash por usuário.
/// </summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(string AccessToken, string TokenType, int ExpiresInSeconds);

/// <summary>Cadastro de um novo jogador, com senha própria (hash BCrypt).</summary>
public class RegisterRequest
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}
