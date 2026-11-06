namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Estado atual de um traço de personalidade (Big Five) do jogador.
/// Média móvel 0-100 mantida pela IA a cada avaliação noturna.
/// Espelha a tabela `personality_traits`.
/// </summary>
public class PersonalityTrait
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public BigFiveTrait TraitCode { get; set; }

    /// <summary>Média móvel 0-100. 50 = neutro.</summary>
    public decimal Value { get; set; } = 50m;

    public DateTime UpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
