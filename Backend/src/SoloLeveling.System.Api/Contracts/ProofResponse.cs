namespace SoloLeveling.System.Api.Contracts;

/// <summary>Resposta de POST /api/v1/quests/{id}/proof.</summary>
public record ProofResponse(int ProofId, int QuestId, string Message);
