namespace SoloLeveling.System.Api.Contracts;

/// <summary>Item de GET /api/v1/quests e /quests/today.</summary>
public record QuestResponse(
    int Id,
    int? GoalId,
    string Title,
    string? Description,
    string? MetricTarget,
    string ProofType,
    int EstimatedMinutes,
    string AttributeType,
    int XpReward,
    string Status,
    string Kind,
    bool IsPenalty,
    string TargetDate,
    bool HasProof);
