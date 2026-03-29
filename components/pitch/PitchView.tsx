"use client";

import { type Player } from "@/engine/models/player";
import { type Formation } from "@/engine/models/formation";
import { getFormationPositions } from "@/engine/models/formation-layout";
import { PlayerCard } from "./PlayerCard";
import { PitchField } from "./PitchField";

interface PitchViewProps {
  /** All players in the squad */
  players: Player[];
  /** Current formation */
  formation: Formation;
  /** 11 player IDs in formation-slot order */
  lineupIds: number[];
  /** Called when a card on the pitch is clicked */
  onPlayerClick?: (playerId: number) => void;
  className?: string;
}

/** Single-team pitch view for Squad screen. Shows 11 player cards on a football pitch. */
export function PitchView({
  players,
  formation,
  lineupIds,
  onPlayerClick,
  className = "",
}: PitchViewProps) {
  const positions = getFormationPositions(formation);
  const playerMap = new Map(players.map((p) => [p.id, p]));

  return (
    <PitchField className={`aspect-[3/4] ${className}`}>
      {positions.map((pos, slotIndex) => {
        const playerId = lineupIds[slotIndex];
        const player = playerId ? playerMap.get(playerId) : undefined;
        const slotPosition = formation.positions[slotIndex];

        if (!player) return null;

        return (
          <div
            key={`slot-${slotIndex}`}
            className="absolute -translate-x-1/2 -translate-y-1/2 transition-all duration-500 ease-out"
            style={{
              left: `${pos[0] * 100}%`,
              top: `${pos[1] * 100}%`,
            }}
          >
            <PlayerCard
              player={player}
              slotPosition={slotPosition}
              onClick={() => onPlayerClick?.(player.id)}
            />
          </div>
        );
      })}

      {/* Chemistry lines */}
      <ChemistryLines
        positions={positions}
        lineupIds={lineupIds}
        playerMap={playerMap}
      />
    </PitchField>
  );
}

/** Draw chemistry connection lines between nearby players */
function ChemistryLines({
  positions,
  lineupIds,
  playerMap,
}: {
  positions: readonly [number, number][];
  lineupIds: number[];
  playerMap: Map<number, Player>;
}) {
  const lines: { x1: number; y1: number; x2: number; y2: number; color: string }[] = [];

  for (let i = 0; i < lineupIds.length; i++) {
    const p1 = playerMap.get(lineupIds[i]);
    if (!p1) continue;

    for (let j = i + 1; j < lineupIds.length; j++) {
      const p2 = playerMap.get(lineupIds[j]);
      if (!p2) continue;

      const [x1, y1] = positions[i];
      const [x2, y2] = positions[j];
      const dist = Math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2);

      if (dist > 0.35) continue; // Only nearby players

      const avgChem = (p1.chemistry + p2.chemistry) / 2;
      const color =
        avgChem >= 70 ? "rgba(88, 204, 2, 0.3)" :
        avgChem >= 40 ? "rgba(255, 200, 0, 0.2)" :
        "rgba(255, 75, 75, 0.2)";

      lines.push({
        x1: x1 * 100,
        y1: y1 * 100,
        x2: x2 * 100,
        y2: y2 * 100,
        color,
      });
    }
  }

  return (
    <svg className="absolute inset-0 w-full h-full pointer-events-none" viewBox="0 0 100 100" preserveAspectRatio="none">
      {lines.map((line, i) => (
        <line
          key={i}
          x1={line.x1}
          y1={line.y1}
          x2={line.x2}
          y2={line.y2}
          stroke={line.color}
          strokeWidth="0.6"
        />
      ))}
    </svg>
  );
}
