namespace SoloLeveling.System.Core.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Diretório raiz onde as evidências são salvas (ex: /var/www/system_proofs/).</summary>
    public string ProofsPath { get; set; } = "/var/www/system_proofs";
}
