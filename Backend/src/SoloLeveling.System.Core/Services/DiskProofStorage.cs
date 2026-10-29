using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Options;

namespace SoloLeveling.System.Core.Services;

/// <summary>
/// Grava as evidências no disco da VPS (StorageOptions.ProofsPath).
/// </summary>
public class DiskProofStorage : IProofStorage
{
    private readonly StorageOptions _options;

    public DiskProofStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveAsync(int questId, string originalFileName, Stream content, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var targetDir = Path.Combine(_options.ProofsPath, today);
        Directory.CreateDirectory(targetDir);

        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var fileName = $"quest_{questId}_{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetDir, fileName);

        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs, ct);
        }

        return fullPath;
    }

    public async Task<byte[]> ReadAsync(string filePath, CancellationToken ct = default)
    {
        return await File.ReadAllBytesAsync(filePath, ct);
    }
}
