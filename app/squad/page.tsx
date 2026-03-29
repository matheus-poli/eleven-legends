"use client";

import { useState, useMemo, useEffect, useCallback, Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { type Club } from "@/engine/models/club";
import { type Player } from "@/engine/models/player";
import {
  type Formation,
  F442,
  F433,
  F352,
  F4231,
  F532,
  FORMATION_PRESETS,
} from "@/engine/models/formation";
import { Position } from "@/engine/enums/position";
import {
  overallForPosition,
  outfieldOverall,
  goalkeeperOverall,
} from "@/engine/models/player-attributes";
import { optimalLineup, averageOverall } from "@/engine/simulation/formation-optimizer";
import { OvrBadge, Flag, StatBar, PageWrapper } from "@/components/ui";
import { PitchView } from "@/components/pitch";
import { useGameStore } from "@/store/game-store";

// ─── Attribute categories for the detail panel ────────────────────────

const TECHNICAL_ATTRS = [
  { key: "finishing",   label: "FIN" },
  { key: "passing",     label: "PAS" },
  { key: "dribbling",   label: "DRI" },
  { key: "firstTouch",  label: "FT"  },
  { key: "technique",   label: "TEC" },
] as const;

const MENTAL_ATTRS = [
  { key: "decisions",    label: "DEC" },
  { key: "composure",    label: "CMP" },
  { key: "positioning",  label: "POS" },
  { key: "anticipation", label: "ANT" },
  { key: "offTheBall",   label: "OTB" },
] as const;

const PHYSICAL_ATTRS = [
  { key: "speed",        label: "SPD" },
  { key: "acceleration", label: "ACC" },
  { key: "stamina",      label: "STA" },
  { key: "strength",     label: "STR" },
  { key: "agility",      label: "AGI" },
] as const;

const GK_ATTRS = [
  { key: "reflexes",       label: "REF" },
  { key: "handling",       label: "HAN" },
  { key: "gkPositioning",  label: "GPO" },
  { key: "aerial",         label: "AER" },
] as const;

// ─── Greedy rearrange: fit existing 11 into new formation ─────────────

function rearrangeForFormation(
  currentIds: number[],
  players: readonly Player[],
  newFormation: Formation,
): number[] {
  const playerMap = new Map(players.map((p) => [p.id, p]));
  const available = new Set(currentIds.filter((id) => playerMap.has(id)));
  const result: number[] = new Array(newFormation.positions.length).fill(0);

  // Sort slots by scarcity (GK first)
  const slotOrder = Array.from(
    { length: newFormation.positions.length },
    (_, i) => i,
  ).sort((a, b) => {
    const aGk = newFormation.positions[a] === Position.GK ? 0 : 1;
    const bGk = newFormation.positions[b] === Position.GK ? 0 : 1;
    return aGk - bGk;
  });

  for (const slotIdx of slotOrder) {
    const slotPos = newFormation.positions[slotIdx];
    let bestId = -1;
    let bestOvr = -Infinity;

    for (const pid of available) {
      const p = playerMap.get(pid)!;
      let ovr = overallForPosition(p.attributes, slotPos);
      if (p.primaryPosition === slotPos) ovr += 5;
      else if (p.secondaryPosition === slotPos) ovr += 2;

      if (ovr > bestOvr) {
        bestOvr = ovr;
        bestId = pid;
      }
    }

    if (bestId >= 0) {
      result[slotIdx] = bestId;
      available.delete(bestId);
    }
  }

  return result;
}

// ─── Position color helper ──────────────────────────────────────────

function positionColor(pos: Position): string {
  if (pos === Position.GK) return "badge-warning";
  if ([Position.CB, Position.LB, Position.RB, Position.LWB, Position.RWB].includes(pos)) return "badge-info";
  if ([Position.CDM, Position.CM, Position.CAM, Position.LM, Position.RM].includes(pos)) return "badge-success";
  return "badge-error";
}

// ─── Component ──────────────────────────────────────────────────────

export default function SquadPage() {
  return (
    <Suspense fallback={<div className="flex items-center justify-center min-h-screen"><span className="loading loading-spinner loading-lg text-primary" /></div>}>
      <SquadContent />
    </Suspense>
  );
}

function SquadContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const gameState = useGameStore((s) => s.gameState);

  const returnUrl = searchParams.get("returnUrl") ?? "/hub";

  const club = gameState?.playerClub ?? null;
  const squad = club?.team.players ?? [];
  const playerMap = useMemo(() => new Map(squad.map((p) => [p.id, p])), [squad]);

  // State
  const [formation, setFormation] = useState<Formation>(F442);
  const [lineupIds, setLineupIds] = useState<number[]>(() => {
    if (!club) return [];
    // Initialize from team's existing lineup, or optimize
    const existing = [...club.team.startingLineup];
    if (existing.length === 11 && existing.every((id) => playerMap.has(id))) {
      return existing;
    }
    return [...optimalLineup(squad, F442)];
  });
  const [selectedPlayerId, setSelectedPlayerId] = useState<number | null>(null);

  // Bench players = not in lineup
  const lineupSet = useMemo(() => new Set(lineupIds), [lineupIds]);
  const benchPlayers = useMemo(
    () => squad.filter((p) => !lineupSet.has(p.id)),
    [squad, lineupSet],
  );

  const selectedPlayer = selectedPlayerId !== null ? playerMap.get(selectedPlayerId) ?? null : null;

  // Formation OVR calculations
  const formationOvrs = useMemo(() => {
    return FORMATION_PRESETS.map((f) => ({
      formation: f,
      ovr: averageOverall(squad, f),
    }));
  }, [squad]);

  // Current lineup average OVR
  const currentOvr = useMemo(() => {
    let total = 0;
    let count = 0;
    for (let i = 0; i < lineupIds.length; i++) {
      const p = playerMap.get(lineupIds[i]);
      if (p) {
        total += overallForPosition(p.attributes, formation.positions[i]);
        count++;
      }
    }
    return count > 0 ? total / count : 0;
  }, [lineupIds, formation, playerMap]);

  // Save lineup to team on unmount or when navigating away
  useEffect(() => {
    return () => {
      const currentGs = useGameStore.getState().gameState;
      if (currentGs) {
        const currentClub = currentGs.playerClub;
        currentClub.team = {
          ...currentClub.team,
          startingLineup: lineupIds,
        };
      }
    };
  }, [lineupIds]);

  // Redirect if no game
  useEffect(() => {
    if (!gameState) {
      router.replace("/");
    }
  }, [gameState, router]);

  if (!gameState || !club) return null;

  // Handle formation change — rearrange existing 11
  function handleFormationChange(newFormation: Formation) {
    setFormation(newFormation);
    setLineupIds(rearrangeForFormation(lineupIds, squad, newFormation));
    setSelectedPlayerId(null);
  }

  // Auto best OVR
  function handleAutoOptimize() {
    const optimal = [...optimalLineup(squad, formation)];
    setLineupIds(optimal);
    setSelectedPlayerId(null);
  }

  // Click a bench player — swap into best-fit slot
  function handleBenchPlayerClick(benchPlayerId: number) {
    const benchPlayer = playerMap.get(benchPlayerId);
    if (!benchPlayer) return;

    // Find the slot where this player fits best
    let bestSlot = -1;
    let bestImprovement = -Infinity;

    for (let i = 0; i < formation.positions.length; i++) {
      const slotPos = formation.positions[i];
      const currentPlayerId = lineupIds[i];
      const currentPlayer = playerMap.get(currentPlayerId);
      const currentOvr = currentPlayer ? overallForPosition(currentPlayer.attributes, slotPos) : 0;

      let benchOvr = overallForPosition(benchPlayer.attributes, slotPos);
      if (benchPlayer.primaryPosition === slotPos) benchOvr += 5;
      else if (benchPlayer.secondaryPosition === slotPos) benchOvr += 2;

      const improvement = benchOvr - currentOvr;
      if (improvement > bestImprovement) {
        bestImprovement = improvement;
        bestSlot = i;
      }
    }

    if (bestSlot >= 0) {
      const newLineup = [...lineupIds];
      // The player currently in that slot goes to bench (removed from lineup)
      const displacedId = newLineup[bestSlot];
      newLineup[bestSlot] = benchPlayerId;
      setLineupIds(newLineup);
    }
  }

  return (
    <PageWrapper gradient="bg-base-200">
      <div className="max-w-7xl mx-auto px-4 py-4">
        {/* Top bar */}
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-3">
            <button
              className="btn btn-ghost btn-sm gap-1"
              onClick={() => {
                // Save before leaving
                club.team = { ...club.team, startingLineup: lineupIds };
                router.push(returnUrl);
              }}
            >
              <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
              </svg>
              Back
            </button>
            <div className="flex items-center gap-2">
              <Flag country={club.country} size="md" />
              <h1 className="text-xl font-bold text-base-content">{club.name}</h1>
            </div>
          </div>
          <div className="badge badge-lg badge-success font-bold text-white">
            OVR {Math.round(currentOvr)}
          </div>
        </div>

        {/* Formation selector */}
        <div className="card bg-base-100 shadow-md mb-4">
          <div className="card-body p-3">
            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-sm font-semibold text-base-content/60 mr-1">Formation</span>
              <div className="join">
                {formationOvrs.map(({ formation: f, ovr }) => (
                  <button
                    key={f.name}
                    className={`join-item btn btn-sm ${
                      formation.name === f.name
                        ? "btn-primary"
                        : "btn-ghost"
                    }`}
                    onClick={() => handleFormationChange(f)}
                  >
                    <span className="font-bold">{f.name}</span>
                    <span className="text-[10px] opacity-70 ml-1">{Math.round(ovr)}</span>
                  </button>
                ))}
              </div>
              <div className="ml-auto">
                <button
                  className="btn btn-warning btn-sm font-bold shadow-sm"
                  onClick={handleAutoOptimize}
                >
                  <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                  </svg>
                  Auto Best OVR
                </button>
              </div>
            </div>
          </div>
        </div>

        {/* Main content: pitch + side panel */}
        <div className="flex gap-4 flex-col lg:flex-row">
          {/* Left: Pitch */}
          <div className="lg:w-[70%] w-full">
            <div className="card bg-base-100 shadow-md overflow-hidden">
              <PitchView
                players={[...squad]}
                formation={formation}
                lineupIds={lineupIds}
                onPlayerClick={(id) => setSelectedPlayerId(id)}
                className="w-full"
              />
            </div>
          </div>

          {/* Right: Bench or Player Detail */}
          <div className="lg:w-[30%] w-full">
            {selectedPlayer ? (
              /* Player Detail Panel */
              <PlayerDetailPanel
                player={selectedPlayer}
                formation={formation}
                lineupIds={lineupIds}
                onBack={() => setSelectedPlayerId(null)}
              />
            ) : (
              /* Bench List */
              <div className="card bg-base-100 shadow-md">
                <div className="card-body p-3">
                  <h3 className="font-bold text-base-content mb-2 flex items-center gap-2">
                    <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4 text-base-content/50" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
                    </svg>
                    Bench
                    <span className="badge badge-sm badge-neutral">{benchPlayers.length}</span>
                  </h3>

                  <div className="space-y-1 max-h-[60vh] overflow-y-auto pr-1">
                    {benchPlayers.length === 0 ? (
                      <p className="text-sm text-base-content/40 text-center py-4">
                        All players are in the lineup
                      </p>
                    ) : (
                      benchPlayers
                        .sort((a, b) => {
                          const ovrA = a.primaryPosition === Position.GK
                            ? goalkeeperOverall(a.attributes)
                            : outfieldOverall(a.attributes);
                          const ovrB = b.primaryPosition === Position.GK
                            ? goalkeeperOverall(b.attributes)
                            : outfieldOverall(b.attributes);
                          return ovrB - ovrA;
                        })
                        .map((player) => {
                          const ovr = overallForPosition(player.attributes, player.primaryPosition);
                          return (
                            <button
                              key={player.id}
                              className="flex items-center gap-2 w-full p-2 rounded-lg hover:bg-base-200 transition-colors text-left"
                              onClick={() => handleBenchPlayerClick(player.id)}
                            >
                              <OvrBadge ovr={ovr} size="sm" />
                              <span className={`badge badge-xs ${positionColor(player.primaryPosition)}`}>
                                {player.primaryPosition}
                              </span>
                              <span className="text-sm font-medium truncate flex-1">
                                {player.name}
                              </span>
                              <span className="text-xs text-base-content/40">
                                {player.age}y
                              </span>
                            </button>
                          );
                        })
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </PageWrapper>
  );
}

// ─── Player Detail Panel ──────────────────────────────────────────────

function PlayerDetailPanel({
  player,
  formation,
  lineupIds,
  onBack,
}: {
  player: Player;
  formation: Formation;
  lineupIds: number[];
  onBack: () => void;
}) {
  const isGk = player.primaryPosition === Position.GK;
  const ovr = overallForPosition(player.attributes, player.primaryPosition);

  // Find which slot this player occupies
  const slotIndex = lineupIds.indexOf(player.id);
  const slotPos = slotIndex >= 0 ? formation.positions[slotIndex] : null;
  const slotOvr = slotPos ? overallForPosition(player.attributes, slotPos) : null;

  return (
    <div className="card bg-base-100 shadow-md">
      <div className="card-body p-4 space-y-4">
        {/* Back button */}
        <button
          className="btn btn-ghost btn-xs self-start gap-1"
          onClick={onBack}
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Back to Bench
        </button>

        {/* Player header */}
        <div className="text-center">
          <h3 className="text-lg font-bold text-base-content">{player.name}</h3>
          <div className="flex items-center justify-center gap-2 mt-1">
            <OvrBadge ovr={ovr} size="lg" />
            <span className={`badge ${positionColor(player.primaryPosition)} font-bold`}>
              {player.primaryPosition}
            </span>
            {player.secondaryPosition && (
              <span className={`badge badge-outline badge-sm ${positionColor(player.secondaryPosition)}`}>
                {player.secondaryPosition}
              </span>
            )}
          </div>
          {slotPos && slotPos !== player.primaryPosition && (
            <div className="mt-1">
              <span className="text-xs text-orange font-medium">
                Playing as {slotPos} (OVR: {Math.round(slotOvr!)})
              </span>
            </div>
          )}
        </div>

        {/* Info row */}
        <div className="flex items-center justify-center gap-4 text-sm">
          <div className="flex items-center gap-1">
            <span className="text-base-content/50">Age</span>
            <span className="font-bold">{player.age}</span>
          </div>
          <div className="flex items-center gap-1">
            <span className="text-base-content/50">Morale</span>
            <span className={`font-bold ${
              player.morale >= 70 ? "text-green" : player.morale >= 40 ? "text-yellow" : "text-red"
            }`}>
              {player.morale}
            </span>
          </div>
          <div className="flex items-center gap-1">
            <span className="text-base-content/50">Chem</span>
            <span className={`font-bold ${
              player.chemistry >= 70 ? "text-green" : player.chemistry >= 40 ? "text-yellow" : "text-red"
            }`}>
              {player.chemistry}
            </span>
          </div>
        </div>

        <div className="divider my-0" />

        {/* Technical */}
        <div>
          <h4 className="text-xs font-bold text-blue uppercase tracking-wide mb-1.5">Technical</h4>
          <div className="space-y-1">
            {TECHNICAL_ATTRS.map(({ key, label }) => (
              <StatBar key={key} label={label} value={player.attributes[key]} />
            ))}
          </div>
        </div>

        {/* Mental */}
        <div>
          <h4 className="text-xs font-bold text-purple uppercase tracking-wide mb-1.5">Mental</h4>
          <div className="space-y-1">
            {MENTAL_ATTRS.map(({ key, label }) => (
              <StatBar key={key} label={label} value={player.attributes[key]} />
            ))}
          </div>
        </div>

        {/* Physical */}
        <div>
          <h4 className="text-xs font-bold text-green uppercase tracking-wide mb-1.5">Physical</h4>
          <div className="space-y-1">
            {PHYSICAL_ATTRS.map(({ key, label }) => (
              <StatBar key={key} label={label} value={player.attributes[key]} />
            ))}
          </div>
        </div>

        {/* GK (only for goalkeepers) */}
        {isGk && (
          <div>
            <h4 className="text-xs font-bold text-yellow uppercase tracking-wide mb-1.5">Goalkeeper</h4>
            <div className="space-y-1">
              {GK_ATTRS.map(({ key, label }) => (
                <StatBar key={key} label={label} value={player.attributes[key]} />
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
