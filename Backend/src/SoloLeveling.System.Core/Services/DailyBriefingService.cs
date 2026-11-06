using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Domain;
using SoloLeveling.System.Core.Options;

namespace SoloLeveling.System.Core.Services;

/// <summary>
/// Orquestra o briefing diário: busca manchetes dos feeds, pede o resumo à IA e
/// grava (upsert) o briefing do dia. Idempotente por data.
/// </summary>
public class DailyBriefingService
{
    private readonly SystemDbContext _db;
    private readonly INewsFetcher _fetcher;
    private readonly IBriefingGenerator _generator;
    private readonly NewsOptions _options;
    private readonly ILogger<DailyBriefingService> _logger;

    public DailyBriefingService(
        SystemDbContext db,
        INewsFetcher fetcher,
        IBriefingGenerator generator,
        IOptions<NewsOptions> options,
        ILogger<DailyBriefingService> logger)
    {
        _db = db;
        _fetcher = fetcher;
        _generator = generator;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        var headlines = await _fetcher.FetchAsync(_options.MaxHeadlines, ct);
        _logger.LogInformation("Briefing: {Count} manchetes coletadas.", headlines.Count);

        if (headlines.Count == 0)
        {
            _logger.LogWarning("Nenhuma manchete coletada. Briefing de {Date} não gerado.", today);
            return;
        }

        var summary = await _generator.SummarizeAsync(headlines, ct);

        var existing = await _db.DailyBriefings
            .FirstOrDefaultAsync(b => b.BriefingDate == today, ct);

        if (existing is null)
        {
            _db.DailyBriefings.Add(new DailyBriefing
            {
                BriefingDate = today,
                SummaryMd = summary,
                SourceCount = headlines.Count,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.SummaryMd = summary;
            existing.SourceCount = headlines.Count;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Briefing de {Date} gravado ({Count} fontes).", today, headlines.Count);
    }
}
