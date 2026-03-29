import { type ReactNode } from "react";

type BadgeVariant = "success" | "info" | "warning" | "error" | "neutral" | "primary" | "secondary" | "accent";

interface BadgeProps {
  children: ReactNode;
  variant?: BadgeVariant;
  size?: "xs" | "sm" | "md" | "lg";
  className?: string;
}

const sizeClasses = {
  xs: "badge-xs text-[10px]",
  sm: "badge-sm text-xs",
  md: "badge-md text-sm",
  lg: "badge-lg text-base font-bold",
};

export function Badge({ children, variant = "neutral", size = "md", className = "" }: BadgeProps) {
  return (
    <span className={`badge badge-${variant} ${sizeClasses[size]} ${className}`}>
      {children}
    </span>
  );
}

/** OVR rating badge with tier-based coloring */
export function OvrBadge({ ovr, size = "md" }: { ovr: number; size?: "xs" | "sm" | "md" | "lg" }) {
  const variant: BadgeVariant =
    ovr >= 80 ? "warning" :
    ovr >= 60 ? "info" :
    "neutral";

  return (
    <Badge variant={variant} size={size} className="font-bold tabular-nums">
      {Math.round(ovr)}
    </Badge>
  );
}

/** Match rating badge (SofaScore-style, 0-10) */
export function RatingBadge({ rating, size = "sm" }: { rating: number; size?: "xs" | "sm" | "md" | "lg" }) {
  const bg =
    rating >= 8.0 ? "bg-green-600" :
    rating >= 7.0 ? "bg-green" :
    rating >= 6.5 ? "bg-yellow" :
    rating >= 6.0 ? "bg-orange" :
    "bg-red";

  const sizeClass = size === "xs" ? "text-[10px] px-1" :
                    size === "sm" ? "text-xs px-1.5 py-0.5" :
                    size === "md" ? "text-sm px-2 py-0.5" :
                    "text-base px-2.5 py-1";

  return (
    <span className={`${bg} text-white rounded-lg font-bold tabular-nums ${sizeClass}`}>
      {rating.toFixed(1)}
    </span>
  );
}
