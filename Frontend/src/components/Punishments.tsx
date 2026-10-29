import { useCallback, useEffect, useState, type FormEvent } from "react";
import {
  getPunishments,
  createPunishment,
  updatePunishment,
  deletePunishment,
  ApiError,
} from "../api";
import type {
  Punishment,
  PunishmentInput,
  PunishmentCategory,
  ProofType,
  AttributeType,
} from "../types";
import { LogoutIcon } from "./icons";

interface Props {
  onLogout: () => void;
  onBack: () => void;
}

const CATEGORIES: PunishmentCategory[] = ["FISICO", "ROTINA", "FINANCEIRO"];
const PROOF_TYPES: ProofType[] = [
  "STRAVA",
  "SUMMARY",
  "MINDMAP",
  "PHOTO",
  "PAYMENT_RECEIPT",
  "GENERIC",
];
const ATTRIBUTES: AttributeType[] = ["STR", "INT", "WIS", "VIT"];

const EMPTY: PunishmentInput = {
  code: "",
  category: "FISICO",
  title: "",
  description: "",
  proofType: "GENERIC",
  estimatedMinutes: 45,
  xpReward: 100,
  attributeType: "VIT",
  weight: 10,
  isActive: true,
};

export default function Punishments({ onLogout, onBack }: Props) {
  const [items, setItems] = useState<Punishment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<PunishmentInput>(EMPTY);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setItems(await getPunishments());
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        onLogout();
        return;
      }
      setError(err instanceof ApiError ? err.message : "Falha ao carregar punições.");
    } finally {
      setLoading(false);
    }
  }, [onLogout]);

  useEffect(() => {
    void load();
  }, [load]);

  function startCreate() {
    setEditingId(null);
    setForm(EMPTY);
    setFormError(null);
  }

  function startEdit(p: Punishment) {
    setEditingId(p.id);
    const { id: _id, ...rest } = p;
    setForm(rest);
    setFormError(null);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setFormError(null);
    try {
      if (editingId === null) {
        await createPunishment(form);
      } else {
        await updatePunishment(editingId, form);
      }
      startCreate();
      await load();
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : "Falha ao salvar.");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(p: Punishment) {
    if (!confirm(`Remover a punição "${p.title}"?`)) return;
    try {
      await deletePunishment(p.id);
      if (editingId === p.id) startCreate();
      await load();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Falha ao remover.");
    }
  }

  function set<K extends keyof PunishmentInput>(key: K, value: PunishmentInput[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  return (
    <div className="dashboard">
      <header className="topbar">
        <span className="system-title small">&#128128; PUNIÇÕES</span>
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
            {editingId === null ? "Nova punição" : "Editar punição"}
          </h2>
          <form className="punish-form" onSubmit={handleSubmit}>
            <div className="form-grid">
              <label className="field">
                <span>Código</span>
                <input
                  value={form.code}
                  onChange={(e) => set("code", e.target.value)}
                  placeholder="TAXA_MADRUGADA"
                  required
                />
              </label>

              <label className="field">
                <span>Categoria</span>
                <select
                  value={form.category}
                  onChange={(e) => set("category", e.target.value as PunishmentCategory)}
                >
                  {CATEGORIES.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              </label>

              <label className="field field-wide">
                <span>Título</span>
                <input
                  value={form.title}
                  onChange={(e) => set("title", e.target.value)}
                  placeholder="A Taxa da Madrugada"
                  required
                />
              </label>

              <label className="field field-wide">
                <span>Descrição (prescritiva: o que, quanto, como comprovar)</span>
                <textarea
                  value={form.description}
                  onChange={(e) => set("description", e.target.value)}
                  rows={3}
                  required
                />
              </label>

              <label className="field">
                <span>Tipo de prova</span>
                <select
                  value={form.proofType}
                  onChange={(e) => set("proofType", e.target.value as ProofType)}
                >
                  {PROOF_TYPES.map((p) => (
                    <option key={p} value={p}>
                      {p}
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

              <label className="field">
                <span>Duração (min)</span>
                <input
                  type="number"
                  min={1}
                  max={480}
                  value={form.estimatedMinutes}
                  onChange={(e) => set("estimatedMinutes", Number(e.target.value))}
                />
              </label>

              <label className="field">
                <span>XP</span>
                <input
                  type="number"
                  min={0}
                  value={form.xpReward}
                  onChange={(e) => set("xpReward", Number(e.target.value))}
                />
              </label>

              <label className="field">
                <span>Peso no sorteio</span>
                <input
                  type="number"
                  min={1}
                  max={1000}
                  value={form.weight}
                  onChange={(e) => set("weight", Number(e.target.value))}
                />
              </label>

              <label className="field checkbox-field">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(e) => set("isActive", e.target.checked)}
                />
                <span>Ativa (entra no sorteio)</span>
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
                {saving ? "Salvando..." : editingId === null ? "Criar punição" : "Salvar alterações"}
              </button>
            </div>
          </form>
        </section>

        <section className="panel">
          <div className="quests-header">
            <h2>Catálogo</h2>
            <span className="pending-count">{items.length} punições</span>
          </div>

          {loading ? (
            <div className="loading">Carregando...</div>
          ) : items.length === 0 ? (
            <p className="empty">Nenhuma punição cadastrada.</p>
          ) : (
            <div className="quests-list">
              {items.map((p) => (
                <div key={p.id} className={`punish-row ${p.isActive ? "" : "punish-inactive"}`}>
                  <div className="punish-main">
                    <div className="punish-head">
                      <span className="attr-tag">{p.category}</span>
                      <strong>{p.title}</strong>
                      {!p.isActive && <span className="punish-off">inativa</span>}
                    </div>
                    <p className="quest-desc">{p.description}</p>
                    <div className="quest-meta">
                      <span className="meta-chip">peso {p.weight}</span>
                      <span className="meta-chip">{p.estimatedMinutes} min</span>
                      <span className="meta-chip">{p.xpReward} XP</span>
                      <span className="meta-chip">{p.proofType}</span>
                      <span className="meta-chip">{p.attributeType}</span>
                    </div>
                  </div>
                  <div className="punish-actions">
                    <button className="btn btn-ghost" onClick={() => startEdit(p)}>
                      Editar
                    </button>
                    <button className="btn btn-ghost btn-danger" onClick={() => void handleDelete(p)}>
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
