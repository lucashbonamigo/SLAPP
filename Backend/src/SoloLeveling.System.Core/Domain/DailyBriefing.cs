namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Briefing diário de notícias, gerado de manhã a partir de feeds RSS e resumido
/// pela IA. Um por dia. Espelha a tabela `daily_briefings`.
/// </summary>
public class DailyBriefing
{
    public int Id { get; set; }

    public DateOnly BriefingDate { get; set; }

    /// <summary>Resumo em markdown/bullets gerado pela IA (fallback/compatibilidade).</summary>
    public string SummaryMd { get; set; } = string.Empty;

    /// <summary>Itens curados em JSON: [{source,title,url,comment}]. Fonte dos links.</summary>
    public string? ItemsJson { get; set; }

    /// <summary>Quantas manchetes alimentaram o resumo.</summary>
    public int SourceCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
