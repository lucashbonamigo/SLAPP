import { useCallback, useEffect, useState, type FormEvent } from "react";
import { getGoals, createGoal, updateGoal, deleteGoal, ApiError } from "../api";
import { LogoutIcon } from "./icons";
import type {
  Goal,
  GoalInput,
  GoalCategory,
  GoalStatus,
  AttributeType,
} from "../types";

interface Props {
  onLogout: () => void;
  onBack: () => void;
}

const CATEGORIES: GoalCategory[] = ["RUNNING", "STUDY", "READING", "GENERIC"];
const STATUSES: GoalStatus[] = ["ACTIVE", "COMPLETED", "PAUSED"];
const ATTRIBUTES: AttributeType[] = ["STR", "INT", "WIS", "VIT"];

function todayIso(): string {
  const d = new Date();
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const dd = String(d.getDate()).padStart(2, "0");
  return `${d.getFullYear()}-${mm}-${dd}`;
}

const EMPTY: GoalInput = {
  title: "",
  description: null,
  attributeType: "STR",
  category: "GENERIC",
  targetMetric: null,
  contentBacklog: null,
  status: "ACTIVE",
  progressNotes: null,
  startDate: todayIso(),
  targetDate: null,
};

const STATUS_LABEL: Record<GoalStatus, string> = {
  ACTIVE: "Ativo",
  COMPLETED: "Concluído",
  PAUSED: "Pausado",
};

export default function Goals({ onLogout, onBack }: Props) {
  const [items, setItems] = useState<Goal[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<GoalInput>(EMPTY);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setItems(await getGoals());
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        onLogout();
        return;
      }
      setError(err instanceof ApiError ? err.message : "Falha ao carregar objetivos.");
    } finally {
      setLoading(false);
    }
  }, [onLogout]);

  useEffect(() => {
    void load();
  }, [load]);

  function startCreate() {
    setEditingId(null);
    setForm({ ...EMPTY, startDate: todayIso() });
    setFormError(null);
  }

  function startEdit(g: Goal) {
    setEditingId(g.id);
    const { id: _id, ...rest } = g;
    setForm(rest);
    setFormError(null);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setFormError(null);
    try {
      if (editingId === null) {
        await createGoal(form);
      } else {
        await updateGoal(editingId, form);
      }
      startCreate();
      await load();
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Falha ao salvar.");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(g: Goal) {
    if (!confirm(`Remover o objetivo "${g.title}"? As tarefas ligadas a ele ficam sem vínculo.`))
      return;
    try {
      await deleteGoal(g.id);
      if (editingId === g.id) startCreate();
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Falha ao remover.");
    }
  }

  function set<K extends keyof GoalInput>(key: K, value: GoalInput[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  return (
    <div className="dashboard">
      <header className="topbar">
        <span className="system-title small">◎ OBJETIVOS</span>
        <div className="topbar-actions">
          <button className="btn btn-ghost" onClick={onBack}>
            Voltar
          </button>
          <button
            className="btn btn-icon btn-icon-danger"
            onClick={onLogout}
            title="Sair"
            aria-label="Sair"
          >
            <LogoutIcon />
          </button>
        </div>
      </header>

      {error && <div className="alert alert-error">{error}</div>}

      <div className="content">
        <section className="panel">
          <h2 className="form-title">
            {editingId === null ? "Novo objetivo" : "Editar objetivo"}
          </h2>
          <form className="punish-form" onSubmit={handleSubmit}>
            <div className="form-grid">
              <label className="field field-wide">
                <span>Título</span>
                <input
                  value={form.title}
                  onChange={(e) => set("title", e.target.value)}
                  placeholder="Correr uma meia maratona"
                  required
                />
              </label>

              <label className="field">
                <span>Categoria</span>
                <select
                  value={form.category}
                  onChange={(e) => set("category", e.target.value as GoalCategory)}
                >
                  {CATEGORIES.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              </label>

              <label className="field">
                <span>Atributo</span>
                <select
                  value={form.attributeType}
                  onChange={(e) => set("attributeType", e.target.value as AttributeType)}
                >
                  {ATTRIBUTES.map((a) => (
                    <option key={a} value={a}>
                      {a}
                    </option>
                  ))}
                </select>
              </label>

              <label className="field field-wide">
                <span>Meta mensurável</span>
                <input
                  value={form.targetMetric ?? ""}
                  onChange={(e) => set("targetMetric", e.target.value || null)}
                  placeholder="21.1 km em menos de 2h"
                />
              </label>

              <label className="field field-wide">
                <span>Backlog de conteúdo (etapas que a IA vai fatiando)</span>
                <textarea
                  value={form.contentBacklog ?? ""}
                  onChange={(e) => set("contentBacklog", e.target.value || null)}
                  rows={3}
                  placeholder="Plano progressivo: base 5km -> 8km -> 12km..."
                />
              </label>

              <label className="field field-wide">
                <span>Descrição (opcional)</span>
                <textarea
                  value={form.description ?? ""}
                  onChange={(e) => set("description", e.target.value || null)}
                  rows={2}
                />
              </label>

              <label className="field">
                <span>Status</span>
                <select
                  value={form.status}
                  onChange={(e) => set("status", e.target.value as GoalStatus)}
                >
                  {STATUSES.map((s) => (
                    <option key={s} value={s}>
                      {STATUS_LABEL[s]}
                    </option>
                  ))}
                </select>
              </label>

              <label className="field">
                <span>Início</span>
                <input
                  type="date"
                  value={form.startDate}
                  onChange={(e) => set("startDate", e.target.value)}
                />
              </label>

              <label className="field">
                <span>Prazo (opcional)</span>
                <input
                  type="date"
                  value={form.targetDate ?? ""}
                  onChange={(e) => set("targetDate", e.target.value || null)}
                />
              </label>

              <label className="field field-wide">
                <span>Progresso (editável; a IA também atualiza)</span>
                <textarea
                  value={form.progressNotes ?? ""}
                  onChange={(e) => set("progressNotes", e.target.value || null)}
                  rows={2}
                  placeholder="(a IA preenche a cada avaliação)"
                />
              </label>
            </div>

            {formError && <div className="alert alert-error">{formError}</div>}

            <div className="form-actions">
              {editingId !== null && (
                <button type="button" className="btn btn-ghost" onClick={startCreate}>
                  Cancelar edição
                </button>
              )}
              <button type="submit" className="btn btn-primary form-submit" disabled={saving}>
                {saving ? "Salvando..." : editingId === null ? "Criar objetivo" : "Salvar alterações"}
              </button>
            </div>
          </form>
        </section>

        <section className="panel">
          <div className="quests-header">
            <h2>Meus objetivos</h2>
            <span className="pending-count">{items.length} objetivos</span>
          </div>

          {loading ? (
            <div className="loading">Carregando...</div>
          ) : items.length === 0 ? (
            <p className="empty">Nenhum objetivo cadastrado.</p>
          ) : (
            <div className="quests-list">
              {items.map((g) => (
                <div
                  key={g.id}
                  className={`punish-row ${g.status !== "ACTIVE" ? "punish-inactive" : ""}`}
                >
                  <div className="punish-main">
                    <div className="punish-head">
                      <span className="attr-tag">{g.category}</span>
                      <strong>{g.title}</strong>
                      <span className="punish-off">{STATUS_LABEL[g.status]}</span>
                    </div>
                    {g.targetMetric && (
                      <div className="quest-metric">
                        <span className="metric-icon">◎</span>
                        <span>{g.targetMetric}</span>
                      </div>
                    )}
                    {g.progressNotes && (
                      <p className="quest-desc">
                        <strong>Progresso:</strong> {g.progressNotes}
                      </p>
                    )}
                    <div className="quest-meta">
                      <span className="meta-chip">{g.attributeType}</span>
                      <span className="meta-chip">início {g.startDate}</span>
                      {g.targetDate && <span className="meta-chip">prazo {g.targetDate}</span>}
                    </div>
                  </div>
                  <div className="punish-actions">
                    <button className="btn btn-ghost" onClick={() => startEdit(g)}>
                      Editar
                    </button>
                    <button
                      className="btn btn-ghost btn-danger"
                      onClick={() => void handleDelete(g)}
                    >
                      Remover
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
