"use client";

import { useState, useEffect, useMemo, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useGameStore } from "@/store/game-store";
import { PageWrapper, WindIcon } from "@/components/ui";
import { generate as generateCards } from "@/engine/locker-room-card-generator";
import { applyCard } from "@/engine/simulation/halftime-processor";
import { type LockerRoomCard } from "@/engine/models/locker-room-card";
import { CardEffect } from "@/engine/enums/card-effect";
import { SeededRng } from "@/engine/simulation/rng";
import {
  FireIcon,
  BoltIcon,
  ShieldCheckIcon,
} from "@heroicons/react/24/solid";
import {
  getMatchSession,
  getMatchConfig,
  getMatchContext,
} from "@/store/match-store";

// ---------------------------------------------------------------------------
// Card visual helpers
// ---------------------------------------------------------------------------

function cardEffectIcon(effect: CardEffect): ReactNode {
  const cls = "w-10 h-10 text-white";
  switch (effect) {
    case CardEffect.MoraleBoost:
      return <FireIcon className={cls} />;
    case CardEffect.StaminaRecovery:
      return <BoltIcon className={cls} />;
    case CardEffect.TeamBuff:
      return <ShieldCheckIcon className={cls} />;
    case CardEffect.OpponentDebuff:
      return <WindIcon className={cls} />;
  }
}

function cardEffectColor(effect: CardEffect): string {
  switch (effect) {
    case CardEffect.MoraleBoost:
      return "from-orange to-red";
    case CardEffect.StaminaRecovery:
      return "from-green to-green-dark";
    case CardEffect.TeamBuff:
      return "from-blue to-blue-dark";
    case CardEffect.OpponentDebuff:
      return "from-purple to-purple/80";
  }
}

function cardBorderColor(effect: CardEffect): string {
  switch (effect) {
    case CardEffect.MoraleBoost:
      return "border-orange/50 hover:border-orange";
    case CardEffect.StaminaRecovery:
      return "border-green/50 hover:border-green";
    case CardEffect.TeamBuff:
      return "border-blue/50 hover:border-blue";
    case CardEffect.OpponentDebuff:
      return "border-purple/50 hover:border-purple";
  }
}

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export default function HalftimePage() {
  const router = useRouter();
  const gameState = useGameStore((s) => s.getState);

  const [chosen, setChosen] = useState<number | null>(null);
  const [animating, setAnimating] = useState(false);

  const session = getMatchSession();
  const config = getMatchConfig();
  const ctx = getMatchContext();

  // Generate 3 cards based on match context
  const cards: LockerRoomCard[] = useMemo(() => {
    if (!session || !config) return [];

    const state = session.state;
    const gs = (() => {
      try {
        return gameState();
      } catch {
        return null;
      }
    })();

    const isHome = ctx?.playerFixture?.homeClubId === gs?.manager.clubId;
    const scoreDiff = isHome
      ? state.scoreHome - state.scoreAway
      : state.scoreAway - state.scoreHome;

    // Calculate average stamina for the player's active team
    const activeIds = isHome
      ? state.homeActivePlayerIds
      : state.awayActivePlayerIds;

    let avgStamina = 70;
    let avgMorale = 50;
    if (activeIds.length > 0) {
      let totalStamina = 0;
      let count = 0;
      for (const pid of activeIds) {
        const stam = state.playerStamina[pid];
        if (stam != null) {
          totalStamina += stam;
          count++;
        }
      }
      if (count > 0) avgStamina = totalStamina / count;

      const team = isHome ? config.homeTeam : config.awayTeam;
      let totalMorale = 0;
      let moraleCount = 0;
      for (const pid of activeIds) {
        const player = team.players.find((p) => p.id === pid);
        if (player) {
          totalMorale += player.morale;
          moraleCount++;
        }
      }
      if (moraleCount > 0) avgMorale = totalMorale / moraleCount;
    }

    const rng = new SeededRng(config.seed + 999);
    return generateCards(rng, scoreDiff, avgStamina, avgMorale);
  }, [session, config, ctx, gameState]);

  // Redirect if no active session
  useEffect(() => {
    if (!session || !config) {
      router.replace("/match");
    }
  }, [session, config, router]);

  if (!session || !config) {
    return null;
  }

  const state = session.state;

  const gs = (() => {
    try {
      return gameState();
    } catch {
      return null;
    }
  })();
  const isHome = ctx?.playerFixture?.homeClubId === gs?.manager.clubId;

  // ---------------------------------------------------------------------------
  // Handlers
  // ---------------------------------------------------------------------------

  function handleChoose(index: number) {
    if (chosen !== null || animating) return;

    setChosen(index);
    setAnimating(true);

    const card = cards[index];

    // Apply card effect
    applyCard(state, config!, card, isHome ?? true);

    // Start second half after short animation delay
    setTimeout(() => {
      session!.startSecondHalf();
      router.push("/match");
    }, 800);
  }

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  return (
    <PageWrapper gradient="bg-gradient-to-b from-neutral via-blue-dark/30 to-neutral">
      <div className="min-h-screen flex flex-col items-center justify-center px-4 py-8">
        {/* Score */}
        <div className="text-center mb-8">
          <p className="text-white/40 text-sm mb-1 uppercase tracking-widest">
            Halftime
          </p>
          <div className="flex items-center gap-4">
            <span className="text-white/70 text-lg">
              {config.homeTeam.name}
            </span>
            <span className="text-5xl font-black text-white tabular-nums">
              {state.scoreHome} - {state.scoreAway}
            </span>
            <span className="text-white/70 text-lg">
              {config.awayTeam.name}
            </span>
          </div>
        </div>

        {/* Title */}
        <h2 className="text-2xl font-bold text-white mb-8">
          Choose a Locker Room Card
        </h2>

        {/* Cards */}
        <div className="flex gap-6 flex-wrap justify-center">
          {cards.map((card, i) => {
            const isChosen = chosen === i;
            const isDimmed = chosen !== null && !isChosen;

            return (
              <div
                key={i}
                className={`
                  relative w-64 rounded-2xl border-2 overflow-hidden
                  transition-all duration-500 ease-out cursor-pointer
                  ${cardBorderColor(card.effect)}
                  ${isChosen ? "scale-110 shadow-2xl shadow-white/20 -translate-y-4" : ""}
                  ${isDimmed ? "opacity-30 scale-95 pointer-events-none" : ""}
                  ${!chosen ? "hover:scale-105 hover:-translate-y-2 hover:shadow-xl" : ""}
                `}
                style={{
                  perspective: "800px",
                  transformStyle: "preserve-3d",
                }}
                onClick={() => handleChoose(i)}
              >
                {/* Gradient header */}
                <div
                  className={`bg-gradient-to-br ${cardEffectColor(card.effect)} p-6 text-center`}
                >
                  <div className="flex justify-center mb-2">
                    {cardEffectIcon(card.effect)}
                  </div>
                  <h3 className="text-white font-bold text-lg">
                    {card.name}
                  </h3>
                </div>

                {/* Body */}
                <div className="bg-base-300 p-4 text-center">
                  <p className="text-base-content/70 text-sm mb-3">
                    {card.description}
                  </p>
                  <span className="badge badge-lg badge-warning font-bold tabular-nums">
                    +{card.magnitude}
                  </span>
                </div>

                {/* Choose button */}
                <div className="bg-base-300 px-4 pb-4">
                  <button
                    className={`btn btn-block btn-raised ${
                      isChosen ? "btn-success" : "btn-primary"
                    }`}
                    disabled={chosen !== null}
                  >
                    {isChosen ? "Chosen!" : "Choose"}
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </PageWrapper>
  );
}
