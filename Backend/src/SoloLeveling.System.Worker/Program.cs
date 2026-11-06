using SoloLeveling.System.Core;
using SoloLeveling.System.Core.Services;
using SoloLeveling.System.Worker;

// Modo de teste: "dotnet run -- --run-now" (ou env RUN_NOW=true) executa a auditoria
// uma única vez e encerra, simulando o gatilho das 21:30 sem esperar o relógio.
var runNow =
    args.Contains("--run-now", StringComparer.OrdinalIgnoreCase) ||
    string.Equals(Environment.GetEnvironmentVariable("RUN_NOW"), "true", StringComparison.OrdinalIgnoreCase);

// Execução manual só do briefing de notícias (sem esperar a manhã).
var runBriefingNow =
    args.Contains("--run-briefing-now", StringComparer.OrdinalIgnoreCase) ||
    string.Equals(Environment.GetEnvironmentVariable("RUN_BRIEFING_NOW"), "true", StringComparison.OrdinalIgnoreCase);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSystemCore(builder.Configuration);
builder.Services.AddScoped<DailyEvaluationService>();

if (runBriefingNow)
{
    // Gera o briefing de hoje uma única vez e encerra.
    using var host = builder.Build();
    using var scope = host.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RunBriefingNow");
    var service = scope.ServiceProvider.GetRequiredService<DailyBriefingService>();

    logger.LogInformation("== Geração manual do briefing (--run-briefing-now) ==");
    try
    {
        await service.RunAsync();
        logger.LogInformation("== Briefing concluído ==");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Falha no briefing manual.");
        Environment.ExitCode = 1;
    }

    return;
}

if (runNow)
{
    // Execução imediata e única, sem agendar o worker em background.
    using var host = builder.Build();

    using var scope = host.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("RunNow");
    var engine = scope.ServiceProvider.GetRequiredService<DailyEvaluationService>();

    logger.LogInformation("== Simulação manual da auditoria (--run-now) ==");
    try
    {
        await engine.RunAsync();
        logger.LogInformation("== Auditoria concluída ==");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Falha na auditoria manual.");
        Environment.ExitCode = 1;
    }

    return;
}

// Modo normal: agenda a auditoria noturna (21:30) e o briefing matinal (05:00).
builder.Services.AddHostedService<SystemEngineWorker>();
builder.Services.AddHostedService<BriefingScheduleWorker>();

var app = builder.Build();
app.Run();
