"use client";

import { useRouter } from "next/navigation";
import { useState, useCallback, useEffect } from "react";
import { DayType } from "@/engine/enums/day-type";
import { Flag, Money, formatMoney, GameLayout } from "@/components/ui";
import { useGameStore } from "@/store/game-store";

/** Map day types to colors and labels */
const DAY_CONFIG: Record<DayType, { color: string; bg: string; dot: string; label: string; icon: string }> = {
  [DayType.Training]:       { color: "text-blue",   bg: "bg-blue/10",   dot: "bg-blue",   label: "Train",    icon: "M13 10V3L4 14h7v7l9-11h-7z" },
  [DayType.Rest]:           { color: "text-base-content/50", bg: "bg-base-200", dot: "bg-base-300", label: "Rest", icon: "M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" },
  [DayType.MatchDay]:       { color: "text-green",  bg: "bg-green/10",  dot: "bg-green",  label: "National", icon: "M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064" },
  [DayType.MundialMatchDay]:{ color: "text-yellow", bg: "bg-yellow/10", dot: "bg-yellow", label: "Mundial",  icon: "M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064" },
  [DayType.TransferWindow]: { color: "text-orange", bg: "bg-orange/10", dot: "bg-orange", label: "Transfer", icon: "M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" },
};

export default function HubPage() {
  const router = useRouter();
  const gameState = useGameStore((s) => s.gameState);
  const [, forceUpdate] = useState(0);

  // Redirect to home if no game
  useEffect(() => {
    if (!gameState) {
      router.replace("/");
    }
  }, [gameState, router]);

  if (!gameState) return null;

  const gs = gameState;
  const club = gs.playerClub;
  const day = gs.currentDay;
  const calendar = gs.calendar;
  const dayIndex = gs.currentDayIndex;
  const totalDays = calendar.length;
  const progressPct = Math.round((dayIndex / totalDays) * 100);

  const config = DAY_CONFIG[day.type];

  // Upcoming calendar dots (next 14 days)
  const upcomingDays = calendar.slice(dayIndex, dayIndex + 14);

  function advanceDay() {
    const result = gs.advanceDay();
    if (result.gameOver || result.victory || result.finished) {
      router.push("/season-end");
      return;
    }
    forceUpdate((n) => n + 1);
  }

  return (
    <GameLayout
      header={
        <div className="bg-base-100 border-b border-base-200 px-4 py-3 shadow-sm">
          <div className="max-w-4xl mx-auto flex items-center justify-between flex-wrap gap-2">
            {/* Club name + flag */}
            <div className="flex items-center gap-3">
              <Flag country={club.country} size="lg" />
              <h1 className="text-xl font-bold text-base-content">{club.name}</h1>
            </div>

            {/* Info chips */}
            <div className="flex items-center gap-2 flex-wrap">
              <div className="badge badge-info gap-1 font-medium">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                {formatMoney(club.balance)}
              </div>
              <div className="badge badge-warning gap-1 font-medium">
                Rep {gs.manager.reputation}
              </div>
              <div className="badge badge-neutral gap-1 font-medium">
                {club.team.players.length} players
              </div>
            </div>
          </div>
        </div>
      }
      footer={
        <div className="bg-base-100 border-t border-base-200 px-4 py-3 shadow-inner">
          <div className="max-w-4xl mx-auto flex items-center justify-between">
            <button
              className="btn btn-outline btn-sm gap-2"
              onClick={() => router.push("/squad")}
            >
              <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              Squad
            </button>

            <span className="text-xs text-base-content/40">
              Day {day.day} of {totalDays}
            </span>
          </div>
        </div>
      }
    >
      <div className="max-w-4xl mx-auto px-4 py-6 space-y-6">
        {/* Season progress */}
        <div className="card bg-base-100 shadow-md">
          <div className="card-body p-4">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-semibold text-base-content/60">Season Progress</span>
              <span className="text-sm font-bold text-green tabular-nums">{progressPct}%</span>
            </div>
            <progress
              className="progress progress-success w-full h-3"
              value={dayIndex}
              max={totalDays}
            />
          </div>
        </div>

        {/* Day card */}
        <div className={`card shadow-lg border-2 border-base-200 ${config.bg}`}>
          <div className="card-body items-center text-center py-8">
            <div className={`w-16 h-16 rounded-full ${config.bg} flex items-center justify-center mb-2`}>
              <svg xmlns="http://www.w3.org/2000/svg" className={`h-8 w-8 ${config.color}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={config.icon} />
              </svg>
            </div>
            <h2 className={`text-3xl font-black ${config.color}`}>
              Day {day.day}
            </h2>
            <div className={`badge badge-lg ${config.color} font-bold`}>
              {config.label}
            </div>
          </div>
        </div>

        {/* Calendar preview */}
        <div className="card bg-base-100 shadow-md">
          <div className="card-body p-4">
            <span className="text-sm font-semibold text-base-content/60 mb-2">Upcoming</span>

            {/* Dots */}
            <div className="flex items-center gap-1.5 flex-wrap mb-3">
              {upcomingDays.map((d, i) => {
                const dc = DAY_CONFIG[d.type];
                return (
                  <div
                    key={i}
                    className={`w-5 h-5 rounded-full ${dc.dot} ${
                      i === 0 ? "ring-2 ring-offset-2 ring-base-content/30" : ""
                    } transition-all`}
                    title={`Day ${d.day}: ${dc.label}`}
                  />
                );
              })}
            </div>

            {/* Legend */}
            <div className="flex items-center gap-4 flex-wrap">
              {Object.values(DayType).map((type) => {
                const dc = DAY_CONFIG[type];
                return (
                  <div key={type} className="flex items-center gap-1.5">
                    <div className={`w-3 h-3 rounded-full ${dc.dot}`} />
                    <span className="text-xs text-base-content/50">{dc.label}</span>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* Action buttons */}
        <div className="flex flex-col gap-3">
          {day.type === DayType.Training && (
            <>
              <button
                className="btn btn-info btn-lg shadow-lg font-bold text-white w-full"
                onClick={() => router.push("/training")}
              >
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                </svg>
                Train
              </button>
              <button
                className="btn btn-ghost btn-md w-full"
                onClick={advanceDay}
              >
                Skip
              </button>
            </>
          )}

          {day.type === DayType.MatchDay && (
            <button
              className="btn btn-success btn-lg shadow-lg font-bold text-white w-full"
              onClick={() => router.push("/match")}
            >
              <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z" />
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              Play Match
            </button>
          )}

          {day.type === DayType.MundialMatchDay && (
            <button
              className="btn btn-warning btn-lg shadow-lg font-bold w-full"
              onClick={() => router.push("/match")}
            >
              <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3.055 11H5a2 2 0 012 2v1a2 2 0 002 2 2 2 0 012 2v2.945M8 3.935V5.5A2.5 2.5 0 0010.5 8h.5a2 2 0 012 2 2 2 0 104 0 2 2 0 012-2h1.064M15 20.488V18a2 2 0 012-2h3.064" />
              </svg>
              Play Mundial Match
            </button>
          )}

          {day.type === DayType.TransferWindow && (
            <>
              <button
                className="btn btn-lg shadow-lg font-bold text-white w-full"
                style={{ backgroundColor: "var(--color-orange)" }}
                onClick={() => router.push("/transfers")}
              >
                <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
                </svg>
                Open Transfers
              </button>
              <button
                className="btn btn-success btn-md w-full font-bold text-white"
                onClick={advanceDay}
              >
                Advance Day
              </button>
            </>
          )}

          {day.type === DayType.Rest && (
            <button
              className="btn btn-info btn-lg shadow-lg font-bold text-white w-full"
              onClick={advanceDay}
            >
              Advance Day
            </button>
          )}
        </div>
      </div>
    </GameLayout>
  );
}
