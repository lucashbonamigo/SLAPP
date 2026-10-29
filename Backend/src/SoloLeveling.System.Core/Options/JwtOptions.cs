namespace SoloLeveling.System.Core.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SoloLeveling.System";
    public string Audience { get; set; } = "SoloLeveling.System.Client";

    /// <summary>Chave simétrica para assinar os tokens (mínimo 32 chars).</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 720;
}
