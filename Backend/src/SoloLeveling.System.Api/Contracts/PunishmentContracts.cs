using System.ComponentModel.DataAnnotations;

namespace SoloLeveling.System.Api.Contracts;

/// <summary>Item de resposta do catálogo de punições.</summary>
public record PunishmentResponse(
    int Id,
    string Code,
    string Category,
    string Title,
    string Description,
    string ProofType,
    int EstimatedMinutes,
    int XpReward,
    string AttributeType,
    int Weight,
    bool IsActive);

/// <summary>Payload para criar/atualizar uma punição.</summary>
public class PunishmentRequest
{
    [Required]
    [MaxLength(40)]
    public string Code { get; set; } = string.Empty;

    /// <summary>FISICO, ROTINA ou FINANCEIRO.</summary>
    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>STRAVA, SUMMARY, MINDMAP, PHOTO, PAYMENT_RECEIPT ou GENERIC.</summary>
    [Required]
    public string ProofType { get; set; } = "GENERIC";

    [Range(1, 480)]
    public int EstimatedMinutes { get; set; } = 45;

    [Range(0, 100000)]
    public int XpReward { get; set; } = 100;

    /// <summary>STR, INT, WIS ou VIT.</summary>
    [Required]
    public string AttributeType { get; set; } = "VIT";

    [Range(1, 1000)]
    public int Weight { get; set; } = 10;

    public bool IsActive { get; set; } = true;
}
