"use client";

import { type ReactNode, useEffect, useRef } from "react";
import { staggerIn } from "@/lib/animations";

interface PageWrapperProps {
  children: ReactNode;
  className?: string;
  gradient?: string;
}

/** Animated page wrapper with anime.js entrance */
export function PageWrapper({ children, className = "", gradient }: PageWrapperProps) {
  const bg = gradient ?? "bg-base-100";

  return (
    <div className={`min-h-screen ${bg} page-enter ${className}`}>
      {children}
    </div>
  );
}

/** Fixed header + scrollable content + fixed footer layout */
export function GameLayout({
  header,
  children,
  footer,
  className = "",
}: {
  header?: ReactNode;
  children: ReactNode;
  footer?: ReactNode;
  className?: string;
}) {
  return (
    <div className={`flex flex-col h-screen page-enter ${className}`}>
      {header && <div className="shrink-0">{header}</div>}
      <div className="flex-1 overflow-auto">{children}</div>
      {footer && <div className="shrink-0">{footer}</div>}
    </div>
  );
}

/** Container that auto-staggers its children on mount with anime.js */
export function StaggerContainer({
  children,
  className = "",
  delay = 40,
}: {
  children: ReactNode;
  className?: string;
  delay?: number;
}) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (ref.current) {
      const items = ref.current.children;
      if (items.length > 0) {
        staggerIn(Array.from(items) as HTMLElement[], delay);
      }
    }
  }, [delay]);

  return (
    <div ref={ref} className={className}>
      {children}
    </div>
  );
}
