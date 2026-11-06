using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Options;

namespace SoloLeveling.System.Core.Services;

/// <summary>
/// Gera o briefing diário chamando o Gemini (texto puro) para filtrar e resumir
/// as manchetes nos temas de interesse do jogador.
/// </summary>
public class GeminiBriefingGenerator : IBriefingGenerator
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiBriefingGenerator> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiBriefingGenerator(
        HttpClient http,
        IOptions<GeminiOptions> options,
        ILogger<GeminiBriefingGenerator> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> SummarizeAsync(IReadOnlyList<Headline> headlines, CancellationToken ct = default)
    {
        if (headlines.Count == 0)
        {
            return "Nenhuma manchete disponível hoje.";
        }

        var prompt = BuildPrompt(headlines);

        var requestBody = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.4 }
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        using var response = await _http.PostAsJsonAsync(url, requestBody, JsonOpts, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini (briefing) retornou {Status}: {Payload}", response.StatusCode, payload);
            throw new InvalidOperationException($"Falha ao gerar briefing: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(payload);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return string.IsNullOrWhiteSpace(text)
            ? "O Sistema não conseguiu gerar o briefing de hoje."
            : text.Trim();
    }

    private static string BuildPrompt(IReadOnlyList<Headline> headlines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Você é o curador de notícias de um mentor de alta performance.");
        sb.AppendLine("Filtre as manchetes abaixo e mantenha APENAS o que afeta o mercado global de tecnologia,");
        sb.AppendLine("C#/.NET e engenharia de software, economia e geopolítica. Descarte fofoca, entretenimento e ruído.");
        sb.AppendLine("Responda em português do Brasil, em NO MÁXIMO 5 bullets curtos (markdown, iniciando com '- ').");
        sb.AppendLine("Cada bullet: um fato objetivo + por que importa, em 1 frase. Sem introdução nem conclusão.");
        sb.AppendLine();
        sb.AppendLine("MANCHETES:");
        foreach (var h in headlines)
        {
            sb.Append("- [").Append(h.Source).Append("] ").Append(h.Title);
            if (!string.IsNullOrWhiteSpace(h.Summary))
            {
                sb.Append(" — ").Append(h.Summary);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
