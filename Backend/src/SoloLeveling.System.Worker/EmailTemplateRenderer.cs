using System.Net;
using System.Text;
using SoloLeveling.System.Core.Domain;
using SoloLeveling.System.Core.Services.Models;

namespace SoloLeveling.System.Worker;

/// <summary>
/// Carrega os templates HTML de e-mail (EmailTemplates/*.html) e preenche os
/// placeholders {{...}} com os dados do veredito. Escolhe o tema conforme o
/// resultado: sucesso (azul) ou fracasso/punição (vermelho).
/// </summary>
public static class EmailTemplateRenderer
{
    private static readonly string TemplateDir =
        Path.Combine(AppContext.BaseDirectory, "EmailTemplates");

    public static string RenderVerdict(User player, EvaluationResult result)
    {
        var templateName = result.IsPunishment ? "failure.html" : "success.html";
        var template = LoadTemplate(templateName);

        return template
            .Replace("{{USERNAME}}", Enc(player.Username))
            .Replace("{{LEVEL}}", player.Level.ToString())
            .Replace("{{XP}}", player.CurrentXp.ToString())
            .Replace("{{XP_DELTA}}", $"{result.TotalXpDelta:+0;-0;0} XP")
            .Replace("{{VERDICT_SUMMARY}}", Enc(result.VerdictSummary))
            .Replace("{{VERDICT_ROWS}}", BuildVerdictRows(result, result.IsPunishment))
            .Replace("{{TOMORROW_ROWS}}", BuildTomorrowRows(result, result.IsPunishment))
            .Replace("{{DEBT_BLOCK}}", BuildDebtBlock(player, result.IsPunishment));
    }

    private static string LoadTemplate(string name)
    {
        var path = Path.Combine(TemplateDir, name);
        return File.ReadAllText(path);
    }

    private static string BuildVerdictRows(EvaluationResult result, bool punishment)
    {
        if (result.Verdicts.Count == 0)
        {
            return EmptyNote("Sem avaliações registradas.", punishment);
        }

        var ok = punishment ? "#f87171" : "#34d399";
        var sb = new StringBuilder();
        foreach (var v in result.Verdicts)
        {
            var color = v.Approved ? "#34d399" : "#f87171";
            var mark = v.Approved ? "&#10003;" : "&#10007;";
            sb.Append(
                $"<div style=\"background:#0a1120;border:1px solid rgba(148,163,184,0.15);border-radius:8px;padding:10px 12px;margin-bottom:8px;\">" +
                $"<span style=\"color:{color};font-weight:700;\">{mark}</span> " +
                $"<span style=\"color:#cbd5e1;font-size:13px;\">Quest {v.QuestId}: " +
                $"<strong style=\"color:{ok};\">{v.XpDelta:+0;-0;0} XP</strong> &mdash; {Enc(v.Comment)}</span>" +
                $"</div>");
        }
        return sb.ToString();
    }

    private static string BuildTomorrowRows(EvaluationResult result, bool punishment)
    {
        if (result.TomorrowQuests.Count == 0)
        {
            return EmptyNote("Nenhuma missão gerada.", punishment);
        }

        var sb = new StringBuilder();
        foreach (var q in result.TomorrowQuests)
        {
            var (border, badge) = q.Kind switch
            {
                QuestKind.PENALTY => ("#dc2626", "<span style=\"background:#dc2626;color:#1a0505;font-size:10px;font-weight:800;letter-spacing:1px;padding:2px 6px;border-radius:4px;\">&#128128; PENALIDADE</span> "),
                QuestKind.BONUS => ("#34d399", "<span style=\"background:#34d399;color:#04140d;font-size:10px;font-weight:800;letter-spacing:1px;padding:2px 6px;border-radius:4px;\">B&Ocirc;NUS</span> "),
                _ => ("rgba(56,189,248,0.3)", string.Empty)
            };

            var metric = string.IsNullOrWhiteSpace(q.MetricTarget)
                ? string.Empty
                : $"<div style=\"color:#94a3b8;font-size:12px;margin-top:4px;\">&#9678; {Enc(q.MetricTarget)}</div>";

            sb.Append(
                $"<div style=\"background:#0a1120;border:1px solid {border};border-radius:8px;padding:12px;margin-bottom:8px;\">" +
                $"<div>{badge}<span style=\"color:#38bdf8;font-size:11px;font-weight:700;\">[{q.Attribute}]</span> " +
                $"<strong style=\"color:#e2e8f0;font-size:14px;\">{Enc(q.Title)}</strong></div>" +
                metric +
                $"<div style=\"color:#64748b;font-size:11px;margin-top:6px;\">{q.XpReward} XP &middot; {q.EstimatedMinutes} min</div>" +
                $"</div>");
        }
        return sb.ToString();
    }

    private static string BuildDebtBlock(User player, bool punishment)
    {
        if (player.DebtAmount <= 0)
        {
            return string.Empty;
        }

        return
            "<tr><td style=\"padding:8px 32px 16px;\">" +
            "<div style=\"background:rgba(220,38,38,0.12);border:1px solid #dc2626;border-radius:10px;padding:16px;\">" +
            "<p style=\"margin:0 0 6px;color:#f87171;font-size:14px;font-weight:800;\">&#128176; DÉBITO DO SISTEMA</p>" +
            $"<p style=\"margin:0;color:#fca5a5;font-size:13px;\">Faltas acumuladas: <strong>{player.FailureCount}</strong> &middot; " +
            $"Dívida: <strong>R$ {player.DebtAmount:0.00}</strong></p>" +
            "<p style=\"margin:8px 0 0;color:#fca5a5;font-size:12px;\">Transfira o valor para uma causa que você despreza e anexe o comprovante na Penalty Quest de pagamento para quitar o débito.</p>" +
            "</div></td></tr>";
    }

    private static string EmptyNote(string text, bool punishment)
    {
        var color = punishment ? "#7f1d1d" : "#64748b";
        return $"<p style=\"color:{color};font-size:13px;font-style:italic;margin:0;\">{Enc(text)}</p>";
    }

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
