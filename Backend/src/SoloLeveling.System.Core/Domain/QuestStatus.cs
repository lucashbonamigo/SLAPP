namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Estado de uma missão. Mapeado para o ENUM status na tabela quests.
/// </summary>
public enum QuestStatus
{
    PENDING,
    COMPLETED,
    FAILED
}
