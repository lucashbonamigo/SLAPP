import { useRef, useState } from "react";
import { uploadProof, ApiError } from "../api";
import type { Quest } from "../types";

interface Props {
  quest: Quest;
  onUploaded: () => void;
}

const ATTR_LABEL: Record<Quest["attributeType"], string> = {
  STR: "FOR",
  INT: "INT",
  WIS: "SAB",
  VIT: "VIT",
};

const PROOF_LABEL: Record<Quest["proofType"], string> = {
  STRAVA: "Prova: Strava",
  SUMMARY: "Prova: Resumo",
  MINDMAP: "Prova: Mapa mental",
  PHOTO: "Prova: Foto",
  PAYMENT_RECEIPT: "Prova: Comprovante",
  GENERIC: "Prova: Evidência",
};

function todayIso(): string {
  const d = new Date();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${d.getFullYear()}-${mm}-${dd}`;
}

function formatDate(iso: string): string {
  const t = todayIso();
  if (iso === t) return "Hoje";
  const [y, m, d] = iso.split("-").map(Number);
  const target = new Date(y, m - 1, d);
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  if (
    target.getFullYear() === tomorrow.getFullYear() &&
    target.getMonth() === tomorrow.getMonth() &&
    target.getDate() === tomorrow.getDate()
  ) {
    return "Amanhã";
  }
  return `${String(d).padStart(2, "0")}/${String(m).padStart(2, "0")}`;
}

export default function QuestCard({ quest, onUploaded }: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleFile(file: File) {
    setError(null);
    setUploading(true);
    try {
      await uploadProof(quest.id, file);
      onUploaded();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.status === 403
            ? "Prazo expirado. O Sistema recusou a evidência."
            : err.message
          : "Falha no envio da evidência.";
      setError(message);
    } finally {
      setUploading(false);
      if (inputRef.current) inputRef.current.value = "";
    }
  }

  const statusClass = `quest-status status-${quest.status.toLowerCase()}`;
  const isToday = quest.targetDate === todayIso();
  const isPenalty = quest.kind === "PENALTY";
  const isBonus = quest.kind === "BONUS";

  const kindClass = isPenalty ? "quest-penalty" : isBonus ? "quest-bonus" : "";

  return (
    <div className={`quest ${kindClass}`}>
      <div className="quest-top">
        <span className="attr-tag">{ATTR_LABEL[quest.attributeType]}</span>
        <div className="quest-top-right">
          <span className="quest-date">{formatDate(quest.targetDate)}</span>
          <span className={statusClass}>{quest.status}</span>
        </div>
      </div>

      {isPenalty && (
        <div className="penalty-banner">💀 PENALIDADE DO SISTEMA</div>
      )}

      <h3 className="quest-title">
        {isBonus && <span className="bonus-flag">BÔNUS </span>}
        {quest.title}
      </h3>

      {quest.description && <p className="quest-desc">{quest.description}</p>}

      {quest.metricTarget && (
        <div className="quest-metric">
          <span className="metric-icon">◎</span>
          <span>{quest.metricTarget}</span>
        </div>
      )}

      <div className="quest-meta">
        <span className="meta-chip">{quest.estimatedMinutes} min</span>
        <span className="meta-chip">{PROOF_LABEL[quest.proofType]}</span>
      </div>

      <div className="quest-bottom">
        <span className="xp-reward">+{quest.xpReward} XP</span>

        {quest.hasProof ? (
          <span className="proof-ok">Evidência anexada</span>
        ) : isToday ? (
          <>
            <input
              ref={inputRef}
              type="file"
              accept="image/*"
              hidden
              onChange={(e) => {
                const file = e.target.files?.[0];
                if (file) void handleFile(file);
              }}
            />
            <button
              className="btn btn-ghost"
              onClick={() => inputRef.current?.click()}
              disabled={uploading}
            >
              {uploading ? "Enviando..." : "Anexar evidência"}
            </button>
          </>
        ) : (
          <span className="proof-locked">Disponível na data</span>
        )}
      </div>

      {error && <div className="alert alert-error quest-error">{error}</div>}
    </div>
  );
}
