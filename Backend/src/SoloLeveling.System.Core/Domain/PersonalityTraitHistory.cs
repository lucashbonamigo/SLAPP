namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Snapshot diário de um traço de personalidade, para plotar o radar ao longo
/// do tempo e comparar com a silhueta "Nível 100".
/// Espelha a tabela `personality_trait_history`.
/// </summary>
public class PersonalityTraitHistory
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public BigFiveTrait TraitCode { get; set; }

    /// <summary>Valor do traço na data do snapshot.</summary>
    public decimal Value { get; set; }

    /// <summary>Variação aplicada neste dia pela IA.</summary>
    public decimal Delta { get; set; }

    public DateOnly RecordedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
