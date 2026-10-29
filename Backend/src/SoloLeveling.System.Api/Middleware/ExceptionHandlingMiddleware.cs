using System.Text.Json;

namespace SoloLeveling.System.Api.Middleware;

/// <summary>
/// Converte exceções não tratadas em uma resposta JSON { message },
/// evitando 500 com stack trace cru (text/plain) chegando ao frontend.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado em {Path}.", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var message = _env.IsDevelopment()
                ? $"Erro interno: {ex.Message}"
                : "O Sistema encontrou um erro inesperado.";

            var payload = JsonSerializer.Serialize(new { message });
            await context.Response.WriteAsync(payload);
        }
    }
}
