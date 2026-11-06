namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Dado numérico (série temporal) produzido por uma missão aprovada.
/// A matéria-prima dos gráficos de evolução e da análise de platô pela IA.
/// Espelha a tabela `quest_metrics`.
/// </summary>
public class QuestMetric
{
    public int Id { get; set; }

    public int QuestId { get; set; }

    /// <summary>Objetivo macro associado (desnormalizado p/ séries temporais).</summary>
    public int? GoalId { get; set; }

    public int UserId { get; set; }

    /// <summary>Chave da métrica. Ex: pace_medio, bpm_medio, distancia_km, foco_min.</summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>Valor numérico plotável.</summary>
    public decimal MetricValue { get; set; }

    /// <summary>Unidade legível. Ex: km, min/km, bpm, min, pag.</summary>
    public string? Unit { get; set; }

    /// <summary>Data a que a métrica se refere (normalmente o target_date da quest).</summary>
    public DateOnly RecordedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public Quest? Quest { get; set; }

    public Goal? Goal { get; set; }

    public User? User { get; set; }
}
