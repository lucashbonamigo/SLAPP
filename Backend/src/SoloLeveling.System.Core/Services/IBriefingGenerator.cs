namespace SoloLeveling.System.Core.Services;

/// <summary>Um item curado do briefing: a manchete original + a nota do mentor.</summary>
public record BriefingItem(string Source, string Title, string? Url, string Comment);

/// <summary>Seleciona e comenta as manchetes mais relevantes via IA.</summary>
public interface IBriefingGenerator
{
    /// <summary>
    /// Recebe manchetes brutas e devolve as mais relevantes em TI, C#/.NET,
    /// economia e geopolítica, cada uma com um comentário curto e o link original preservado.
    /// </summary>
    Task<IReadOnlyList<BriefingItem>> CurateAsync(IReadOnlyList<Headline> headlines, CancellationToken ct = default);
}
