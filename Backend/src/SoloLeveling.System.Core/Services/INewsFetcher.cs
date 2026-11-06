namespace SoloLeveling.System.Core.Services;

/// <summary>Uma manchete lida de um feed RSS/Atom.</summary>
public record Headline(string Source, string Title, string? Summary, DateTimeOffset? PublishedAt);

/// <summary>Busca manchetes recentes dos feeds configurados.</summary>
public interface INewsFetcher
{
    /// <summary>Lê todos os feeds e devolve as manchetes mais recentes, até o teto informado.</summary>
    Task<IReadOnlyList<Headline>> FetchAsync(int maxHeadlines, CancellationToken ct = default);
}
