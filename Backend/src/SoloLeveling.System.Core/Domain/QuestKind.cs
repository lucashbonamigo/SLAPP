namespace SoloLeveling.System.Core.Domain;

/// <summary>Natureza da missão. Substitui o antigo flag is_penalty.</summary>
public enum QuestKind
{
    /// <summary>Missão normal, derivada de um objetivo ativo.</summary>
    NORMAL,
    /// <summary>Punição herdada de uma falha.</summary>
    PENALTY,
    /// <summary>Tarefa bônus de evento aleatório.</summary>
    BONUS
}
