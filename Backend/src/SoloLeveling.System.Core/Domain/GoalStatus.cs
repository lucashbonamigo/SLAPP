namespace SoloLeveling.System.Core.Domain;

/// <summary>Estado de um objetivo. Apenas ACTIVE gera tarefas diárias.</summary>
public enum GoalStatus
{
    ACTIVE,
    COMPLETED,
    PAUSED
}
