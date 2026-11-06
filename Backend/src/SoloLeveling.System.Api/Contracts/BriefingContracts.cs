namespace SoloLeveling.System.Api.Contracts;

/// <summary>Resposta do briefing diário de notícias.</summary>
public record BriefingResponse(
    string BriefingDate,
    string SummaryMd,
    int SourceCount);
