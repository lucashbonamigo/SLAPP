using System.ComponentModel.DataAnnotations;

namespace SoloLeveling.System.Api.Contracts;

/// <summary>Payload de POST /api/v1/reflections — a reflexão noturna de hoje.</summary>
public class ReflectionRequest
{
    /// <summary>Como o jogador reagiu no piloto automático diante do estresse/desafio.</summary>
    [Required]
    [MinLength(3)]
    public string AutopilotText { get; set; } = string.Empty;

    /// <summary>Como o "Eu Nível 100" teria reagido (opcional).</summary>
    public string? IdealText { get; set; }
}

/// <summary>Resposta com a reflexão e o feedback do mentor (quando já avaliado).</summary>
public record ReflectionResponse(
    int Id,
    string ReflectionDate,
    string AutopilotText,
    string? IdealText,
    string? MentorFeedback);
