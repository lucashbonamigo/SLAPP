using System.Text;
using WebUtility = global::System.Net.WebUtility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoloLeveling.System.Core.Data;
using SoloLeveling.System.Core.Domain;
using SoloLeveling.System.Core.Options;
using SoloLeveling.System.Core.Services;
using SoloLeveling.System.Core.Services.Models;

namespace SoloLeveling.System.Worker;

/// <summary>
/// O Motor do Sistema. Audita as quests do dia, roteia para a IA (recompensa ou punição),
/// atualiza o banco e dispara o veredito por e-mail.
/// </summary>
public class DailyEvaluationService
{
    private readonly SystemDbContext _db;
    private readonly IGeminiEvaluator _evaluator;
    private readonly IProofStorage _storage;
    private readonly IEmailSender _email;
    private readonly WorkerOptions _workerOptions;
    private readonly SystemRulesOptions _rules;
    private readonly ILogger<DailyEvaluationService> _logger;

    public DailyEvaluationService(
        SystemDbContext db,
        IGeminiEvaluator evaluator,
        IProofStorage storage,
        IEmailSender email,
        IOptions<WorkerOptions> workerOptions,
        IOptions<SystemRulesOptions> rules,
        ILogger<DailyEvaluationService> logger)
    {
        _db = db;
        _evaluator = evaluator;
        _storage = storage;
        _email = email;
        _workerOptions = workerOptions.Value;
        _rules = rules.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var userId = _workerOptions.PlayerUserId;
        var today = DateOnly.FromDateTime(DateTime.Now);
        var tomorrow = today.AddDays(1);

        var player = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (player is null)
        {
            _logger.LogError("Jogador {UserId} não encontrado. Auditoria abortada.", userId);
            return;
        }

        var quests = await _db.Quests
            .Include(q => q.Proofs)
            .Where(q => q.UserId == userId && q.TargetDate == today)
            .ToListAsync(ct);

        if (quests.Count == 0)
        {
            _logger.LogInformation("Nenhuma missão para hoje ({Today}). Nada a auditar.", today);
            return;
        }

        // Objetivos ativos: a direção e a memória que a IA usa para fatiar as tarefas.
        var goals = await _db.Goals
            .Where(g => g.UserId == userId && g.Status == GoalStatus.ACTIVE)
            .ToListAsync(ct);

        var goalSummaries = goals
            .Select(g => new GoalSummary(
                g.Id, g.Title, g.AttributeType, g.Category,
                g.TargetMetric, g.ContentBacklog, g.ProgressNotes))
            .ToList();

        // Personalidade atual (Big Five): contexto para a IA ajustar os traços.
        var traits = await _db.PersonalityTraits
            .Where(t => t.UserId == userId)
            .ToListAsync(ct);

        var traitSummaries = traits
            .Select(t => new TraitSummary(t.TraitCode, t.Value))
            .ToList();

        // Contexto de mentor: desempenho dos últimos 7 dias e tendência dos traços.
        var mentor = await BuildMentorContextAsync(userId, traits, today, ct);

        // Reflexão noturna de hoje (o Diário de Piloto Automático), se registrada.
        var reflectionEntity = await _db.DailyReflections
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ReflectionDate == today, ct);

        var reflectionInput = reflectionEntity is null
            ? null
            : new ReflectionInput(reflectionEntity.AutopilotText, reflectionEntity.IdealText);

        // 1. Auditoria: quantas quests do dia estão sem evidência.
        var missing = quests.Count(q => q.Proofs.Count == 0);
        _logger.LogInformation("Auditoria: {Total} quests, {Missing} sem evidência.", quests.Count, missing);

        var summaries = quests
            .Select(q => new QuestSummary(
                q.Id, q.GoalId, q.Title, q.Description, q.MetricTarget,
                q.ProofType, q.AttributeType, q.XpReward, q.Proofs.Count > 0))
            .ToList();

        EvaluationResult result;
        try
        {
            // 2. Roteamento da IA.
            if (missing == 0)
            {
                var proofImages = await LoadProofImagesAsync(quests, ct);
                result = await _evaluator.EvaluateAsync(player, goalSummaries, summaries, proofImages, traitSummaries, mentor, reflectionInput, ct);
            }
            else
            {
                result = await _evaluator.PunishAsync(player, goalSummaries, summaries, traitSummaries, mentor, reflectionInput, ct);
            }

            // 3. Atualização do banco (punição é determinística, não vem da IA).
            await ApplyResultAsync(player, quests, goals, traits, reflectionEntity, result, today, tomorrow, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Falha no julgamento (IA fora, timeout, erro de dados). Não deixa o Sistema
            // morrer calado: avisa o jogador por e-mail e repropaga para o log/exit code.
            _logger.LogError(ex, "Falha ao processar a avaliação diária. Enviando alerta de erro.");
            await SendErrorEmailAsync(player, ex, ct);
            throw;
        }

        // 4. Disparo do veredito por e-mail.
        await SendVerdictEmailAsync(player, result, ct);

        _logger.LogInformation("Auditoria concluída. ΔXP={Delta}, punição={Punish}.", result.TotalXpDelta, result.IsPunishment);
    }

    private async Task<List<ProofImage>> LoadProofImagesAsync(List<Quest> quests, CancellationToken ct)
    {
        var images = new List<ProofImage>();
        foreach (var quest in quests)
        {
            foreach (var proof in quest.Proofs)
            {
                try
                {
                    var bytes = await _storage.ReadAsync(proof.FilePath, ct);
                    images.Add(new ProofImage(quest.Id, proof.FilePath, bytes, GuessMimeType(proof.FilePath)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao ler evidência {Path}.", proof.FilePath);
                }
            }
        }
        return images;
    }

    private async Task ApplyResultAsync(
        User player,
        List<Quest> quests,
        List<Goal> goals,
        List<PersonalityTrait> traits,
        DailyReflection? reflection,
        EvaluationResult result,
        DateOnly today,
        DateOnly tomorrow,
        CancellationToken ct)
    {
        var verdictByQuest = result.Verdicts.ToDictionary(v => v.QuestId);

        var failedCount = 0;
        foreach (var quest in quests)
        {
            if (verdictByQuest.TryGetValue(quest.Id, out var verdict) && verdict.Approved)
            {
                quest.Status = QuestStatus.COMPLETED;
                LevelingService.RewardAttribute(player, quest.AttributeType);
                foreach (var proof in quest.Proofs)
                {
                    proof.AiVerified = true;
                }
            }
            else
            {
                quest.Status = QuestStatus.FAILED;
                failedCount++;
            }
        }

        // Aplica a variação de XP consolidada e recalcula o nível.
        LevelingService.ApplyXp(player, result.TotalXpDelta);

        // Economia de faltas: cada missão reprovada acumula na dívida (determinístico).
        if (failedCount > 0)
        {
            player.FailureCount += failedCount;
            player.DebtAmount += failedCount * _rules.FeePerFailure;
        }

        // Memória: grava o progresso de cada objetivo devolvido pela IA.
        var goalById = goals.ToDictionary(g => g.Id);
        foreach (var progress in result.GoalProgress)
        {
            if (goalById.TryGetValue(progress.GoalId, out var goal))
            {
                goal.ProgressNotes = progress.Note;
            }
        }

        // Métricas (série temporal): grava os dados numéricos extraídos das
        // missões aprovadas. É a matéria-prima dos gráficos de evolução.
        var questById = quests.ToDictionary(q => q.Id);
        foreach (var metric in result.Metrics)
        {
            if (!questById.TryGetValue(metric.QuestId, out var quest))
            {
                continue;
            }

            // Só registra métrica de missão efetivamente aprovada.
            if (!verdictByQuest.TryGetValue(quest.Id, out var v) || !v.Approved)
            {
                continue;
            }

            _db.QuestMetrics.Add(new QuestMetric
            {
                QuestId = quest.Id,
                GoalId = quest.GoalId,
                UserId = player.Id,
                MetricName = metric.MetricName,
                MetricValue = metric.Value,
                Unit = metric.Unit,
                RecordedAt = today,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Personalidade: aplica os deltas de traços (clamp 0-100) e grava o
        // snapshot diário para o radar de evolução.
        ApplyTraitDeltas(player.Id, traits, result.TraitDeltas, today);

        // Mentor: grava a reestruturação cognitiva na reflexão de hoje (se houver).
        if (reflection is not null && !string.IsNullOrWhiteSpace(result.MentorFeedback))
        {
            reflection.MentorFeedback = result.MentorFeedback;
            reflection.UpdatedAt = DateTime.UtcNow;
        }

        // Micro-desafio de personalidade: materializa como uma Quest BONUS avulsa
        // (aparece no dashboard, aceita evidência e passa pela avaliação de amanhã).
        if (result.MicroChallenge is not null)
        {
            var mc = result.MicroChallenge;
            _db.Quests.Add(new Quest
            {
                UserId = player.Id,
                GoalId = null,
                Title = $"[MENTOR] {mc.Title}",
                Description = mc.Description,
                MetricTarget = $"Micro-desafio de {mc.TargetTrait} (até {mc.EstimatedMinutes} min)",
                ProofType = ProofType.GENERIC,
                EstimatedMinutes = Math.Clamp(mc.EstimatedMinutes, 1, _rules.MaxTaskMinutes),
                AttributeType = MapTraitToAttribute(mc.TargetTrait),
                XpReward = 20,
                Status = QuestStatus.PENDING,
                Kind = QuestKind.BONUS,
                IsPenalty = false,
                TargetDate = tomorrow,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Insere as missões normais de amanhã geradas pela IA.
        // A IA NÃO decide punição: qualquer PENALTY vinda dela é ignorada aqui.
        foreach (var generated in result.TomorrowQuests.Where(g => g.Kind != QuestKind.PENALTY))
        {
            _db.Quests.Add(MapToQuest(player.Id, generated, tomorrow));
        }

        // Punição determinística: se houve falha, sorteia UMA punição do catálogo.
        if (failedCount > 0)
        {
            await ApplyPunishmentAsync(player, tomorrow, ct);
        }

        // Evento aleatório: chance diária de uma tarefa bônus extra.
        TryGenerateBonus(player, goals, tomorrow);

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Aplica as variações de traços devolvidas pela IA ao estado atual
    /// (personality_traits, limitado a 0-100) e registra um snapshot do dia em
    /// personality_trait_history. Cria a linha do traço se ela ainda não existir.
    /// </summary>
    private void ApplyTraitDeltas(
        int userId,
        List<PersonalityTrait> traits,
        IReadOnlyList<TraitDelta> deltas,
        DateOnly today)
    {
        if (deltas.Count == 0)
        {
            return;
        }

        var byCode = traits.ToDictionary(t => t.TraitCode);

        foreach (var d in deltas)
        {
            if (!byCode.TryGetValue(d.Trait, out var trait))
            {
                // Traço ainda não semeado: cria a partir do neutro (50).
                trait = new PersonalityTrait
                {
                    UserId = userId,
                    TraitCode = d.Trait,
                    Value = 50m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.PersonalityTraits.Add(trait);
                byCode[d.Trait] = trait;
            }

            var newValue = Math.Clamp(trait.Value + d.Delta, 0m, 100m);
            var appliedDelta = newValue - trait.Value;
            trait.Value = newValue;
            trait.UpdatedAt = DateTime.UtcNow;

            _db.PersonalityTraitHistory.Add(new PersonalityTraitHistory
            {
                UserId = userId,
                TraitCode = d.Trait,
                Value = newValue,
                Delta = appliedDelta,
                RecordedAt = today,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "Traço {Trait}: {Delta:+0.0;-0.0} -> {Value:0.0}. Motivo: {Reason}",
                d.Trait, appliedDelta, newValue, d.Reason ?? "(sem motivo)");
        }
    }

    /// <summary>
    /// Monta o contexto estratégico do mentor: taxa de sucesso dos últimos 7 dias
    /// (missões já julgadas) e a tendência de cada traço (soma dos deltas da semana).
    /// A fraqueza da semana é o traço com pior situação: para NEUROTICISM, valor
    /// alto é ruim; para os demais, valor baixo é ruim.
    /// </summary>
    private async Task<MentorContext> BuildMentorContextAsync(
        int userId,
        List<PersonalityTrait> traits,
        DateOnly today,
        CancellationToken ct)
    {
        var weekAgo = today.AddDays(-7);

        var recent = await _db.Quests
            .Where(q => q.UserId == userId
                && q.TargetDate >= weekAgo && q.TargetDate < today
                && (q.Status == QuestStatus.COMPLETED || q.Status == QuestStatus.FAILED))
            .Select(q => q.Status)
            .ToListAsync(ct);

        var completed = recent.Count(s => s == QuestStatus.COMPLETED);
        var failed = recent.Count(s => s == QuestStatus.FAILED);
        var total = completed + failed;
        var successRate = total > 0 ? (double)completed / total * 100.0 : 0.0;

        // Tendência: soma dos deltas por traço na janela de 7 dias.
        var deltas = await _db.PersonalityTraitHistory
            .Where(h => h.UserId == userId && h.RecordedAt >= weekAgo)
            .GroupBy(h => h.TraitCode)
            .Select(g => new { Trait = g.Key, Sum = g.Sum(x => x.Delta) })
            .ToListAsync(ct);

        var deltaByTrait = deltas.ToDictionary(d => d.Trait, d => d.Sum);
        var valueByTrait = traits.ToDictionary(t => t.TraitCode, t => t.Value);

        var trends = valueByTrait
            .Select(kv => new TraitTrend(
                kv.Key,
                kv.Value,
                deltaByTrait.TryGetValue(kv.Key, out var d) ? d : 0m))
            .ToList();

        // Fraqueza: maior "distância do ideal". Ideal de NEUROTICISM é baixo.
        BigFiveTrait? weakest = null;
        decimal worstGap = decimal.MinValue;
        foreach (var t in trends)
        {
            var gap = t.Trait == BigFiveTrait.NEUROTICISM ? t.Current : 100m - t.Current;
            if (gap > worstGap)
            {
                worstGap = gap;
                weakest = t.Trait;
            }
        }

        return new MentorContext(successRate, completed, failed, trends, weakest);
    }

    /// <summary>Mapeia um traço Big Five para o atributo de ficha mais próximo.</summary>
    private static AttributeType MapTraitToAttribute(BigFiveTrait trait) => trait switch
    {
        BigFiveTrait.CONSCIENTIOUSNESS => AttributeType.WIS,
        BigFiveTrait.NEUROTICISM => AttributeType.VIT,
        BigFiveTrait.OPENNESS => AttributeType.INT,
        BigFiveTrait.EXTRAVERSION => AttributeType.WIS,
        BigFiveTrait.AGREEABLENESS => AttributeType.WIS,
        _ => AttributeType.VIT
    };

    /// <summary>
    /// Sorteia uma punição ativa do catálogo (ponderada por Weight) e materializa
    /// uma Penalty Quest a partir dela. A punição financeira tem peso menor, então
    /// não sai sempre. A decisão é do código, não da IA.
    /// </summary>
    private async Task ApplyPunishmentAsync(User player, DateOnly tomorrow, CancellationToken ct)
    {
        var catalog = await _db.Punishments
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        if (catalog.Count == 0)
        {
            _logger.LogWarning("Catálogo de punições vazio. Nenhuma Penalty Quest gerada.");
            return;
        }

        var chosen = DrawWeighted(catalog);

        // A punição financeira cobra a dívida acumulada; descreve o valor.
        var description = chosen.Category == PunishmentCategory.FINANCEIRO
            ? $"{chosen.Description} Valor devido: R$ {player.DebtAmount:0.00}."
            : chosen.Description;

        _logger.LogInformation("Punição sorteada: {Code} ({Category}).", chosen.Code, chosen.Category);

        _db.Quests.Add(new Quest
        {
            UserId = player.Id,
            GoalId = null,
            Title = chosen.Title,
            Description = description,
            MetricTarget = null,
            ProofType = chosen.ProofType,
            EstimatedMinutes = Math.Clamp(chosen.EstimatedMinutes, 1, _rules.MaxTaskMinutes),
            AttributeType = chosen.AttributeType,
            XpReward = chosen.XpReward,
            Status = QuestStatus.PENDING,
            Kind = QuestKind.PENALTY,
            IsPenalty = true,
            TargetDate = tomorrow,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>Sorteio ponderado por Weight.</summary>
    private static Punishment DrawWeighted(List<Punishment> catalog)
    {
        var total = catalog.Sum(p => Math.Max(1, p.Weight));
        var roll = Random.Shared.Next(total);
        var acc = 0;
        foreach (var p in catalog)
        {
            acc += Math.Max(1, p.Weight);
            if (roll < acc)
            {
                return p;
            }
        }
        return catalog[^1];
    }

    private Quest MapToQuest(int userId, GeneratedQuest g, DateOnly date)
    {
        return new Quest
        {
            UserId = userId,
            GoalId = g.GoalId,
            Title = g.Title,
            Description = g.Description,
            MetricTarget = g.MetricTarget,
            ProofType = g.ProofType,
            EstimatedMinutes = Math.Clamp(g.EstimatedMinutes, 1, _rules.MaxTaskMinutes),
            AttributeType = g.Attribute,
            XpReward = g.XpReward,
            Status = QuestStatus.PENDING,
            Kind = g.Kind,
            IsPenalty = g.Kind == QuestKind.PENALTY,
            TargetDate = date,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Rola o dado do evento aleatório e, se acertar, cria uma tarefa bônus.</summary>
    private void TryGenerateBonus(User player, List<Goal> goals, DateOnly tomorrow)
    {
        if (_rules.BonusChancePercent <= 0)
        {
            return;
        }

        var roll = Random.Shared.Next(1, 101); // 1..100
        if (roll > _rules.BonusChancePercent)
        {
            return;
        }

        // Alinha o bônus a um objetivo ativo quando houver, senão é um desafio de disciplina.
        var goal = goals.Count > 0 ? goals[Random.Shared.Next(goals.Count)] : null;
        var attribute = goal?.AttributeType ?? AttributeType.VIT;

        _logger.LogInformation("Evento aleatório acionado (roll={Roll} <= {Chance}%). Tarefa bônus gerada.",
            roll, _rules.BonusChancePercent);

        _db.Quests.Add(new Quest
        {
            UserId = player.Id,
            GoalId = goal?.Id,
            Title = "[EVENTO] Desafio Surpresa do Sistema",
            Description = goal is null
                ? "Um gatilho aleatório do Sistema. Cumpra um desafio extra de disciplina de até 30 min."
                : $"Gatilho aleatório ligado ao objetivo '{goal.Title}'. Avance um passo extra hoje.",
            MetricTarget = "Desafio bônus de até 30 min",
            ProofType = ProofType.GENERIC,
            EstimatedMinutes = 30,
            AttributeType = attribute,
            XpReward = 25,
            Status = QuestStatus.PENDING,
            Kind = QuestKind.BONUS,
            IsPenalty = false,
            TargetDate = tomorrow,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task SendVerdictEmailAsync(User player, EvaluationResult result, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(player.Email))
        {
            _logger.LogWarning("Jogador sem e-mail cadastrado. Veredito não enviado.");
            return;
        }

        var subject = result.IsPunishment
            ? "[O SISTEMA] Penalidade Aplicada"
            : "[O SISTEMA] Veredito Diário";

        string body;
        try
        {
            body = EmailTemplateRenderer.RenderVerdict(player, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao renderizar o template de e-mail. Usando corpo simples.");
            body = $"<p>{WebUtility.HtmlEncode(result.VerdictSummary)}</p>";
        }

        try
        {
            await _email.SendAsync(player.Email, subject, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar o veredito por e-mail.");
        }
    }

    /// <summary>
    /// Envia um alerta quando a auditoria falha (IA fora do ar, timeout, erro de dados).
    /// Mantém o Sistema transparente mesmo quando não consegue julgar.
    /// </summary>
    private async Task SendErrorEmailAsync(User player, Exception error, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(player.Email))
        {
            _logger.LogWarning("Jogador sem e-mail cadastrado. Alerta de erro não enviado.");
            return;
        }

        var when = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var sb = new StringBuilder();
        sb.Append("<h2>[O SISTEMA] Falha na auditoria</h2>");
        sb.Append($"<p><strong>Jogador:</strong> {WebUtility.HtmlEncode(player.Username)}</p>");
        sb.Append($"<p><strong>Quando:</strong> {when}</p>");
        sb.Append("<p>O Sistema não conseguiu concluir o julgamento das missões de hoje. ");
        sb.Append("Nenhum XP foi alterado e nenhuma missão de amanhã foi gerada nesta execução.</p>");
        sb.Append($"<p><strong>Detalhe técnico:</strong> {WebUtility.HtmlEncode(error.Message)}</p>");
        sb.Append("<p>Verifique o serviço e rode a auditoria novamente.</p>");

        try
        {
            await _email.SendAsync(player.Email, "[O SISTEMA] Falha na Auditoria Diária", sb.ToString(), ct);
            _logger.LogInformation("Alerta de erro enviado para {Email}.", player.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar o alerta de erro por e-mail.");
        }
    }

    private static string GuessMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
    }
}

