namespace SoloLeveling.System.Api.Auth;

/// <summary>
/// Credencial compartilhada do MVP. Enquanto não há hash de senha por usuário,
/// o login valida o e-mail existente + esta senha única.
/// </summary>
public class ApiAuthOptions
{
    public const string SectionName = "ApiAuth";

    public string Password { get; set; } = string.Empty;
}
