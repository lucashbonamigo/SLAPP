using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Options;

namespace SoloLeveling.System.Core.Services;

/// <summary>
/// Cura o briefing diário chamando o Gemini (texto puro). A IA recebe as
/// manchetes numeradas e devolve QUAIS índices importam + um comentário por item.
/// O link original é resolvido a partir do índice, preservando a fonte clicável.
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

    public async Task<IReadOnlyList<BriefingItem>> CurateAsync(
        IReadOnlyList<Headline> headlines,
        CancellationToken ct = default)
    {
        if (headlines.Count == 0)
        {
            return Array.Empty<BriefingItem>();
        }

        var prompt = BuildPrompt(headlines);

        var requestBody = new
        {
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.4, response_mime_type = "application/json" }
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        using var response = await _http.PostAsJsonAsync(url, requestBody, JsonOpts, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini (briefing) retornou {Status}: {Payload}", response.StatusCode, payload);
            throw new InvalidOperationException($"Falha ao gerar briefing: {response.StatusCode}");
        }

        string? text;
        using (var doc = JsonDocument.Parse(payload))
        {
            text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();
        }

        return ParseSelection(text, headlines);
    }

    /// <summary>Resolve os índices escolhidos pela IA de volta às manchetes originais (com URL).</summary>
    private IReadOnlyList<BriefingItem> ParseSelection(string? json, IReadOnlyList<Headline> headlines)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<BriefingItem>();
        }

        SelectionDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<SelectionDto>(json, JsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Briefing: resposta da IA ilegível: {Json}", json);
            return Array.Empty<BriefingItem>();
        }

        if (dto?.Items is null || dto.Items.Count == 0)
        {
            return Array.Empty<BriefingItem>();
        }

        var result = new List<BriefingItem>();
        var seen = new HashSet<int>();

        foreach (var sel in dto.Items)
        {
            // 'index' é 1-based no prompt; converte e valida.
            var idx = sel.Index - 1;
            if (idx < 0 || idx >= headlines.Count || !seen.Add(idx))
            {
                continue;
            }

            var h = headlines[idx];
            var comment = string.IsNullOrWhiteSpace(sel.Comment) ? h.Title : sel.Comment!.Trim();
            result.Add(new BriefingItem(h.Source, h.Title, h.Url, comment));
        }

        return result;
    }

    private static string BuildPrompt(IReadOnlyList<Headline> headlines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Você é o curador de notícias de um mentor de alta performance.");
        sb.AppendLine("Das manchetes NUMERADAS abaixo, selecione APENAS as que afetam o mercado global de tecnologia,");
        sb.AppendLine("C#/.NET e engenharia de software, economia e geopolítica. Descarte fofoca, entretenimento e ruído.");
        sb.AppendLine("Escolha NO MÁXIMO 5, as mais relevantes. Para cada uma, escreva um comentário de 1 frase em");
        sb.AppendLine("português do Brasil dizendo por que importa. NÃO invente manchetes: use apenas os índices listados.");
        sb.AppendLine();
        sb.AppendLine("Responda ESTRITAMENTE em JSON, sem markdown, no formato:");
        sb.AppendLine("{ \"items\": [ { \"index\": 1, \"comment\": \"por que importa\" } ] }");
        sb.AppendLine();
        sb.AppendLine("MANCHETES:");
        for (var i = 0; i < headlines.Count; i++)
        {
            var h = headlines[i];
            sb.Append(i + 1).Append(". [").Append(h.Source).Append("] ").Append(h.Title);
            if (!string.IsNullOrWhiteSpace(h.Summary))
            {
                sb.Append(" — ").Append(h.Summary);
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private sealed class SelectionDto
    {
        [JsonPropertyName("items")] public List<SelectionItemDto>? Items { get; set; }
    }

    private sealed class SelectionItemDto
    {
        [JsonPropertyName("index")] public int Index { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
    }
}
