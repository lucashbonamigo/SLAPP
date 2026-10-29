namespace SoloLeveling.System.Core.Services;

public interface IProofStorage
{
    /// <summary>Salva o stream da evidência e devolve o caminho absoluto gravado.</summary>
    Task<string> SaveAsync(int questId, string originalFileName, Stream content, CancellationToken ct = default);

    /// <summary>Lê o conteúdo de uma evidência já gravada.</summary>
    Task<byte[]> ReadAsync(string filePath, CancellationToken ct = default);
}
