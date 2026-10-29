interface Props {
  size?: "small" | "large";
}

/**
 * Marca "SISTEMA" — tipografia imponente, sem ornamentos.
 */
export default function SystemLogo({ size = "large" }: Props) {
  const isLarge = size === "large";
  return (
    <div className={`system-logo ${isLarge ? "system-logo-lg" : "system-logo-sm"}`}>
      <span className="system-logo-text">SISTEMA</span>
    </div>
  );
}
