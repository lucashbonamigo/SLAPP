using SoloLeveling.System.Core;
using SoloLeveling.System.Worker;

// Modo de teste: "dotnet run -- --run-now" (ou env RUN_NOW=true) executa a auditoria
// uma única vez e encerra, simulando o gatilho das 21:30 sem esperar o relógio.
var runNow =
    args.Contains("--run-now", StringComparer.OrdinalIgnoreCase) ||
    string.Equals(Environment.GetEnvironmentVariable("RUN_NOW"), "true", StringComparison.OrdinalIgnoreCase);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSystemCore(builder.Configuration);
builder.Services.AddScoped<DailyEvaluationService>();

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

// Modo normal: agenda a execução diária (21:30).
builder.Services.AddHostedService<SystemEngineWorker>();

var app = builder.Build();
app.Run();
