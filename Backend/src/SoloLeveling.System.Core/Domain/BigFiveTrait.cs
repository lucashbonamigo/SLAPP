namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Fatores do Modelo dos Cinco Grandes (Big Five / OCEAN). Mapeados para o
/// ENUM do MySQL em personality_traits / personality_trait_history.
/// Para NEUROTICISM, valores MENORES representam maior resiliência/mente fria.
/// </summary>
public enum BigFiveTrait
{
    /// <summary>Abertura à experiência (curiosidade, criatividade).</summary>
    OPENNESS,

    /// <summary>Conscienciosidade (disciplina, organização, foco).</summary>
    CONSCIENTIOUSNESS,

    /// <summary>Extroversão (assertividade, exposição social).</summary>
    EXTRAVERSION,

    /// <summary>Amabilidade (cooperação, empatia).</summary>
    AGREEABLENESS,

    /// <summary>Neuroticismo (reatividade ao estresse; menor é melhor).</summary>
    NEUROTICISM
}
