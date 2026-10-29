using SoloLeveling.System.Core.Domain;
using SoloLeveling.System.Core.Services.Models;

namespace SoloLeveling.System.Core.Services;

/// <summary>
/// Abstração do "Sistema" via Gemini. Avalia evidências e gera as metas do dia
/// seguinte alinhadas aos objetivos ativos do jogador.
/// </summary>
public interface IGeminiEvaluator
{
    /// <summary>
    /// Cenário A: todas as quests têm evidência. Avalia as imagens, atualiza o
    /// progresso dos objetivos e gera as metas de amanhã.
    /// </summary>
    Task<EvaluationResult> EvaluateAsync(
        User player,
        IReadOnlyList<GoalSummary> goals,
        IReadOnlyList<QuestSummary> quests,
        IReadOnlyList<ProofImage> proofs,
        CancellationToken ct = default);

    /// <summary>
    /// Cenário B: falta pelo menos uma evidência. Gera a punição prescritiva e a
    /// Penalty Quest, e ainda assim fatia o próximo passo dos objetivos.
    /// </summary>
    Task<EvaluationResult> PunishAsync(
        User player,
        IReadOnlyList<GoalSummary> goals,
        IReadOnlyList<QuestSummary> quests,
        CancellationToken ct = default);
}
