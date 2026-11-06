namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Briefing diário de notícias, gerado de manhã a partir de feeds RSS e resumido
/// pela IA. Um por dia. Espelha a tabela `daily_briefings`.
/// </summary>
public class DailyBriefing
{
    public int Id { get; set; }

    public DateOnly BriefingDate { get; set; }

    /// <summary>Resumo em markdown/bullets gerado pela IA.</summary>
    public string SummaryMd { get; set; } = string.Empty;

    /// <summary>Quantas manchetes alimentaram o resumo.</summary>
    public int SourceCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
