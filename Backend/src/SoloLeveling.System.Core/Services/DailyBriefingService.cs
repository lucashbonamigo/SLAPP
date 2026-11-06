using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Domain;
using SoloLeveling.System.Core.Options;

namespace SoloLeveling.System.Core.Services;

/// <summary>
/// Orquestra o briefing diário: busca manchetes dos feeds, pede o resumo à IA,
/// grava (upsert) o briefing do dia e envia por e-mail ao jogador. Idempotente por data.
/// </summary>
public class DailyBriefingService
{
    private readonly SystemDbContext _db;
    private readonly INewsFetcher _fetcher;
    private readonly IBriefingGenerator _generator;
    private readonly IEmailSender _email;
    private readonly NewsOptions _options;
    private readonly WorkerOptions _workerOptions;
    private readonly ILogger<DailyBriefingService> _logger;

    public DailyBriefingService(
        SystemDbContext db,
        INewsFetcher fetcher,
        IBriefingGenerator generator,
        IEmailSender email,
        IOptions<NewsOptions> options,
        IOptions<WorkerOptions> workerOptions,
        ILogger<DailyBriefingService> logger)
    {
        _db = db;
        _fetcher = fetcher;
        _generator = generator;
        _email = email;
        _options = options.Value;
        _workerOptions = workerOptions.Value;
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

        var items = await _generator.CurateAsync(headlines, ct);
        _logger.LogInformation("Briefing: {Count} itens curados pela IA.", items.Count);

        if (items.Count == 0)
        {
            _logger.LogWarning("IA não selecionou nenhuma manchete. Briefing de {Date} não gerado.", today);
            return;
        }

        var itemsJson = JsonSerializer.Serialize(items);
        // summary_md derivado como fallback legível (sem link) a partir dos itens.
        var summary = string.Join("\n", items.Select(i => $"- {i.Comment}"));

        var existing = await _db.DailyBriefings
            .FirstOrDefaultAsync(b => b.BriefingDate == today, ct);

        if (existing is null)
        {
            _db.DailyBriefings.Add(new DailyBriefing
            {
                BriefingDate = today,
                SummaryMd = summary,
                ItemsJson = itemsJson,
                SourceCount = items.Count,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.SummaryMd = summary;
            existing.ItemsJson = itemsJson;
            existing.SourceCount = items.Count;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Briefing de {Date} gravado ({Count} itens).", today, items.Count);

        await SendBriefingEmailAsync(today, items, ct);
    }

    /// <summary>Envia o briefing do dia por e-mail ao jogador configurado no Worker.</summary>
    private async Task SendBriefingEmailAsync(DateOnly date, IReadOnlyList<BriefingItem> items, CancellationToken ct)
    {
        var player = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _workerOptions.PlayerUserId, ct);

        if (player is null || string.IsNullOrWhiteSpace(player.Email))
        {
            _logger.LogWarning("Jogador sem e-mail. Briefing de {Date} não enviado.", date);
            return;
        }

        var subject = $"[O SISTEMA] Briefing do Dia — {date:dd/MM}";
        var body = BuildBriefingHtml(player.Username, date, items);

        try
        {
            await _email.SendAsync(player.Email, subject, body, ct);
            _logger.LogInformation("Briefing de {Date} enviado para {Email}.", date, player.Email);
        }
        catch (Exception ex)
        {
            // Falha de e-mail não deve derrubar a geração do briefing (já gravado).
            _logger.LogError(ex, "Falha ao enviar o briefing por e-mail.");
        }
    }

    /// <summary>Monta o HTML do e-mail: cada notícia é um link clicável + comentário do mentor.</summary>
    private static string BuildBriefingHtml(string username, DateOnly date, IReadOnlyList<BriefingItem> items)
    {
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Arial,sans-serif;max-width:560px;margin:0 auto;color:#1f2937\">");
        sb.Append("<h2 style=\"color:#0ea5e9;margin-bottom:4px\">Briefing do Dia</h2>");
        sb.Append($"<p style=\"color:#6b7280;margin-top:0;font-size:13px\">{WebUtility.HtmlEncode(username)} · {date:dd/MM/yyyy} · {items.Count} notícias</p>");

        sb.Append("<ul style=\"padding-left:18px;line-height:1.5\">");
        foreach (var it in items)
        {
            sb.Append("<li style=\"margin-bottom:12px\">");

            var title = WebUtility.HtmlEncode(it.Title);
            if (!string.IsNullOrWhiteSpace(it.Url))
            {
                var href = WebUtility.HtmlEncode(it.Url);
                sb.Append($"<a href=\"{href}\" style=\"color:#0ea5e9;font-weight:bold;text-decoration:none\">{title}</a>");
            }
            else
            {
                sb.Append($"<strong>{title}</strong>");
            }

            sb.Append($"<br><span style=\"font-size:13px\">{WebUtility.HtmlEncode(it.Comment)}</span>");
            sb.Append($"<br><span style=\"color:#9ca3af;font-size:11px\">{WebUtility.HtmlEncode(it.Source)}</span>");
            sb.Append("</li>");
        }
        sb.Append("</ul>");

        sb.Append("<p style=\"color:#9ca3af;font-size:11px;margin-top:20px\">O Sistema · curadoria automática de notícias</p>");
        sb.Append("</div>");
        return sb.ToString();
    }
}
