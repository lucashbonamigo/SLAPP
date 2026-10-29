namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// A Ficha de Personagem. Espelha a tabela `users`.
/// </summary>
public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Hash BCrypt da senha. Null para usuários antigos (login via senha compartilhada).</summary>
    public string? PasswordHash { get; set; }

    public int Level { get; set; } = 1;

    public int CurrentXp { get; set; }

    /// <summary>FOR - Força/Físico (stat_str).</summary>
    public int StatStr { get; set; } = 10;

    /// <summary>INT - Inteligência/Estudos (stat_int).</summary>
    public int StatInt { get; set; } = 10;

    /// <summary>SAB - Sabedoria/Disciplina (stat_wis).</summary>
    public int StatWis { get; set; } = 10;

    /// <summary>VIT - Vitalidade/Resistência (stat_vit).</summary>
    public int StatVit { get; set; } = 10;

    /// <summary>Horário limite inflexível para envio de evidências.</summary>
    public TimeSpan DailyDeadline { get; set; } = new(21, 30, 0);

    /// <summary>Total de missões não cumpridas acumuladas.</summary>
    public int FailureCount { get; set; }

    /// <summary>Valor em R$ a ser doado (R$10 por falta).</summary>
    public decimal DebtAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Quest> Quests { get; set; } = new List<Quest>();

    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
}
