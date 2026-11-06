namespace SoloLeveling.System.Api.Contracts;

/// <summary>Um item curado do briefing, com link para a fonte original.</summary>
public record BriefingItemDto(string Source, string Title, string? Url, string Comment);

/// <summary>Resposta do briefing diário de notícias.</summary>
public record BriefingResponse(
    string BriefingDate,
    string SummaryMd,
    int SourceCount,
    IReadOnlyList<BriefingItemDto> Items);
