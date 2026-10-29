namespace SoloLeveling.System.Api.Contracts;

/// <summary>Resposta de GET /api/v1/player/status — a ficha atualizada do personagem.</summary>
public record PlayerStatusResponse(
    int Id,
    string Username,
    int Level,
    int CurrentXp,
    int XpToNextLevel,
    AttributesDto Attributes,
    string DailyDeadline);

public record AttributesDto(int For, int Int, int Sab, int Vit);
