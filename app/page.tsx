"use client";

import Link from "next/link";
import { useEffect, useRef } from "react";
import { bounceIn, floatLoop } from "@/lib/animations";

export default function MainMenu() {
  const iconRef = useRef<HTMLDivElement>(null);
  const titleRef = useRef<HTMLHeadingElement>(null);
  const buttonsRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (iconRef.current) {
      bounceIn(iconRef.current, 100);
      setTimeout(() => {
        if (iconRef.current) floatLoop(iconRef.current);
      }, 800);
    }
    if (titleRef.current) bounceIn(titleRef.current, 300);
    if (buttonsRef.current) {
      const buttons = buttonsRef.current.children;
      Array.from(buttons).forEach((btn, i) => {
        const el = btn as HTMLElement;
        el.style.opacity = "0";
        el.style.transform = "translateY(20px)";
        setTimeout(() => {
          el.style.transition = "all 0.5s cubic-bezier(0.34, 1.56, 0.64, 1)";
          el.style.opacity = "1";
          el.style.transform = "translateY(0)";
        }, 600 + i * 120);
      });
    }
  }, []);

  return (
    <div className="hero min-h-screen bg-gradient-to-b from-green-500 to-green-700">
      <div className="hero-content text-center">
        <div className="max-w-md">
          {/* Floating football icon */}
          <div ref={iconRef} className="text-7xl mb-4 opacity-0">⚽</div>

          {/* Title */}
          <h1
            ref={titleRef}
            className="text-6xl font-black text-white tracking-tight mb-2 opacity-0"
          >
            ELEVEN LEGENDS
          </h1>
          <p className="text-lg text-white/60 mb-10">Football Manager</p>

          {/* Buttons */}
          <div ref={buttonsRef} className="flex flex-col gap-3">
            <Link
              href="/club-selection"
              className="btn btn-warning btn-lg text-lg shadow-lg btn-raised font-bold"
            >
              New Game
            </Link>
            <button className="btn btn-info btn-lg text-lg btn-raised font-bold opacity-50" disabled>
              Online
            </button>
          </div>

          <p className="text-xs text-white/30 mt-10">Pre-Alpha Demo</p>
        </div>
      </div>
    </div>
  );
}
