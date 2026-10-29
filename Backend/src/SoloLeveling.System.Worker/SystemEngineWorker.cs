using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Options;

namespace SoloLeveling.System.Worker;

/// <summary>
/// Agenda a execução diária do Motor do Sistema no horário WorkerOptions.RunAt (ex: 21:30:00).
/// </summary>
public class SystemEngineWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly WorkerOptions _options;
    private readonly ILogger<SystemEngineWorker> _logger;

    public SystemEngineWorker(
        IServiceProvider services,
        IOptions<WorkerOptions> options,
        ILogger<SystemEngineWorker> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TimeSpan.TryParse(_options.RunAt, out var runAt))
        {
            runAt = new TimeSpan(21, 30, 0);
            _logger.LogWarning("Worker:RunAt inválido. Usando padrão 21:30:00.");
        }

        _logger.LogInformation("Motor do Sistema ativo. Execução diária às {RunAt}.", runAt);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNext(runAt);
            _logger.LogInformation("Próxima auditoria em {Delay}.", delay);

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
            var engine = scope.ServiceProvider.GetRequiredService<DailyEvaluationService>();
            await engine.RunAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na execução do Motor do Sistema.");
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
