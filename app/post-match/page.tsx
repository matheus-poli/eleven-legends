"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { useGameStore } from "@/store/game-store";
import {
  PageWrapper,
  RatingBadge,
  Money,
  formatMoney,
  Badge,
} from "@/components/ui";
import { MatchPitchView } from "@/components/pitch";
import { EventType } from "@/engine/enums/event-type";
import { type Player } from "@/engine/models/player";
import { type MatchEvent } from "@/engine/models/match-event";
import {
  getMatchSession,
  getMatchConfig,
  getMatchContext,
  getMatchResult,
  clearMatch,
} from "@/store/match-store";

// ---------------------------------------------------------------------------
// Outcome helpers
// ---------------------------------------------------------------------------

type Outcome = "win" | "draw" | "loss";

function getOutcome(
  scoreHome: number,
  scoreAway: number,
  isHome: boolean,
): Outcome {
  const playerScore = isHome ? scoreHome : scoreAway;
  const oppScore = isHome ? scoreAway : scoreHome;
  if (playerScore > oppScore) return "win";
  if (playerScore < oppScore) return "loss";
  return "draw";
}

const OUTCOME_CONFIG: Record<
  Outcome,
  { gradient: string; label: string; icon: string; textColor: string }
> = {
  win: {
    gradient: "bg-gradient-to-b from-green-600 to-green-900",
    label: "VICTORY",
    icon: "\uD83C\uDFC6",
    textColor: "text-green-100",
  },
  draw: {
    gradient: "bg-gradient-to-b from-yellow-600 to-yellow-900",
    label: "DRAW",
    icon: "\uD83E\uDD1D",
    textColor: "text-yellow-100",
  },
  loss: {
    gradient: "bg-gradient-to-b from-gray-600 to-gray-900",
    label: "DEFEAT",
    icon: "\uD83D\uDE14",
    textColor: "text-gray-200",
  },
};

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export default function PostMatchPage() {
  const router = useRouter();
  const gameState = useGameStore((s) => s.getState);

  const [processed, setProcessed] = useState(false);

  const result = getMatchResult();
  const config = getMatchConfig();
  const ctx = getMatchContext();
  const session = getMatchSession();

  // Redirect if no result
  useEffect(() => {
    if (!result || !config || !ctx) {
      router.replace("/match");
    }
  }, [result, config, ctx, router]);

  // Process day completion on mount
  useEffect(() => {
    if (processed || !result || !ctx) return;

    try {
      const gs = gameState();
      gs.finishDay(ctx, result);
      setProcessed(true);
    } catch {
      // Game state might not be available
      setProcessed(true);
    }
  }, [processed, result, ctx, gameState]);

  if (!result || !config) {
    return null;
  }

  const finalState = result.finalState;

  // Determine outcome
  const gs = (() => {
    try {
      return gameState();
    } catch {
      return null;
    }
  })();
  const isHome = ctx?.playerFixture?.homeClubId === gs?.manager.clubId;
  const outcome = getOutcome(
    finalState.scoreHome,
    finalState.scoreAway,
    isHome ?? true,
  );
  const outcomeCfg = OUTCOME_CONFIG[outcome];

  // Build player lookup
  const allPlayers = new Map<number, Player>();
  for (const p of config.homeTeam.players) allPlayers.set(p.id, p);
  for (const p of config.awayTeam.players) allPlayers.set(p.id, p);

  // MVP / SVP
  const mvp = allPlayers.get(result.mvpPlayerId);
  const svp = allPlayers.get(result.svpPlayerId);
  const mvpRating = finalState.playerRatings[result.mvpPlayerId] ?? 6.0;
  const svpRating = finalState.playerRatings[result.svpPlayerId] ?? 6.0;

  // Goals
  const goals = result.events.filter((e) => e.type === EventType.Goal);
  const cards = result.events.filter(
    (e) =>
      e.type === EventType.YellowCard || e.type === EventType.RedCard,
  );

  // Match money earned (rough estimate based on outcome)
  const moneyEarned =
    outcome === "win" ? 50000 : outcome === "draw" ? 20000 : 10000;

  // Other notable results (from ctx.allFixtures)
  const otherResults = (ctx?.allFixtures ?? [])
    .filter(
      (f) =>
        f.result !== null &&
        f !== ctx?.playerFixture,
    )
    .slice(0, 5);

  // ---------------------------------------------------------------------------
  // Handler
  // ---------------------------------------------------------------------------

  function handleContinue() {
    clearMatch();
    // Navigate based on season state
    if (gs?.isSeasonOver) {
      router.push("/season-end");
    } else {
      router.push("/hub");
    }
  }

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  return (
    <PageWrapper gradient={outcomeCfg.gradient}>
      <div className="min-h-screen flex flex-col">
        {/* Header */}
        <div className="text-center py-8">
          <span className="text-5xl mb-2 block">{outcomeCfg.icon}</span>
          <h1
            className={`text-5xl font-black tracking-tight ${outcomeCfg.textColor}`}
          >
            {outcomeCfg.label}
          </h1>
          <div className="flex items-center justify-center gap-4 mt-4">
            <span className="text-white/70 text-lg">
              {config.homeTeam.name}
            </span>
            <span className="text-4xl font-black text-white tabular-nums">
              {finalState.scoreHome} - {finalState.scoreAway}
            </span>
            <span className="text-white/70 text-lg">
              {config.awayTeam.name}
            </span>
          </div>
        </div>

        {/* Main content */}
        <div className="flex-1 flex flex-col lg:flex-row gap-4 px-4 pb-4">
          {/* Left: Pitch (60%) */}
          <div className="lg:w-[60%] flex items-start justify-center">
            <div className="w-full max-w-lg">
              <MatchPitchView
                config={config}
                ratings={finalState.playerRatings}
                className="w-full"
              />
            </div>
          </div>

          {/* Right: Sidebar (40%) */}
          <div className="lg:w-[40%] space-y-4 overflow-auto max-h-[60vh] lg:max-h-[70vh]">
            {/* MVP Card */}
            {mvp && (
              <div className="card bg-base-300/80 backdrop-blur border-2 border-warning/50 shadow-lg">
                <div className="card-body p-4">
                  <div className="flex items-center gap-3">
                    <span className="text-3xl">\uD83C\uDFC5</span>
                    <div className="flex-1">
                      <p className="text-xs text-warning font-bold uppercase tracking-wider">
                        Man of the Match
                      </p>
                      <p className="font-bold text-lg">{mvp.name}</p>
                      <p className="text-xs text-base-content/50">
                        {mvp.primaryPosition}
                      </p>
                    </div>
                    <RatingBadge rating={mvpRating} size="lg" />
                  </div>
                </div>
              </div>
            )}

            {/* SVP Card */}
            {svp && (
              <div className="card bg-base-300/80 backdrop-blur border-2 border-info/50 shadow-lg">
                <div className="card-body p-4">
                  <div className="flex items-center gap-3">
                    <span className="text-2xl">\u2B50</span>
                    <div className="flex-1">
                      <p className="text-xs text-info font-bold uppercase tracking-wider">
                        Second Best
                      </p>
                      <p className="font-bold">{svp.name}</p>
                      <p className="text-xs text-base-content/50">
                        {svp.primaryPosition}
                      </p>
                    </div>
                    <RatingBadge rating={svpRating} size="md" />
                  </div>
                </div>
              </div>
            )}

            {/* Money earned */}
            <div className="card bg-base-300/80 backdrop-blur shadow-lg">
              <div className="card-body p-4 flex-row items-center gap-3">
                <span className="text-2xl">\uD83D\uDCB0</span>
                <div className="flex-1">
                  <p className="text-xs text-base-content/50 font-bold uppercase tracking-wider">
                    Match Revenue
                  </p>
                  <Money amount={moneyEarned} />
                </div>
              </div>
            </div>

            {/* Goals */}
            {goals.length > 0 && (
              <div className="card bg-base-300/80 backdrop-blur border border-success/30 shadow-lg">
                <div className="card-body p-4">
                  <h3 className="text-sm font-bold text-success mb-2">
                    \u26BD Goals
                  </h3>
                  <div className="space-y-1.5">
                    {goals.map((evt, i) => {
                      const scorer = allPlayers.get(evt.playerId);
                      const assister = evt.secondaryPlayerId
                        ? allPlayers.get(evt.secondaryPlayerId)
                        : null;
                      const minute =
                        evt.tick > 45
                          ? evt.tick
                          : evt.tick;
                      return (
                        <div
                          key={i}
                          className="flex items-center gap-2 text-sm"
                        >
                          <span className="text-base-content/40 tabular-nums w-8 text-right">
                            {minute}&apos;
                          </span>
                          <span className="font-medium">
                            {scorer?.name ?? `Player #${evt.playerId}`}
                          </span>
                          {assister && (
                            <span className="text-base-content/40">
                              (ast. {assister.name})
                            </span>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            )}

            {/* Cards */}
            {cards.length > 0 && (
              <div className="card bg-base-300/80 backdrop-blur border border-warning/30 shadow-lg">
                <div className="card-body p-4">
                  <h3 className="text-sm font-bold text-warning mb-2">
                    Cards
                  </h3>
                  <div className="space-y-1.5">
                    {cards.map((evt, i) => {
                      const player = allPlayers.get(evt.playerId);
                      return (
                        <div
                          key={i}
                          className="flex items-center gap-2 text-sm"
                        >
                          <span>
                            {evt.type === EventType.RedCard
                              ? "\uD83D\uDFE5"
                              : "\uD83D\uDFE8"}
                          </span>
                          <span className="font-medium">
                            {player?.name ??
                              `Player #${evt.playerId}`}
                          </span>
                          <span className="text-base-content/40 tabular-nums">
                            {evt.tick}&apos;
                          </span>
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            )}

            {/* Other results */}
            {otherResults.length > 0 && (
              <div className="card bg-base-300/80 backdrop-blur shadow-lg">
                <div className="card-body p-4">
                  <h3 className="text-sm font-bold text-base-content/60 mb-2">
                    Other Results
                  </h3>
                  <div className="space-y-1">
                    {otherResults.map((fixture, i) => {
                      const home = gs?.clubs.find(
                        (c) => c.id === fixture.homeClubId,
                      );
                      const away = gs?.clubs.find(
                        (c) => c.id === fixture.awayClubId,
                      );
                      return (
                        <div
                          key={i}
                          className="flex items-center gap-2 text-sm"
                        >
                          <span className="flex-1 text-right truncate">
                            {home?.name ?? "Home"}
                          </span>
                          <span className="font-bold tabular-nums text-base-content/70 w-12 text-center">
                            {fixture.result
                              ? `${fixture.result[0]}-${fixture.result[1]}`
                              : "?-?"}
                          </span>
                          <span className="flex-1 truncate">
                            {away?.name ?? "Away"}
                          </span>
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 text-center">
          <button
            className="btn btn-primary btn-lg shadow-lg min-w-48"
            onClick={handleContinue}
          >
            Continue
          </button>
        </div>
      </div>
    </PageWrapper>
  );
}
