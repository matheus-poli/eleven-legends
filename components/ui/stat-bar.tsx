/** Colored stat bar for player attributes (0-100) */
export function StatBar({ label, value }: { label: string; value: number }) {
  const color =
    value >= 80 ? "bg-green" :
    value >= 60 ? "bg-blue" :
    value >= 40 ? "bg-yellow" :
    "bg-red";

  return (
    <div className="flex items-center gap-1.5">
      <span className="text-xs text-base-content/50 w-8 shrink-0">{label}</span>
      <div className="flex-1 bg-base-200 rounded-full h-1.5">
        <div className={`${color} h-1.5 rounded-full`} style={{ width: `${value}%` }} />
      </div>
      <span className={`text-xs font-medium tabular-nums w-6 text-right ${
        value >= 80 ? "text-green" : value >= 60 ? "text-blue" : value >= 40 ? "text-yellow" : "text-red"
      }`}>
        {value}
      </span>
    </div>
  );
}

/** Compact horizontal stat chip (for transfer cards etc.) */
export function StatChip({ label, value }: { label: string; value: number }) {
  const color =
    value >= 80 ? "text-green" :
    value >= 60 ? "text-blue" :
    value >= 40 ? "text-yellow" :
    "text-red";

  return (
    <span className={`text-[11px] font-medium ${color}`}>
      {label} {value}
    </span>
  );
}
