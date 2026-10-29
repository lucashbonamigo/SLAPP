import type { PlayerStatus } from "../types";

interface Props {
  player: PlayerStatus;
}

export default function PlayerCard({ player }: Props) {
  const totalForLevel = player.currentXp + player.xpToNextLevel;
  const progress =
    totalForLevel > 0 ? (player.currentXp / totalForLevel) * 100 : 0;

  const stats: Array<{ label: string; value: number; hint: string }> = [
    { label: "FOR", value: player.attributes.for, hint: "Força / Físico" },
    { label: "INT", value: player.attributes.int, hint: "Inteligência / Estudos" },
    { label: "SAB", value: player.attributes.sab, hint: "Sabedoria / Disciplina" },
    { label: "VIT", value: player.attributes.vit, hint: "Vitalidade / Resistência" },
  ];

  return (
    <section className="panel player-card">
      <div className="player-header">
        <div>
          <span className="eyebrow">Ficha do Jogador</span>
          <h2>{player.username}</h2>
        </div>
        <div className="level-badge">
          <span className="level-number">{player.level}</span>
          <span className="level-label">NÍVEL</span>
        </div>
      </div>

      <div className="xp-row">
        <div className="xp-bar">
          <div className="xp-fill" style={{ width: `${progress}%` }} />
        </div>
        <span className="xp-text">
          {player.currentXp} XP · faltam {player.xpToNextLevel} para o próximo nível
        </span>
      </div>

      <div className="stats-grid">
        {stats.map((s) => (
          <div className="stat" key={s.label} title={s.hint}>
            <span className="stat-label">{s.label}</span>
            <span className="stat-value">{s.value}</span>
          </div>
        ))}
      </div>

      <p className="deadline">
        Prazo inflexível do Sistema: <strong>{player.dailyDeadline}</strong>
      </p>
    </section>
  );
}
