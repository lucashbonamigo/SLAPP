namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Tipo do objetivo. Define como a IA gera a tarefa e qual comprovação exigir.
/// </summary>
public enum GoalCategory
{
    /// <summary>Corrida/ciclismo. Comprovação via print do Strava (distância+pace+tempo).</summary>
    RUNNING,
    /// <summary>Estudo. Comprovação via resumo ou mapa mental.</summary>
    STUDY,
    /// <summary>Leitura. Comprovação via foto de resumo escrito.</summary>
    READING,
    /// <summary>Genérico, sem regra específica.</summary>
    GENERIC
}
