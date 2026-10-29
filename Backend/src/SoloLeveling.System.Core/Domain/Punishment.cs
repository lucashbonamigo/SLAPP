namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Punição do catálogo. O Worker sorteia uma (ponderada por <see cref="Weight"/>)
/// quando o jogador falha, e materializa uma Penalty Quest a partir dela.
/// Espelha a tabela `punishments`.
/// </summary>
public class Punishment
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public PunishmentCategory Category { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ProofType ProofType { get; set; } = ProofType.GENERIC;

    public int EstimatedMinutes { get; set; } = 45;

    public int XpReward { get; set; } = 100;

    public AttributeType AttributeType { get; set; } = AttributeType.VIT;

    /// <summary>Peso no sorteio. Maior = sai com mais frequência.</summary>
    public int Weight { get; set; } = 10;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}
