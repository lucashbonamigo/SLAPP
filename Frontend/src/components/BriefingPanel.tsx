import { useEffect, useState } from "react";
import { getTodayBriefing, ApiError } from "../api";
import type { Briefing } from "../types";

/** Converte o markdown simples (bullets "- ") do resumo numa lista. */
function parseBullets(md: string): string[] {
  return md
    .split("\n")
    .map((l) => l.trim())
    .filter((l) => l.length > 0)
    .map((l) => l.replace(/^[-*•]\s*/, ""));
}

export default function BriefingPanel() {
  const [briefing, setBriefing] = useState<Briefing | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const b = await getTodayBriefing();
        if (active) setBriefing(b);
      } catch (err) {
        if (active) {
          setError(err instanceof ApiError ? err.message : "Falha ao carregar o briefing.");
        }
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
  }, []);

  if (loading) {
    return <p className="empty">Carregando o briefing...</p>;
  }

  if (error) {
    return <div className="alert alert-error">{error}</div>;
  }

  if (!briefing) {
    return (
      <p className="empty">
        O briefing de hoje ainda não foi gerado. O Sistema o prepara de manhã.
      </p>
    );
  }

  const bullets = parseBullets(briefing.summaryMd);

  return (
    <div className="briefing">
      <div className="briefing-meta">
        <span>{briefing.briefingDate}</span>
        <span>{briefing.sourceCount} fontes</span>
      </div>
      {bullets.length > 0 ? (
        <ul className="briefing-list">
          {bullets.map((b, i) => (
            <li key={i}>{b}</li>
          ))}
        </ul>
      ) : (
        <p className="briefing-raw">{briefing.summaryMd}</p>
      )}
    </div>
  );
}
