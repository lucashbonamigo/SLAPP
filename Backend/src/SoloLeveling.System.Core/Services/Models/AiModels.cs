using SoloLeveling.System.Core.Domain;

namespace SoloLeveling.System.Core.Services.Models;

/// <summary>Uma evidência enviada para a IA avaliar.</summary>
public record ProofImage(int QuestId, string FilePath, byte[] Content, string MimeType);

/// <summary>Resumo de um objetivo ativo, para a IA fatiar o próximo passo.</summary>
public record GoalSummary(
    int GoalId,
    string Title,
    AttributeType Attribute,
    GoalCategory Category,
    string? TargetMetric,
    string? ContentBacklog,
    string? ProgressNotes);

/// <summary>Resumo de uma quest para montar o prompt da IA.</summary>
public record QuestSummary(
    int QuestId,
    int? GoalId,
    string Title,
    string? Description,
    string? MetricTarget,
    ProofType ProofType,
    AttributeType Attribute,
    int XpReward,
    bool HasProof);

/// <summary>Veredito da IA para uma quest específica.</summary>
public record QuestVerdict(int QuestId, bool Approved, int XpDelta, string Comment);

/// <summary>Nova missão gerada pela IA para o dia seguinte.</summary>
public record GeneratedQuest(
    int? GoalId,
    string Title,
    string? Description,
    string? MetricTarget,
    ProofType ProofType,
    int EstimatedMinutes,
    AttributeType Attribute,
    int XpReward,
    QuestKind Kind);

/// <summary>Atualização de progresso de um objetivo (a "memória" da IA).</summary>
public record GoalProgress(int GoalId, string Note);

/// <summary>Resultado completo da avaliação diária da IA.</summary>
public record EvaluationResult(
    IReadOnlyList<QuestVerdict> Verdicts,
    IReadOnlyList<GeneratedQuest> TomorrowQuests,
    IReadOnlyList<GoalProgress> GoalProgress,
    int TotalXpDelta,
    int FailuresToCharge,
    string VerdictSummary,
    bool IsPunishment);
