namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Daily Quest. Espelha a tabela `quests`.
/// </summary>
public class Quest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>Objetivo que originou esta missão (null para punições/bônus avulsos).</summary>
    public int? GoalId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Meta mensurável. Ex: "6 km a 5:30/km em até 33 min".</summary>
    public string? MetricTarget { get; set; }

    /// <summary>O que comprova a execução desta tarefa.</summary>
    public ProofType ProofType { get; set; } = ProofType.GENERIC;

    /// <summary>Teto de esforço estimado, nunca acima de 60 minutos.</summary>
    public int EstimatedMinutes { get; set; } = 60;

    public AttributeType AttributeType { get; set; }

    public int XpReward { get; set; } = 10;

    public QuestStatus Status { get; set; } = QuestStatus.PENDING;

    /// <summary>Marca se é uma punição herdada do dia anterior (legado; ver <see cref="Kind"/>).</summary>
    public bool IsPenalty { get; set; }

    /// <summary>Natureza da missão: normal, punição ou bônus.</summary>
    public QuestKind Kind { get; set; } = QuestKind.NORMAL;

    /// <summary>Data em que a missão deve ser cumprida.</summary>
    public DateOnly TargetDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public Goal? Goal { get; set; }

    public ICollection<Proof> Proofs { get; set; } = new List<Proof>();
}
