/**
 * Game-specific SVG icons that don't exist in heroicons.
 * For standard icons, import directly from @heroicons/react.
 */

/** Soccer ball icon */
export function SoccerBallIcon({ className = "w-6 h-6" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor">
      <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 3.3l1.35-.95c1.82.56 3.37 1.76 4.38 3.34l-.39 1.34-1.35.46L13 6.7V5.3zm-3.35-.95L11 5.3v1.4L7.01 9.49l-1.35-.46-.39-1.34a8.982 8.982 0 014.38-3.34zM7.08 17.11l-1.14.1A8.94 8.94 0 013 12c0-.73.08-1.45.23-2.14l1.18-.39 1.34.48 1.46 4.34-.13 1.42-.99 1.4zm7.42 3.68A8.86 8.86 0 0112 21a8.86 8.86 0 01-2.5-.21l-.79-1.09.15-1.41 4.04-1.89h2.2l.94 1.09-.54 1.82-.01.38zm3.44-1.57l-1.13-.1-1-1.4-.12-1.42 1.46-4.34 1.34-.48 1.18.39c.15.69.23 1.41.23 2.14a8.94 8.94 0 01-2.96 5.21z" />
    </svg>
  );
}

/** Yellow card rectangle */
export function YellowCardIcon({ className = "w-5 h-5" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 16 20" fill="none">
      <rect x="1" y="1" width="14" height="18" rx="2" fill="#FFC800" stroke="#E5B400" strokeWidth="1" />
    </svg>
  );
}

/** Red card rectangle */
export function RedCardIcon({ className = "w-5 h-5" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 16 20" fill="none">
      <rect x="1" y="1" width="14" height="18" rx="2" fill="#FF4B4B" stroke="#EA2B2B" strokeWidth="1" />
    </svg>
  );
}

/** Whistle icon for fouls */
export function WhistleIcon({ className = "w-5 h-5" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor">
      <path d="M12 1C8.676 1 6 3.676 6 7c0 1.747.757 3.318 1.957 4.412L3.707 15.66a1 1 0 000 1.415l3.218 3.218a1 1 0 001.414 0l4.249-4.25A5.978 5.978 0 0012 13a5.978 5.978 0 003.412-1.957l4.249 4.25a1 1 0 001.414 0l.218-.218a1 1 0 000-1.414L17.043 9.41A5.978 5.978 0 0018 7c0-3.324-2.676-6-6-6zm0 10a4 4 0 110-8 4 4 0 010 8z" />
    </svg>
  );
}

/** Gloves/save icon */
export function GlovesIcon({ className = "w-5 h-5" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="currentColor">
      <path d="M14 2a3 3 0 00-3 3v4.59l-1.29-1.3a3 3 0 00-4.24 0 3 3 0 000 4.24L10 17.06V20a2 2 0 002 2h6a2 2 0 002-2v-6a8 8 0 00-2-5.29V5a3 3 0 00-3-3h-1zm1 2a1 1 0 011 1v5h-4V5a1 1 0 011-1h2z" />
    </svg>
  );
}

/** Swap arrows for substitution */
export function SubstitutionIcon({ className = "w-5 h-5" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M7 16V4m0 0L3 8m4-4l4 4" />
      <path d="M17 8v12m0 0l4-4m-4 4l-4-4" />
    </svg>
  );
}

/** Wind/debuff icon */
export function WindIcon({ className = "w-5 h-5" }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M9.59 4.59A2 2 0 1111 8H2m10.59 11.41A2 2 0 1014 16H2m15.73-8.27A2.5 2.5 0 1119.5 12H2" />
    </svg>
  );
}
