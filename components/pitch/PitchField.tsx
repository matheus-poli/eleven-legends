"use client";

import { type ReactNode } from "react";

/** Green football pitch background with field markings via CSS */
export function PitchField({
  children,
  className = "",
}: {
  children?: ReactNode;
  className?: string;
}) {
  return (
    <div className={`pitch-bg relative rounded-lg overflow-hidden ${className}`}>
      {/* Field markings overlay */}
      <svg
        className="absolute inset-0 w-full h-full pointer-events-none"
        viewBox="0 0 100 100"
        preserveAspectRatio="none"
      >
        {/* Border */}
        <rect x="1" y="1" width="98" height="98" fill="none" stroke="rgba(255,255,255,0.2)" strokeWidth="0.5" />

        {/* Center line */}
        <line x1="0" y1="50" x2="100" y2="50" stroke="rgba(255,255,255,0.2)" strokeWidth="0.4" />

        {/* Center circle */}
        <circle cx="50" cy="50" r="10" fill="none" stroke="rgba(255,255,255,0.2)" strokeWidth="0.4" />
        <circle cx="50" cy="50" r="0.8" fill="rgba(255,255,255,0.2)" />

        {/* Top penalty box */}
        <rect x="25" y="0" width="50" height="14" fill="none" stroke="rgba(255,255,255,0.2)" strokeWidth="0.4" />
        <rect x="35" y="0" width="30" height="6" fill="none" stroke="rgba(255,255,255,0.2)" strokeWidth="0.4" />

        {/* Bottom penalty box */}
        <rect x="25" y="86" width="50" height="14" fill="none" stroke="rgba(255,255,255,0.2)" strokeWidth="0.4" />
        <rect x="35" y="94" width="30" height="6" fill="none" stroke="rgba(255,255,255,0.2)" strokeWidth="0.4" />
      </svg>

      {/* Content (player cards) positioned over the pitch */}
      <div className="relative w-full h-full">
        {children}
      </div>
    </div>
  );
}
