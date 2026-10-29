namespace SoloLeveling.System.Core.Domain;

/// <summary>
/// Proof of Work. Espelha a tabela `proofs`.
/// </summary>
public class Proof
{
    public int Id { get; set; }

    public int QuestId { get; set; }

    /// <summary>Caminho do arquivo no SSD da VPS (ex: /var/www/system_proofs/...).</summary>
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }

    /// <summary>Atualizado para TRUE após a IA validar a evidência.</summary>
    public bool AiVerified { get; set; }

    public Quest? Quest { get; set; }
}
