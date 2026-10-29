using System.ComponentModel.DataAnnotations;

namespace SoloLeveling.System.Api.Contracts;

/// <summary>Item de resposta de um objetivo.</summary>
public record GoalResponse(
    int Id,
    string Title,
    string? Description,
    string AttributeType,
    string Category,
    string? TargetMetric,
    string? ContentBacklog,
    string Status,
    string? ProgressNotes,
    string StartDate,
    string? TargetDate);

/// <summary>Payload para criar/atualizar um objetivo.</summary>
public class GoalRequest
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>STR, INT, WIS ou VIT.</summary>
    [Required]
    public string AttributeType { get; set; } = "VIT";

    /// <summary>RUNNING, STUDY, READING ou GENERIC.</summary>
    [Required]
    public string Category { get; set; } = "GENERIC";

    [MaxLength(255)]
    public string? TargetMetric { get; set; }

    public string? ContentBacklog { get; set; }

    /// <summary>ACTIVE, COMPLETED ou PAUSED.</summary>
    [Required]
    public string Status { get; set; } = "ACTIVE";

    /// <summary>Notas de progresso. Opcional — a IA também atualiza este campo.</summary>
    public string? ProgressNotes { get; set; }

    /// <summary>Data de início (yyyy-MM-dd). Se vazio, usa hoje na criação.</summary>
    public string? StartDate { get; set; }

    /// <summary>Prazo final opcional (yyyy-MM-dd).</summary>
    public string? TargetDate { get; set; }
}
