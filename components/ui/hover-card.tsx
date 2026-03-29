"use client";

import { type ReactNode } from "react";
import { useTilt } from "@/lib/use-tilt";

interface HoverCard3DProps {
  children: ReactNode;
  /** Accent color for top border */
  accent?: string;
  onClick?: () => void;
  className?: string;
  /** Disable the 3D effect */
  disabled?: boolean;
}

/**
 * DaisyUI-style card with real 3D mouse-tracking tilt on hover.
 * Used for training cards, halftime cards, club selection, etc.
 */
export function HoverCard3D({
  children,
  accent,
  onClick,
  className = "",
  disabled = false,
}: HoverCard3DProps) {
  const { ref, handlers } = useTilt(12, 1.03);
  const tiltHandlers = disabled ? {} : handlers;

  const borderTop = accent ? `border-t-4` : "";
  const borderColor = accent ? { borderTopColor: accent } : {};

  return (
    <div
      ref={disabled ? undefined : ref}
      {...tiltHandlers}
      className={`card-3d card bg-white shadow-lg rounded-2xl overflow-hidden
        ${borderTop}
        ${onClick ? "cursor-pointer active:scale-[0.98]" : ""}
        ${disabled ? "opacity-50 pointer-events-none" : ""}
        ${className}`}
      style={borderColor}
      onClick={onClick}
    >
      <div className="card-body p-5">
        {children}
      </div>
    </div>
  );
}
