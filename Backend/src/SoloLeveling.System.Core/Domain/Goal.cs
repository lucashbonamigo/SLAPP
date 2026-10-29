namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Objetivo macro do jogador. É a "memória de longo prazo" do Sistema:
/// a IA lê os objetivos ativos para gerar o próximo passo de cada um e
/// atualiza <see cref="ProgressNotes"/> a cada avaliação. Espelha a tabela `goals`.
/// </summary>
public class Goal
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public AttributeType AttributeType { get; set; }

    public GoalCategory Category { get; set; } = GoalCategory.GENERIC;

    /// <summary>Meta mensurável final. Ex: "21.1 km em menos de 2h".</summary>
    public string? TargetMetric { get; set; }

    /// <summary>Conteúdos/etapas que a IA vai consumindo dia a dia.</summary>
    public string? ContentBacklog { get; set; }

    public GoalStatus Status { get; set; } = GoalStatus.ACTIVE;

    /// <summary>Atualizado pela IA a cada dia: a evolução e o próximo passo.</summary>
    public string? ProgressNotes { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? TargetDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public ICollection<Quest> Quests { get; set; } = new List<Quest>();
}
