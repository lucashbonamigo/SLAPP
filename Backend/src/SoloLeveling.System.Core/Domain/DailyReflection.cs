namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Reflexão noturna do jogador (o "Diário de Piloto Automático").
/// A IA lê na avaliação e devolve a reestruturação cognitiva em MentorFeedback.
/// Espelha a tabela `daily_reflections`.
/// </summary>
public class DailyReflection
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateOnly ReflectionDate { get; set; }

    /// <summary>Como o jogador reagiu no piloto automático diante do estresse/desafio.</summary>
    public string AutopilotText { get; set; } = string.Empty;

    /// <summary>Como o "Eu Nível 100" teria reagido (resposta deliberada desejada).</summary>
    public string? IdealText { get; set; }

    /// <summary>Reestruturação cognitiva devolvida pela IA.</summary>
    public string? MentorFeedback { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
}
