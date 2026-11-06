using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Options;

namespace SoloLeveling.System.Core.Services;

/// <summary>
/// Lê feeds RSS/Atom públicos com System.ServiceModel.Syndication. Falha de um
/// feed não derruba os demais: cada feed é lido isoladamente.
/// </summary>
public class RssNewsFetcher : INewsFetcher
{
    private readonly HttpClient _http;
    private readonly NewsOptions _options;
    private readonly ILogger<RssNewsFetcher> _logger;

    public RssNewsFetcher(
        HttpClient http,
        IOptions<NewsOptions> options,
        ILogger<RssNewsFetcher> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Headline>> FetchAsync(int maxHeadlines, CancellationToken ct = default)
    {
        var all = new List<Headline>();

        foreach (var feedUrl in _options.Feeds)
        {
            try
            {
                var items = await ReadFeedAsync(feedUrl, ct);
                all.AddRange(items);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao ler o feed {Feed}. Ignorando.", feedUrl);
            }
        }

        // Mais recentes primeiro; itens sem data vão para o fim.
        return all
            .OrderByDescending(h => h.PublishedAt ?? DateTimeOffset.MinValue)
            .Take(Math.Max(1, maxHeadlines))
            .ToList();
    }

    private async Task<List<Headline>> ReadFeedAsync(string feedUrl, CancellationToken ct)
    {
        await using var stream = await _http.GetStreamAsync(feedUrl, ct);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });

        var feed = SyndicationFeed.Load(reader);
        if (feed is null)
        {
            return new List<Headline>();
        }

        var source = string.IsNullOrWhiteSpace(feed.Title?.Text)
            ? new Uri(feedUrl).Host
            : feed.Title!.Text;

        return feed.Items
            .Select(item => new Headline(
                source,
                item.Title?.Text?.Trim() ?? "(sem título)",
                Truncate(item.Summary?.Text, 280),
                item.PublishDate == default ? null : item.PublishDate))
            .Where(h => !string.IsNullOrWhiteSpace(h.Title))
            .ToList();
    }

    private static string? Truncate(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        // Remove tags HTML simples que alguns feeds trazem no summary.
        var clean = Regex.Replace(text, "<.*?>", string.Empty).Trim();
        return clean.Length <= max ? clean : clean[..max] + "…";
    }
}
