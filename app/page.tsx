"use client";

import Link from "next/link";

export default function MainMenu() {
  return (
    <div className="hero min-h-screen bg-gradient-to-b from-green-500 to-green-700">
      <div className="hero-content text-center">
        <div className="max-w-md">
          <h1 className="text-6xl font-black text-white tracking-tight mb-2">
            ELEVEN LEGENDS
          </h1>
          <p className="text-lg text-white/70 mb-10">Football Manager</p>

          <div className="flex flex-col gap-3">
            <Link href="/club-selection" className="btn btn-warning btn-lg text-lg shadow-lg">
              New Game
            </Link>
            <button className="btn btn-info btn-lg text-lg opacity-50" disabled>
              Online
            </button>
          </div>

          <p className="text-xs text-white/40 mt-8">Pre-Alpha Demo</p>
        </div>
      </div>
    </div>
  );
}
