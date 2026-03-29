/** Format money amount for display */
export function formatMoney(amount: number): string {
  if (Math.abs(amount) >= 1_000_000) return `${(amount / 1_000_000).toFixed(1)}M`;
  if (Math.abs(amount) >= 1_000) return `${Math.round(amount / 1_000)}K`;
  return `${Math.round(amount)}`;
}

/** Money display with color coding */
export function Money({ amount, showSign = false }: { amount: number; showSign?: boolean }) {
  const color = amount >= 0 ? "text-green" : "text-red";
  const sign = showSign && amount > 0 ? "+" : "";

  return <span className={`font-medium tabular-nums ${color}`}>{sign}{formatMoney(amount)}</span>;
}
