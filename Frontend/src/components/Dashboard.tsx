import { useCallback, useEffect, useMemo, useState } from "react";
import {
  getPlayerStatus,
  getAllQuests,
  getPersonality,
  getMetrics,
  clearToken,
  ApiError,
} from "../api";
import type {
  PlayerStatus,
  Quest,
  PersonalityResponse,
  MetricsResponse,
} from "../types";
import PlayerCard from "./PlayerCard";
import QuestCard from "./QuestCard";
import PersonalityRadar from "./PersonalityRadar";
import MetricChart from "./MetricChart";
import ReflectionPanel from "./ReflectionPanel";
import BriefingPanel from "./BriefingPanel";
import SystemLogo from "./SystemLogo";
import { RefreshIcon, TargetIcon, SkullIcon, LogoutIcon } from "./icons";

interface Props {
  onLogout: () => void;
  onManagePunishments: () => void;
  onManageGoals: () => void;
}

type Tab = "pending" | "done" | "evolution";

export default function Dashboard({ onLogout, onManagePunishments, onManageGoals }: Props) {
  const [player, setPlayer] = useState<PlayerStatus | null>(null);
  const [quests, setQuests] = useState<Quest[]>([]);
  const [personality, setPersonality] = useState<PersonalityResponse | null>(null);
  const [metrics, setMetrics] = useState<MetricsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>("pending");

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [p, q, pers, met] = await Promise.all([
        getPlayerStatus(),
        getAllQuests(),
        getPersonality(),
        getMetrics(),
      ]);
      setPlayer(p);
      setQuests(q);
      setPersonality(pers);
      setMetrics(met);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        onLogout();
        return;
      }
      setError(
        err instanceof ApiError ? err.message : "Falha ao carregar os dados do Sistema."
      );
    } finally {
      setLoading(false);
    }
  }, [onLogout]);

  useEffect(() => {
    void load();
  }, [load]);

  function handleLogout() {
    clearToken();
    onLogout();
  }

  const pendingQuests = useMemo(
    () => quests.filter((q) => q.status === "PENDING"),
    [quests]
  );
  const doneQuests = useMemo(
    () => quests.filter((q) => q.status === "COMPLETED" || q.status === "FAILED"),
    [quests]
  );

  const visible = tab === "pending" ? pendingQuests : doneQuests;
  const hasPersonality = (personality?.traits.length ?? 0) > 0;
  const hasMetrics = (metrics?.series.length ?? 0) > 0;

  return (
    <div className="dashboard">
      <header className="topbar">
        <SystemLogo size="small" />
        <div className="topbar-actions">
          <button
            className="btn btn-icon"
            onClick={() => void load()}
            disabled={loading}
            title="Atualizar"
            aria-label="Atualizar"
          >
            <RefreshIcon />
          </button>
          <button
            className="btn btn-icon"
            onClick={onManageGoals}
            title="Objetivos"
            aria-label="Objetivos"
          >
            <TargetIcon />
          </button>
          <button
            className="btn btn-icon"
            onClick={onManagePunishments}
            title="Punições"
            aria-label="Punições"
          >
            <SkullIcon />
          </button>
          <button
            className="btn btn-icon btn-icon-danger"
            onClick={handleLogout}
            title="Sair"
            aria-label="Sair"
          >
            <LogoutIcon />
          </button>
        </div>
      </header>

      {error && <div className="alert alert-error">{error}</div>}

      {loading && !player ? (
        <div className="loading">Carregando a ficha...</div>
      ) : (
        <div className="content">
          {player && <PlayerCard player={player} />}

          <section className="panel quests-panel">
            <div className="tabs">
              <button
                className={`tab ${tab === "pending" ? "tab-active" : ""}`}
                onClick={() => setTab("pending")}
              >
                Pendentes
                <span className="tab-count">{pendingQuests.length}</span>
              </button>
              <button
                className={`tab ${tab === "done" ? "tab-active" : ""}`}
                onClick={() => setTab("done")}
              >
                Concluídas
                <span className="tab-count">{doneQuests.length}</span>
              </button>
              <button
                className={`tab ${tab === "evolution" ? "tab-active" : ""}`}
                onClick={() => setTab("evolution")}
              >
                Evolução
              </button>
            </div>

            {tab === "evolution" ? (
              <div className="evolution">
                <div className="evolution-block">
                  <h3 className="evolution-title">Briefing do Dia</h3>
                  <BriefingPanel />
                </div>

                <div className="evolution-block">
                  <h3 className="evolution-title">Reflexão Noturna</h3>
                  <ReflectionPanel onSaved={() => void load()} />
                </div>

                <div className="evolution-block">
                  <h3 className="evolution-title">Personalidade (Big Five)</h3>
                  {hasPersonality ? (
                    <PersonalityRadar traits={personality!.traits} />
                  ) : (
                    <p className="empty">
                      Sem dados de personalidade ainda. O Sistema ajusta seus traços
                      a cada avaliação noturna.
                    </p>
                  )}
                </div>

                <div className="evolution-block">
                  <h3 className="evolution-title">Métricas (Motor de 1% ao dia)</h3>
                  {hasMetrics ? (
                    <MetricChart series={metrics!.series} />
                  ) : (
                    <p className="empty">
                      Sem métricas registradas ainda. Cumpra missões para o Sistema
                      extrair seus números (pace, foco, distância).
                    </p>
                  )}
                </div>
              </div>
            ) : visible.length === 0 ? (
              <p className="empty">
                {tab === "pending"
                  ? "Nenhuma missão pendente."
                  : "Nenhuma missão concluída ainda."}
              </p>
            ) : (
              <div className="quests-list">
                {visible.map((q) => (
                  <QuestCard key={q.id} quest={q} onUploaded={() => void load()} />
                ))}
              </div>
            )}
          </section>
        </div>
      )}
    </div>
  );
}
