"use client";

import { useState, useEffect, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useGameStore } from "@/store/game-store";
import { PageWrapper, SoccerBallIcon } from "@/components/ui";
import { generateChoices, processTraining } from "@/engine/simulation/training-processor";
import { type TrainingChoice, type TrainingResult, type TrainingPlayerEvent } from "@/engine/models/training-session";
import { TrainingType } from "@/engine/enums/training-type";
import { SeededRng } from "@/engine/simulation/rng";
import { type Club } from "@/engine/models/club";
import {
  BoltIcon,
  PuzzlePieceIcon,
  MoonIcon,
  StarIcon,
  ArrowUpIcon,
  ArrowDownIcon,
} from "@heroicons/react/24/solid";

/* ------------------------------------------------------------------ */
/*  Training-choice metadata: icon, accent color, and descriptions    */
/* ------------------------------------------------------------------ */

const TRAINING_META: Record<
  TrainingType,
  { icon: ReactNode; accent: string; bg: string; border: string }
> = {
  [TrainingType.IntenseDrills]: {
    icon: <BoltIcon className="w-8 h-8 text-red" />,
    accent: "text-red",
    bg: "bg-red/10",
    border: "border-red",
  },
  [TrainingType.TacticalSession]: {
    icon: <PuzzlePieceIcon className="w-8 h-8 text-green" />,
    accent: "text-green",
    bg: "bg-green/10",
    border: "border-green",
  },
  [TrainingType.LightTraining]: {
    icon: <SoccerBallIcon className="w-8 h-8 text-blue" />,
    accent: "text-blue",
    bg: "bg-blue/10",
    border: "border-blue",
  },
  [TrainingType.RestDay]: {
    icon: <MoonIcon className="w-8 h-8 text-yellow" />,
    accent: "text-yellow-dark",
    bg: "bg-yellow/10",
    border: "border-yellow",
  },
  [TrainingType.YouthFocus]: {
    icon: <StarIcon className="w-8 h-8 text-purple" />,
    accent: "text-purple",
    bg: "bg-purple/10",
    border: "border-purple",
  },
};

/* ================================================================== */
/*  Training Page                                                      */
/* ================================================================== */

export default function TrainingPage() {
  const router = useRouter();
  const gameState = useGameStore((s) => s.gameState);

  const [phase, setPhase] = useState<"choose" | "result">("choose");
  const [result, setResult] = useState<TrainingResult | null>(null);

  useEffect(() => {
    if (!gameState) { router.replace("/"); }
  }, [gameState, router]);

  if (!gameState) return null;

  const gs = gameState;
  const club = gs.playerClub;
  const dayIndex = gs.currentDayIndex;

  // Generate three random choices based on a deterministic seed
  const choiceRng = new SeededRng(gs.baseSeed + dayIndex * 7777);
  const choices = generateChoices(choiceRng);

  /* ---- Handlers -------------------------------------------------- */

  function handleChoose(choice: TrainingChoice) {
    const trainRng = new SeededRng(gs.baseSeed + dayIndex * 7777 + 1);
    const { result: trainResult, updatedClub } = processTraining(choice, club, trainRng);

    // Apply changes to the global game state
    const clubs = gs.clubs as Club[];
    const idx = clubs.findIndex((c) => c.id === club.id);
    if (idx !== -1) {
      clubs[idx] = updatedClub;
    }

    // Advance the day
    gs.advanceDay();

    setResult(trainResult);
    setPhase("result");
  }

  function handleSkip() {
    gs.advanceDay();
    router.push("/hub");
  }

  function handleContinue() {
    router.push("/hub");
  }

  /* ---- Choose Phase ---------------------------------------------- */

  if (phase === "choose") {
    return (
      <PageWrapper gradient="bg-gradient-to-b from-blue to-blue-dark">
        <div className="flex flex-col items-center justify-center min-h-screen px-4 py-10">
          {/* Header */}
          <div className="text-center mb-8">
            <div className="flex justify-center mb-2">
              <BoltIcon className="w-12 h-12 text-white" />
            </div>
            <h1 className="text-3xl font-black text-white tracking-tight">
              Training Day &mdash; Day {dayIndex + 1}
            </h1>
            <p className="text-white/70 mt-2 text-lg">
              Choose your training focus:
            </p>
          </div>

          {/* Choice Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 w-full max-w-3xl">
            {choices.map((choice) => {
              const meta = TRAINING_META[choice.type];
              return (
                <div
                  key={choice.type}
                  className="card bg-base-100 shadow-xl hover:shadow-2xl transition-all duration-200 hover:-translate-y-1 cursor-pointer group"
                >
                  <div className="card-body items-center text-center p-5">
                    {/* Icon area */}
                    <div
                      className={`w-16 h-16 rounded-2xl ${meta.bg} flex items-center justify-center mb-2 group-hover:scale-110 transition-transform`}
                    >
                      {meta.icon}
                    </div>

                    {/* Name */}
                    <h2 className={`card-title text-lg font-bold ${meta.accent}`}>
                      {choice.name}
                    </h2>

                    {/* Description */}
                    <p className="text-sm text-base-content/60 leading-snug">
                      {choice.description}
                    </p>

                    {/* Choose button */}
                    <button
                      className={`btn btn-sm mt-3 w-full border-2 ${meta.border} ${meta.accent} bg-base-100 font-bold btn-raised`}
                      onClick={() => handleChoose(choice)}
                    >
                      Choose
                    </button>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Skip button */}
          <button
            className="btn btn-ghost text-white/60 mt-8 hover:text-white"
            onClick={handleSkip}
          >
            Skip (auto-train)
          </button>
        </div>
      </PageWrapper>
    );
  }

  /* ---- Result Phase ---------------------------------------------- */

  const choiceMeta = result ? TRAINING_META[result.choice.type] : null;

  return (
    <PageWrapper>
      <div className="flex flex-col min-h-screen">
        {/* Header */}
        <div className="bg-base-200 p-6 text-center border-b border-base-300">
          <p className="text-sm text-base-content/50 uppercase tracking-widest mb-1">
            Training Complete
          </p>
          <h1 className="text-2xl font-black flex items-center justify-center gap-2">
            {choiceMeta?.icon && <span className="[&_svg]:w-6 [&_svg]:h-6">{choiceMeta.icon}</span>}
            {result?.choice.name}
          </h1>
        </div>

        {/* Event list */}
        <div className="flex-1 overflow-auto px-4 py-6 max-w-2xl mx-auto w-full">
          {result && result.events.length > 0 ? (
            <div className="flex flex-col gap-3">
              {result.events.map((evt, i) => (
                <TrainingEventCard key={`${evt.playerId}-${i}`} event={evt} />
              ))}
            </div>
          ) : (
            <div className="text-center text-base-content/40 mt-12">
              <MoonIcon className="w-10 h-10 mx-auto mb-2 text-base-content/30" />
              <p>Nothing notable happened during training.</p>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-base-300 bg-base-100">
          <button
            className="btn btn-primary btn-block text-lg font-bold shadow-md btn-raised"
            onClick={handleContinue}
          >
            Continue
          </button>
        </div>
      </div>
    </PageWrapper>
  );
}

/* ================================================================== */
/*  Sub-components                                                     */
/* ================================================================== */

function TrainingEventCard({ event }: { event: TrainingPlayerEvent }) {
  const isPositive = event.isPositive;
  const borderColor = isPositive ? "border-l-green" : "border-l-red";
  const iconBg = isPositive ? "bg-green/10" : "bg-red/10";
  const iconColor = isPositive ? "text-green" : "text-red";

  return (
    <div
      className={`card bg-base-100 shadow-sm border-l-4 ${borderColor} transition-all duration-200 hover:shadow-md`}
    >
      <div className="card-body p-4 flex-row items-center gap-3">
        {/* Arrow icon */}
        <div
          className={`w-9 h-9 rounded-full ${iconBg} flex items-center justify-center ${iconColor} shrink-0`}
        >
          {isPositive ? (
            <ArrowUpIcon className="w-5 h-5" />
          ) : (
            <ArrowDownIcon className="w-5 h-5" />
          )}
        </div>

        {/* Text content */}
        <div className="flex-1 min-w-0">
          <p className="font-semibold text-sm leading-snug">
            {event.description}
          </p>
          <div className="flex gap-3 mt-1">
            {event.moraleDelta !== 0 && (
              <span
                className={`text-xs font-medium ${
                  event.moraleDelta > 0 ? "text-green" : "text-red"
                }`}
              >
                Morale {event.moraleDelta > 0 ? "+" : ""}
                {event.moraleDelta}
              </span>
            )}
            {event.chemistryDelta !== 0 && (
              <span
                className={`text-xs font-medium ${
                  event.chemistryDelta > 0 ? "text-blue" : "text-red"
                }`}
              >
                Chemistry {event.chemistryDelta > 0 ? "+" : ""}
                {event.chemistryDelta}
              </span>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
