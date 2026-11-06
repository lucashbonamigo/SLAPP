namespace SoloLeveling.System.Core.Services;

/// <summary>Resume/filtra manchetes num briefing curto via IA.</summary>
public interface IBriefingGenerator
{
    /// <summary>
    /// Recebe manchetes brutas e devolve um resumo em markdown (bullets) do que
    /// importa em TI, C#/.NET, economia e geopolítica.
    /// </summary>
    Task<string> SummarizeAsync(IReadOnlyList<Headline> headlines, CancellationToken ct = default);
}
