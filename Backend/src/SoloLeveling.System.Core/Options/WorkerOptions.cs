namespace SoloLeveling.System.Core.Options;

public class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>Horário diário em que o Motor do Sistema executa a auditoria (HH:mm:ss).</summary>
    public string RunAt { get; set; } = "21:30:00";

    /// <summary>Id do jogador avaliado pelo worker (MVP single-player).</summary>
    public int PlayerUserId { get; set; } = 1;
}
