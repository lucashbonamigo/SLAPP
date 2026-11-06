using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Domain;
using SoloLeveling.System.Core.Options;
using SoloLeveling.System.Core.Services.Models;

namespace SoloLeveling.System.Core.Services;

/// <summary>
/// Implementação do "Sistema" usando a API Gemini (Generative Language) do Google.
/// Gera tarefas mensuráveis alinhadas aos objetivos do jogador, avalia evidências
/// (imagens em Base64) e devolve veredito + progresso dos objetivos + metas de amanhã.
/// </summary>
public class GeminiEvaluator : IGeminiEvaluator
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly SystemRulesOptions _rules;
    private readonly ILogger<GeminiEvaluator> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public GeminiEvaluator(
        HttpClient http,
        IOptions<GeminiOptions> options,
        IOptions<SystemRulesOptions> rules,
        ILogger<GeminiEvaluator> logger)
    {
        _http = http;
        _options = options.Value;
        _rules = rules.Value;
        _logger = logger;
    }

    public async Task<EvaluationResult> EvaluateAsync(
        User player,
        IReadOnlyList<GoalSummary> goals,
        IReadOnlyList<QuestSummary> quests,
        IReadOnlyList<ProofImage> proofs,
        IReadOnlyList<TraitSummary> traits,
        CancellationToken ct = default)
    {
        var prompt = BuildEvaluationPrompt(player, goals, quests, traits);

        var parts = new List<object> { new { text = prompt } };
        foreach (var proof in proofs)
        {
            parts.Add(new
            {
                inline_data = new
                {
                    mime_type = proof.MimeType,
                    data = Convert.ToBase64String(proof.Content)
                }
            });
        }

        var raw = await CallGeminiAsync(parts, ct);
        return ParseEvaluation(raw, quests, isPunishment: false);
    }

    public async Task<EvaluationResult> PunishAsync(
        User player,
        IReadOnlyList<GoalSummary> goals,
        IReadOnlyList<QuestSummary> quests,
        IReadOnlyList<TraitSummary> traits,
        CancellationToken ct = default)
    {
        var prompt = BuildPunishmentPrompt(player, goals, quests, traits);
        var parts = new List<object> { new { text = prompt } };

        var raw = await CallGeminiAsync(parts, ct);
        return ParseEvaluation(raw, quests, isPunishment: true);
    }

    private async Task<string> CallGeminiAsync(List<object> parts, CancellationToken ct)
    {
        var requestBody = new
        {
            contents = new[] { new { role = "user", parts } },
            generationConfig = new
            {
                temperature = 0.7,
                response_mime_type = "application/json"
            }
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        using var response = await _http.PostAsJsonAsync(url, requestBody, JsonOpts, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini retornou {Status}: {Payload}", response.StatusCode, payload);
            throw new InvalidOperationException($"Falha ao chamar Gemini: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(payload);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "{}";
    }

    private EvaluationResult ParseEvaluation(string json, IReadOnlyList<QuestSummary> quests, bool isPunishment)
    {
        GeminiResponseDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<GeminiResponseDto>(json, JsonOpts);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Não foi possível interpretar a resposta da IA: {Json}", json);
            dto = null;
        }

        if (dto is null)
        {
            // Fallback seguro: na dúvida, o Sistema não recompensa.
            return new EvaluationResult(
                quests.Select(q => new QuestVerdict(q.QuestId, false, 0, "Resposta da IA ilegível.")).ToList(),
                Array.Empty<GeneratedQuest>(),
                Array.Empty<GoalProgress>(),
                Array.Empty<QuestMetricReport>(),
                Array.Empty<TraitDelta>(),
                0,
                0,
                "O Sistema não conseguiu emitir um veredito confiável.",
                isPunishment);
        }

        var verdicts = (dto.Verdicts ?? new List<VerdictDto>())
            .Select(v => new QuestVerdict(v.QuestId, v.Approved, v.XpDelta, v.Comment ?? string.Empty))
            .ToList();

        var tomorrow = (dto.TomorrowQuests ?? new List<GeneratedQuestDto>())
            .Select(MapGeneratedQuest)
            .ToList();

        var goalProgress = (dto.GoalProgress ?? new List<GoalProgressDto>())
            .Where(g => g.GoalId > 0 && !string.IsNullOrWhiteSpace(g.Note))
            .Select(g => new GoalProgress(g.GoalId, g.Note!))
            .ToList();

        var metrics = (dto.Metrics ?? new List<MetricDto>())
            .Where(m => m.QuestId > 0 && !string.IsNullOrWhiteSpace(m.MetricName))
            .Select(m => new QuestMetricReport(m.QuestId, m.MetricName!.Trim(), m.Value, m.Unit))
            .ToList();

        var traitDeltas = (dto.TraitDeltas ?? new List<TraitDeltaDto>())
            .Where(t => t.Delta != 0 && Enum.TryParse<BigFiveTrait>(t.Trait, true, out _))
            .Select(t => new TraitDelta(Enum.Parse<BigFiveTrait>(t.Trait!, true), t.Delta, t.Reason))
            .ToList();

        var totalDelta = verdicts.Sum(v => v.XpDelta);

        return new EvaluationResult(
            verdicts,
            tomorrow,
            goalProgress,
            metrics,
            traitDeltas,
            totalDelta,
            dto.FailuresToCharge ?? 0,
            dto.VerdictSummary ?? string.Empty,
            isPunishment);
    }

    private GeneratedQuest MapGeneratedQuest(GeneratedQuestDto g)
    {
        var attr = Enum.TryParse<AttributeType>(g.Attribute, true, out var a) ? a : AttributeType.VIT;
        var proof = Enum.TryParse<ProofType>(g.ProofType, true, out var p) ? p : ProofType.GENERIC;
        var kind = Enum.TryParse<QuestKind>(g.Kind, true, out var k) ? k : QuestKind.NORMAL;

        // Garante o teto de duração mesmo que a IA exagere.
        var minutes = g.EstimatedMinutes ?? _rules.MaxTaskMinutes;
        if (minutes <= 0 || minutes > _rules.MaxTaskMinutes)
        {
            minutes = _rules.MaxTaskMinutes;
        }

        return new GeneratedQuest(
            g.GoalId,
            g.Title ?? "Missão sem título",
            g.Description,
            g.MetricTarget,
            proof,
            minutes,
            attr,
            g.XpReward ?? 10,
            kind);
    }

    // ---------------------------------------------------------------------
    // Prompts
    // ---------------------------------------------------------------------

    private string BuildEvaluationPrompt(
        User player,
        IReadOnlyList<GoalSummary> goals,
        IReadOnlyList<QuestSummary> quests,
        IReadOnlyList<TraitSummary> traits)
    {
        var sb = new StringBuilder();
        AppendPersona(sb);
        sb.AppendLine("TAREFA: avalie as EVIDÊNCIAS (imagens) de cada missão de hoje e gere as missões de amanhã.");
        sb.AppendLine();
        AppendPlayer(sb, player);
        AppendPersonality(sb, traits);
        AppendGoals(sb, goals);
        AppendTodayQuests(sb, quests);
        AppendEvaluationRules(sb);
        AppendMetricRules(sb);
        AppendPersonalityRules(sb);
        AppendGenerationRules(sb);
        AppendJsonContract(sb);
        return sb.ToString();
    }

    private string BuildPunishmentPrompt(
        User player,
        IReadOnlyList<GoalSummary> goals,
        IReadOnlyList<QuestSummary> quests,
        IReadOnlyList<TraitSummary> traits)
    {
        var missing = quests.Where(q => !q.HasProof).ToList();
        var sb = new StringBuilder();
        AppendPersona(sb);
        sb.AppendLine("TAREFA: o Jogador FALHOU — deixou missões sem evidência até o prazo.");
        sb.AppendLine("Reprove as missões não cumpridas e gere as missões NORMAIS de amanhã (o próximo passo de cada objetivo).");
        sb.AppendLine("NÃO gere punições: a punição é aplicada pelo próprio Sistema, fora da sua resposta. NUNCA use kind=PENALTY.");
        sb.AppendLine();
        AppendPlayer(sb, player);
        AppendPersonality(sb, traits);
        AppendGoals(sb, goals);

        sb.AppendLine("MISSÕES NÃO CUMPRIDAS (reprove todas, xpDelta negativo):");
        foreach (var q in missing)
        {
            sb.AppendLine($"- questId={q.QuestId} | [{q.Attribute}] {q.Title}");
        }
        sb.AppendLine();

        AppendPersonalityRules(sb);
        AppendGenerationRules(sb);
        AppendJsonContract(sb);
        return sb.ToString();
    }

    private static void AppendPersona(StringBuilder sb)
    {
        sb.AppendLine("Você é 'O Sistema' de Solo Leveling: frio, analítico, implacável. Nunca aceita desculpas.");
        sb.AppendLine("Seu papel é substituir a força de vontade por metas objetivas, mensuráveis e verificáveis.");
        sb.AppendLine();
    }

    private static void AppendPlayer(StringBuilder sb, User player)
    {
        sb.AppendLine($"JOGADOR: {player.Username} | Nível {player.Level} | XP atual {player.CurrentXp}");
        sb.AppendLine($"ATRIBUTOS: FOR={player.StatStr} INT={player.StatInt} SAB={player.StatWis} VIT={player.StatVit}");
        sb.AppendLine($"DÍVIDA ATUAL: R$ {player.DebtAmount:0.00} | FALTAS: {player.FailureCount}");
        sb.AppendLine();
    }

    private static void AppendPersonality(StringBuilder sb, IReadOnlyList<TraitSummary> traits)
    {
        sb.AppendLine("PERSONALIDADE ATUAL (Big Five / OCEAN, escala 0-100, 50=neutro):");
        if (traits.Count == 0)
        {
            sb.AppendLine("- (ainda sem medição; trate como 50 em todos os fatores)");
        }
        else
        {
            foreach (var t in traits)
            {
                sb.AppendLine($"- {t.Trait} = {t.Value:0.0} {DescribeTrait(t.Trait)}");
            }
        }
        sb.AppendLine("Observação: em NEUROTICISM, valor MENOR é melhor (mais resiliência/mente fria).");
        sb.AppendLine();
    }

    private static string DescribeTrait(BigFiveTrait trait) => trait switch
    {
        BigFiveTrait.OPENNESS => "(abertura/curiosidade)",
        BigFiveTrait.CONSCIENTIOUSNESS => "(disciplina/foco)",
        BigFiveTrait.EXTRAVERSION => "(assertividade/exposição)",
        BigFiveTrait.AGREEABLENESS => "(cooperação/empatia)",
        BigFiveTrait.NEUROTICISM => "(reatividade ao estresse)",
        _ => string.Empty
    };

    private static void AppendGoals(StringBuilder sb, IReadOnlyList<GoalSummary> goals)
    {
        sb.AppendLine("OBJETIVOS ATIVOS (gere o PRÓXIMO PASSO de cada um, elevando a dificuldade gradualmente):");
        if (goals.Count == 0)
        {
            sb.AppendLine("- (nenhum objetivo ativo; gere tarefas genéricas de disciplina)");
        }
        foreach (var g in goals)
        {
            sb.AppendLine($"- goalId={g.GoalId} | [{g.Attribute}/{g.Category}] {g.Title}");
            sb.AppendLine($"    meta final: {g.TargetMetric}");
            if (!string.IsNullOrWhiteSpace(g.ContentBacklog))
                sb.AppendLine($"    backlog: {g.ContentBacklog}");
            if (!string.IsNullOrWhiteSpace(g.ProgressNotes))
                sb.AppendLine($"    progresso até agora: {g.ProgressNotes}");
            else
                sb.AppendLine("    progresso até agora: (nenhum; esta é a primeira tarefa deste objetivo)");
        }
        sb.AppendLine();
    }

    private static void AppendTodayQuests(StringBuilder sb, IReadOnlyList<QuestSummary> quests)
    {
        sb.AppendLine("MISSÕES DE HOJE A AVALIAR:");
        foreach (var q in quests)
        {
            sb.AppendLine($"- questId={q.QuestId} | goalId={q.GoalId?.ToString() ?? "null"} | [{q.Attribute}] {q.Title}");
            sb.AppendLine($"    meta exigida: {q.MetricTarget ?? "(livre)"} | prova esperada: {q.ProofType} | temEvidencia={q.HasProof}");
        }
        sb.AppendLine();
    }

    private static void AppendEvaluationRules(StringBuilder sb)
    {
        sb.AppendLine("CRITÉRIOS DE APROVAÇÃO (seja rigoroso, reprove o que for raso ou não comprovar a meta):");
        sb.AppendLine("- STRAVA: a imagem deve comprovar distância E tempo/pace da meta. Abaixo da meta = reprovado.");
        sb.AppendLine("- SUMMARY/MINDMAP: o resumo/mapa deve demonstrar compreensão real do conteúdo. Raso/genérico = reprovado.");
        sb.AppendLine("- PHOTO (resumo de leitura): deve fazer sentido e refletir a leitura. Vago = reprovado.");
        sb.AppendLine("- PAYMENT_RECEIPT: deve ser um comprovante de pagamento/doação legível.");
        sb.AppendLine("Aprovado => xpDelta positivo (use a recompensa base). Reprovado => xpDelta <= 0.");
        sb.AppendLine();
    }

    private static void AppendMetricRules(StringBuilder sb)
    {
        sb.AppendLine("EXTRAÇÃO DE MÉTRICAS (o Motor de 1% ao dia):");
        sb.AppendLine("- Para CADA missão APROVADA, extraia da evidência os dados numéricos objetivos e devolva em 'metrics'.");
        sb.AppendLine("- Use nomes de métrica canônicos e consistentes entre dias (snake_case), para permitir gráficos de série temporal:");
        sb.AppendLine("    RUNNING: distancia_km, pace_medio (min/km em decimal, ex 5.30 = 5:30), tempo_min, bpm_medio.");
        sb.AppendLine("    STUDY: foco_min (minutos de estudo focado), topicos (quantidade).");
        sb.AppendLine("    READING: paginas, foco_min.");
        sb.AppendLine("- Só reporte métricas que a evidência realmente comprova. Não invente números.");
        sb.AppendLine();
    }

    private static void AppendPersonalityRules(StringBuilder sb)
    {
        sb.AppendLine("AJUSTE DE PERSONALIDADE (Big Five):");
        sb.AppendLine("- Com base no comportamento comprovado hoje, devolva variações pequenas em 'traitDeltas' (tipicamente entre -3 e +3).");
        sb.AppendLine("- Cumprir tarefas difíceis sob pressão sobe CONSCIENTIOUSNESS; agir com calma diante de estresse ABAIXA NEUROTICISM.");
        sb.AppendLine("- Expor-se (falar/escrever em público, inglês) sobe EXTRAVERSION; explorar conteúdo novo sobe OPENNESS.");
        sb.AppendLine("- Falhas repetidas, procrastinação ou reação emocional descontrolada SOBEM NEUROTICISM e ABAIXAM CONSCIENTIOUSNESS.");
        sb.AppendLine("- Mude apenas os traços efetivamente tocados pelo dia; os demais podem ter delta 0 ou ficar de fora.");
        sb.AppendLine();
    }

    private void AppendGenerationRules(StringBuilder sb)
    {
        sb.AppendLine("REGRAS PARA GERAR AS MISSÕES DE AMANHÃ:");
        sb.AppendLine($"- Gere {_rules.DailyTaskCount} missões normais (kind=NORMAL), UMA por objetivo ativo, cada uma avançando o progresso daquele objetivo.");
        sb.AppendLine($"- Cada missão deve ter 'estimatedMinutes' realista e NUNCA acima de {_rules.MaxTaskMinutes} minutos.");
        sb.AppendLine("- Cada missão DEVE ter 'metricTarget' concreto e mensurável:");
        sb.AppendLine("    RUNNING: distância + pace + tempo-limite (ex: '6 km a 5:30/km em até 33 min'). proofType=STRAVA.");
        sb.AppendLine("    STUDY: conteúdo específico + duração (ex: 'Estudar Micro-frontends: Module Federation por 50 min'). proofType=SUMMARY ou MINDMAP.");
        sb.AppendLine("    READING: material específico recomendado + duração (ex: 'Ler artigo X sobre consistency models, 45 min'). proofType=PHOTO.");
        sb.AppendLine("- Preencha 'goalId' de cada missão com o objetivo correspondente.");
        sb.AppendLine("- Em 'goalProgress', escreva para cada objetivo uma nota curta do avanço e o que vem a seguir (isto é a memória do Sistema).");
        sb.AppendLine();
    }

    private void AppendJsonContract(StringBuilder sb)
    {
        sb.AppendLine("Responda ESTRITAMENTE em JSON, sem markdown, no formato:");
        sb.AppendLine("""
        {
          "verdictSummary": "veredito curto e frio do Sistema",
          "failuresToCharge": 0,
          "verdicts": [
            { "questId": 1, "approved": true, "xpDelta": 15, "comment": "motivo objetivo" }
          ],
          "goalProgress": [
            { "goalId": 1, "note": "avanço e próximo passo" }
          ],
          "metrics": [
            { "questId": 1, "metricName": "distancia_km", "value": 6.0, "unit": "km" },
            { "questId": 1, "metricName": "pace_medio", "value": 5.30, "unit": "min/km" }
          ],
          "traitDeltas": [
            { "trait": "CONSCIENTIOUSNESS", "delta": 1.5, "reason": "cumpriu a meta sob pressão" }
          ],
          "tomorrowQuests": [
            {
              "goalId": 1,
              "title": "título objetivo",
              "description": "instrução clara",
              "metricTarget": "meta mensurável (distância/pace/tempo ou conteúdo/duração)",
              "proofType": "STRAVA|SUMMARY|MINDMAP|PHOTO|PAYMENT_RECEIPT|GENERIC",
              "estimatedMinutes": 50,
              "attribute": "STR|INT|WIS|VIT",
              "xpReward": 15,
              "kind": "NORMAL|PENALTY|BONUS"
            }
          ]
        }
        """);
    }

    // ---------------------------------------------------------------------
    // DTOs de resposta
    // ---------------------------------------------------------------------

    private sealed class GeminiResponseDto
    {
        [JsonPropertyName("verdictSummary")] public string? VerdictSummary { get; set; }
        [JsonPropertyName("failuresToCharge")] public int? FailuresToCharge { get; set; }
        [JsonPropertyName("verdicts")] public List<VerdictDto>? Verdicts { get; set; }
        [JsonPropertyName("goalProgress")] public List<GoalProgressDto>? GoalProgress { get; set; }
        [JsonPropertyName("metrics")] public List<MetricDto>? Metrics { get; set; }
        [JsonPropertyName("traitDeltas")] public List<TraitDeltaDto>? TraitDeltas { get; set; }
        [JsonPropertyName("tomorrowQuests")] public List<GeneratedQuestDto>? TomorrowQuests { get; set; }
    }

    private sealed class MetricDto
    {
        [JsonPropertyName("questId")] public int QuestId { get; set; }
        [JsonPropertyName("metricName")] public string? MetricName { get; set; }
        [JsonPropertyName("value")] public decimal Value { get; set; }
        [JsonPropertyName("unit")] public string? Unit { get; set; }
    }

    private sealed class TraitDeltaDto
    {
        [JsonPropertyName("trait")] public string? Trait { get; set; }
        [JsonPropertyName("delta")] public decimal Delta { get; set; }
        [JsonPropertyName("reason")] public string? Reason { get; set; }
    }

    private sealed class VerdictDto
    {
        [JsonPropertyName("questId")] public int QuestId { get; set; }
        [JsonPropertyName("approved")] public bool Approved { get; set; }
        [JsonPropertyName("xpDelta")] public int XpDelta { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
    }

    private sealed class GoalProgressDto
    {
        [JsonPropertyName("goalId")] public int GoalId { get; set; }
        [JsonPropertyName("note")] public string? Note { get; set; }
    }

    private sealed class GeneratedQuestDto
    {
        [JsonPropertyName("goalId")] public int? GoalId { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("metricTarget")] public string? MetricTarget { get; set; }
        [JsonPropertyName("proofType")] public string? ProofType { get; set; }
        [JsonPropertyName("estimatedMinutes")] public int? EstimatedMinutes { get; set; }
        [JsonPropertyName("attribute")] public string? Attribute { get; set; }
        [JsonPropertyName("xpReward")] public int? XpReward { get; set; }
        [JsonPropertyName("kind")] public string? Kind { get; set; }
    }
}
