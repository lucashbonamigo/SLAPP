namespace SoloLeveling.System.Core.Options;

/// <summary>
/// Regras de jogo configuráveis do Sistema: economia de faltas, eventos
/// aleatórios, limites de tarefa e o catálogo de punições que a IA pode aplicar.
/// </summary>
public class SystemRulesOptions
{
    public const string SectionName = "SystemRules";

    /// <summary>Valor em R$ somado à dívida por cada missão não cumprida.</summary>
    public decimal FeePerFailure { get; set; } = 10m;

    /// <summary>Chance diária (0-100) de gerar uma tarefa bônus.</summary>
    public int BonusChancePercent { get; set; } = 5;

    /// <summary>Teto de duração de qualquer tarefa, em minutos.</summary>
    public int MaxTaskMinutes { get; set; } = 60;

    /// <summary>Quantas tarefas normais gerar por dia (uma por objetivo ativo).</summary>
    public int DailyTaskCount { get; set; } = 3;

    /// <summary>Catálogo de punições que a IA escolhe ao aplicar penalidade.</summary>
    public List<PunishmentOption> PunishmentCatalog { get; set; } = new();
}

public class PunishmentOption
{
    /// <summary>Identificador curto. Ex: "TAXA_MADRUGADA".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Categoria: FISICO, ROTINA, FINANCEIRO.</summary>
    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>Descrição prescritiva: o que fazer, quanto, como comprovar.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Tipo de comprovação esperado (STRAVA, PHOTO, PAYMENT_RECEIPT, GENERIC...).</summary>
    public string ProofType { get; set; } = "GENERIC";
}
