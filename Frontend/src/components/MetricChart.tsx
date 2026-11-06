import { useMemo, useState } from "react";
import type { MetricSeries } from "../types";

interface Props {
  series: MetricSeries[];
}

const WIDTH = 480;
const HEIGHT = 180;
const PAD = { top: 16, right: 16, bottom: 28, left: 40 };

/** Rótulos amigáveis para os nomes canônicos de métrica. */
const METRIC_LABELS: Record<string, string> = {
  distancia_km: "Distância (km)",
  pace_medio: "Pace médio (min/km)",
  tempo_min: "Tempo (min)",
  bpm_medio: "FC média (bpm)",
  foco_min: "Foco (min)",
  topicos: "Tópicos",
  paginas: "Páginas",
};

function labelFor(name: string, unit: string | null): string {
  return METRIC_LABELS[name] ?? (unit ? `${name} (${unit})` : name);
}

export default function MetricChart({ series }: Props) {
  const [selected, setSelected] = useState<string>(
    () => series[0]?.metricName ?? ""
  );

  const active = useMemo(
    () => series.find((s) => s.metricName === selected) ?? series[0],
    [series, selected]
  );

  const geometry = useMemo(() => {
    if (!active || active.points.length === 0) return null;

    const values = active.points.map((p) => p.value);
    const min = Math.min(...values);
    const max = Math.max(...values);
    const span = max - min || 1;
    const n = active.points.length;

    const innerW = WIDTH - PAD.left - PAD.right;
    const innerH = HEIGHT - PAD.top - PAD.bottom;

    const x = (i: number) =>
      PAD.left + (n === 1 ? innerW / 2 : (innerW * i) / (n - 1));
    const y = (v: number) => PAD.top + innerH - ((v - min) / span) * innerH;

    const coords = active.points.map((p, i) => ({
      x: x(i),
      y: y(p.value),
      value: p.value,
      date: p.recordedAt,
    }));

    const line = coords.map((c, i) => `${i === 0 ? "M" : "L"}${c.x.toFixed(1)},${c.y.toFixed(1)}`).join(" ");
    const area = `${line} L${coords[coords.length - 1].x.toFixed(1)},${(HEIGHT - PAD.bottom).toFixed(1)} L${coords[0].x.toFixed(1)},${(HEIGHT - PAD.bottom).toFixed(1)} Z`;

    return { coords, line, area, min, max };
  }, [active]);

  if (!active) {
    return <p className="empty">Nenhuma métrica registrada ainda.</p>;
  }

  return (
    <div className="metric-chart">
      <div className="metric-tabs">
        {series.map((s) => (
          <button
            key={s.metricName}
            className={`metric-tab ${s.metricName === active.metricName ? "metric-tab-active" : ""}`}
            onClick={() => setSelected(s.metricName)}
          >
            {labelFor(s.metricName, s.unit)}
          </button>
        ))}
      </div>

      {!geometry ? (
        <p className="empty">Sem pontos para esta métrica.</p>
      ) : (
        <svg
          viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
          role="img"
          aria-label={`Série temporal de ${labelFor(active.metricName, active.unit)}`}
        >
          {/* eixos de referência (min/max) */}
          <line className="chart-grid" x1={PAD.left} y1={PAD.top} x2={PAD.left} y2={HEIGHT - PAD.bottom} />
          <line className="chart-grid" x1={PAD.left} y1={HEIGHT - PAD.bottom} x2={WIDTH - PAD.right} y2={HEIGHT - PAD.bottom} />

          <text className="chart-axis-label" x={PAD.left - 6} y={PAD.top + 4} textAnchor="end">
            {geometry.max}
          </text>
          <text className="chart-axis-label" x={PAD.left - 6} y={HEIGHT - PAD.bottom} textAnchor="end">
            {geometry.min}
          </text>

          <path className="chart-area" d={geometry.area} />
          <path className="chart-line" d={geometry.line} />

          {geometry.coords.map((c, i) => (
            <circle key={i} className="chart-dot" cx={c.x} cy={c.y} r={3}>
              <title>{`${c.date}: ${c.value}${active.unit ? " " + active.unit : ""}`}</title>
            </circle>
          ))}
        </svg>
      )}
    </div>
  );
}
