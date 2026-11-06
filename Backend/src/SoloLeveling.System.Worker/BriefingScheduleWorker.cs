using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Options;
using SoloLeveling.System.Core.Services;

namespace SoloLeveling.System.Worker;

/// <summary>
/// Agenda o briefing diário de notícias no horário NewsOptions.RunAt (ex: 05:00).
/// Independente da auditoria noturna: cada um tem seu próprio relógio.
/// </summary>
public class BriefingScheduleWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly NewsOptions _options;
    private readonly ILogger<BriefingScheduleWorker> _logger;

    public BriefingScheduleWorker(
        IServiceProvider services,
        IOptions<NewsOptions> options,
        ILogger<BriefingScheduleWorker> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Briefing de notícias desabilitado (News:Enabled=false).");
            return;
        }

        if (!TimeSpan.TryParse(_options.RunAt, out var runAt))
        {
            runAt = new TimeSpan(5, 0, 0);
            _logger.LogWarning("News:RunAt inválido. Usando padrão 05:00:00.");
        }

        _logger.LogInformation("Briefing de notícias ativo. Execução diária às {RunAt}.", runAt);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNext(runAt);
            _logger.LogInformation("Próximo briefing em {Delay}.", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<DailyBriefingService>();
            await service.RunAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gerar o briefing diário.");
        }
    }

    private static TimeSpan TimeUntilNext(TimeSpan runAt)
    {
        var now = DateTime.Now;
        var next = now.Date.Add(runAt);
        if (next <= now)
        {
            next = next.AddDays(1);
        }
        return next - now;
    }
}
