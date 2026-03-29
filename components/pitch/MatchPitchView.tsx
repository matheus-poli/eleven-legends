"use client";

import { type Player } from "@/engine/models/player";
import { type Formation } from "@/engine/models/formation";
import { type MatchConfig } from "@/engine/models/match-config";
import { getFormationPositions } from "@/engine/models/formation-layout";
import { F442 } from "@/engine/models/formation";
import { PlayerCard } from "./PlayerCard";
import { PitchField } from "./PitchField";

interface MatchPitchViewProps {
  config: MatchConfig;
  homeFormation?: Formation;
  awayFormation?: Formation;
  /** Player ID → rating (0-10). Updated each tick for live display. */
  ratings?: Record<number, number>;
  className?: string;
}

/** Dual-team pitch view for match simulation. Home at bottom, away at top (mirrored). */
export function MatchPitchView({
  config,
  homeFormation,
  awayFormation,
  ratings = {},
  className = "",
}: MatchPitchViewProps) {
  const hForm = homeFormation ?? config.homeTactics?.formation ?? F442;
  const aForm = awayFormation ?? config.awayTactics?.formation ?? F442;

  const homePositions = getFormationPositions(hForm);
  const awayPositions = getFormationPositions(aForm);

  const homeIds = config.homeTactics?.startingPlayerIds ?? config.homeTeam.startingLineup;
  const awayIds = config.awayTactics?.startingPlayerIds ?? config.awayTeam.startingLineup;

  const allPlayers = new Map([
    ...config.homeTeam.players.map((p) => [p.id, p] as const),
    ...config.awayTeam.players.map((p) => [p.id, p] as const),
  ]);

  return (
    <PitchField className={`aspect-[3/4] ${className}`}>
      {/* Home team — bottom half (Y as-is) */}
      {homePositions.map((pos, i) => {
        const player = allPlayers.get(homeIds[i]);
        if (!player) return null;
        return (
          <div
            key={`home-${i}`}
            className="absolute -translate-x-1/2 -translate-y-1/2 transition-all duration-300"
            style={{ left: `${pos[0] * 100}%`, top: `${pos[1] * 100}%` }}
          >
            <PlayerCard
              player={player}
              slotPosition={hForm.positions[i]}
              matchRating={ratings[player.id]}
              tint="home"
              showPositionWarning={false}
              size="sm"
            />
          </div>
        );
      })}

      {/* Away team — top half (mirrored Y) */}
      {awayPositions.map((pos, i) => {
        const player = allPlayers.get(awayIds[i]);
        if (!player) return null;
        return (
          <div
            key={`away-${i}`}
            className="absolute -translate-x-1/2 -translate-y-1/2 transition-all duration-300"
            style={{ left: `${pos[0] * 100}%`, top: `${(1 - pos[1]) * 100}%` }}
          >
            <PlayerCard
              player={player}
              slotPosition={aForm.positions[i]}
              matchRating={ratings[player.id]}
              tint="away"
              showPositionWarning={false}
              size="sm"
            />
          </div>
        );
      })}
    </PitchField>
  );
}
