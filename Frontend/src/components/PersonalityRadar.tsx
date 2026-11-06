import { useMemo } from "react";
import type { BigFiveTrait, Trait } from "../types";

interface Props {
  traits: Trait[];
}

/** Ordem fixa dos eixos do pentágono e rótulos curtos em PT. */
const AXES: Array<{ code: BigFiveTrait; label: string; hint: string }> = [
  { code: "OPENNESS", label: "Abertura", hint: "Curiosidade / criatividade" },
  { code: "CONSCIENTIOUSNESS", label: "Disciplina", hint: "Conscienciosidade / foco" },
  { code: "EXTRAVERSION", label: "Assertiv.", hint: "Extroversão / exposição" },
  { code: "AGREEABLENESS", label: "Amabilid.", hint: "Cooperação / empatia" },
  { code: "NEUROTICISM", label: "Estresse", hint: "Neuroticismo (menor é melhor)" },
];

const SIZE = 260;
const CENTER = SIZE / 2;
const RADIUS = 96;
const RINGS = [0.25, 0.5, 0.75, 1];

/** Converte (índice do eixo, valor 0-100) numa coordenada x/y no SVG. */
function point(index: number, value0to100: number): { x: number; y: number } {
  const ratio = Math.max(0, Math.min(100, value0to100)) / 100;
  // -90° deixa o primeiro eixo apontando para cima.
  const angle = (Math.PI * 2 * index) / AXES.length - Math.PI / 2;
  return {
    x: CENTER + Math.cos(angle) * RADIUS * ratio,
    y: CENTER + Math.sin(angle) * RADIUS * ratio,
  };
}

function polygon(values: number[]): string {
  return values.map((v, i) => {
    const p = point(i, v);
    return `${p.x.toFixed(1)},${p.y.toFixed(1)}`;
  }).join(" ");
}

export default function PersonalityRadar({ traits }: Props) {
  const byCode = useMemo(() => {
    const map = new Map<BigFiveTrait, number>();
    for (const t of traits) map.set(t.trait, t.value);
    return map;
  }, [traits]);

  // Valores atuais na ordem dos eixos (faltando => 50 neutro).
  const current = AXES.map((a) => byCode.get(a.code) ?? 50);
  // Silhueta "Nível 100": alvo ideal. Neuroticismo ideal é baixo (20).
  const target = AXES.map((a) => (a.code === "NEUROTICISM" ? 20 : 85));

  return (
    <div className="radar">
      <svg
        viewBox={`0 0 ${SIZE} ${SIZE}`}
        role="img"
        aria-label="Gráfico de radar da personalidade (Big Five)"
      >
        {/* anéis de referência */}
        {RINGS.map((r) => (
          <polygon
            key={r}
            className="radar-ring"
            points={polygon(AXES.map(() => r * 100))}
          />
        ))}

        {/* eixos */}
        {AXES.map((_, i) => {
          const p = point(i, 100);
          return (
            <line
              key={i}
              className="radar-axis"
              x1={CENTER}
              y1={CENTER}
              x2={p.x}
              y2={p.y}
            />
          );
        })}

        {/* silhueta alvo (Nível 100) */}
        <polygon className="radar-target" points={polygon(target)} />

        {/* personalidade atual */}
        <polygon className="radar-current" points={polygon(current)} />

        {/* vértices atuais */}
        {current.map((v, i) => {
          const p = point(i, v);
          return <circle key={i} className="radar-dot" cx={p.x} cy={p.y} r={3} />;
        })}

        {/* rótulos dos eixos */}
        {AXES.map((a, i) => {
          const p = point(i, 118);
          return (
            <text
              key={a.code}
              className="radar-label"
              x={p.x}
              y={p.y}
              textAnchor="middle"
              dominantBaseline="middle"
            >
              {a.label}
            </text>
          );
        })}
      </svg>

      <ul className="radar-legend">
        {AXES.map((a, i) => (
          <li key={a.code} title={a.hint}>
            <span className="radar-legend-name">{a.label}</span>
            <span className="radar-legend-value">{Math.round(current[i])}</span>
          </li>
        ))}
      </ul>
      <p className="radar-note">
        Contorno tracejado = alvo "Nível 100". Em <strong>Estresse</strong>, menor é melhor.
      </p>
    </div>
  );
}
