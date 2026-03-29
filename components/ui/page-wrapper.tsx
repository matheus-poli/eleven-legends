"use client";

import { type ReactNode } from "react";

interface PageWrapperProps {
  children: ReactNode;
  className?: string;
  gradient?: string;
}

/** Animated page wrapper with optional gradient background */
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
