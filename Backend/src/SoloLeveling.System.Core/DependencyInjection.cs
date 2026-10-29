using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Options;
using SoloLeveling.System.Core.Services;

namespace SoloLeveling.System.Core;

public static class DependencyInjection
{
    /// <summary>
    /// Registra DbContext (MySQL via Pomelo), opções e serviços compartilhados entre API e Worker.
    /// </summary>
    public static IServiceCollection AddSystemCore(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");

        services.AddDbContext<SystemDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.Configure<SystemRulesOptions>(configuration.GetSection(SystemRulesOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));

        services.AddScoped<IProofStorage, DiskProofStorage>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddHttpClient<IGeminiEvaluator, GeminiEvaluator>(client =>
        {
            // Evita travar indefinidamente se o modelo estiver congestionado.
            client.Timeout = TimeSpan.FromSeconds(90);
        });

        return services;
    }
}
