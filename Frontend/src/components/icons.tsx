// Ícones SVG inline — sem dependências externas. herdam a cor via currentColor.

interface IconProps {
  size?: number;
}

export function RefreshIcon({ size = 18 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M21 12a9 9 0 1 1-2.64-6.36" />
      <path d="M21 3v6h-6" />
    </svg>
  );
}

export function TargetIcon({ size = 18 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="9" />
      <circle cx="12" cy="12" r="5" />
      <circle cx="12" cy="12" r="1.5" fill="currentColor" />
    </svg>
  );
}

export function SkullIcon({ size = 18 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 2a8 8 0 0 0-8 8c0 2.5 1.2 4.3 3 5.5V19a1 1 0 0 0 1 1h8a1 1 0 0 0 1-1v-3.5c1.8-1.2 3-3 3-5.5a8 8 0 0 0-8-8Z" />
      <circle cx="9" cy="10.5" r="1.6" fill="currentColor" stroke="none" />
      <circle cx="15" cy="10.5" r="1.6" fill="currentColor" stroke="none" />
      <path d="M12 14v2" />
      <path d="M9 20v-2" />
      <path d="M12 20v-2" />
      <path d="M15 20v-2" />
    </svg>
  );
}

export function LogoutIcon({ size = 18 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none"
      stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
      <path d="M16 17l5-5-5-5" />
      <path d="M21 12H9" />
    </svg>
  );
}
