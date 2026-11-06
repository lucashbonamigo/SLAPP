import { useEffect, useState } from "react";
import { getTodayReflection, saveReflection, ApiError } from "../api";
import type { Reflection } from "../types";

interface Props {
  /** Chamado após salvar, para o Dashboard recarregar se quiser. */
  onSaved?: () => void;
}

export default function ReflectionPanel({ onSaved }: Props) {
  const [reflection, setReflection] = useState<Reflection | null>(null);
  const [autopilot, setAutopilot] = useState("");
  const [ideal, setIdeal] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [savedAt, setSavedAt] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const r = await getTodayReflection();
        if (!active) return;
        setReflection(r);
        setAutopilot(r?.autopilotText ?? "");
        setIdeal(r?.idealText ?? "");
      } catch (err) {
        if (active) {
          setError(err instanceof ApiError ? err.message : "Falha ao carregar a reflexão.");
        }
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => {
      active = false;
    };
  }, []);

  async function handleSave() {
    if (autopilot.trim().length < 3) {
      setError("Descreva como você reagiu no piloto automático.");
      return;
    }
    setError(null);
    setSaving(true);
    try {
      const saved = await saveReflection({
        autopilotText: autopilot.trim(),
        idealText: ideal.trim() || undefined,
      });
      setReflection(saved);
      setSavedAt(new Date().toLocaleTimeString());
      onSaved?.();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Falha ao salvar a reflexão.");
    } finally {
      setSaving(false);
    }
  }

  if (loading) {
    return <p className="empty">Carregando a reflexão de hoje...</p>;
  }

  return (
    <div className="reflection">
      {reflection?.mentorFeedback && (
        <div className="mentor-feedback">
          <span className="mentor-feedback-label">Feedback do Mentor</span>
          <p>{reflection.mentorFeedback}</p>
        </div>
      )}

      <p className="reflection-intro">
        Antes de dormir, registre a verdade do dia. O Mentor vai analisar na
        avaliação noturna.
      </p>

      <label className="reflection-field">
        <span>Como você reagiu no piloto automático hoje?</span>
        <textarea
          value={autopilot}
          onChange={(e) => setAutopilot(e.target.value)}
          rows={4}
          placeholder="Diante do estresse/desafio, qual foi sua reação automática?"
        />
      </label>

      <label className="reflection-field">
        <span>Como o "Eu Nível 100" teria reagido? (opcional)</span>
        <textarea
          value={ideal}
          onChange={(e) => setIdeal(e.target.value)}
          rows={3}
          placeholder="A resposta deliberada que você gostaria de ter tido."
        />
      </label>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="reflection-actions">
        <button className="btn" onClick={() => void handleSave()} disabled={saving}>
          {saving ? "Salvando..." : reflection ? "Atualizar reflexão" : "Registrar reflexão"}
        </button>
        {savedAt && <span className="reflection-saved">Salvo às {savedAt}</span>}
      </div>
    </div>
  );
}
