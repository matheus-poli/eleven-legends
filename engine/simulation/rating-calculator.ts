import { EventType, Position } from "@/engine/enums";
import type { MatchConfig, MatchEvent, MatchState, Player } from "@/engine/models";

/**
 * Calculates player ratings during and after a match.
 * Base rating: 6.0. Adjusted by events and position context.
 */

export const BASE_RATING = 6.0;
export const MIN_RATING = 0.0;
export const MAX_RATING = 10.0;

/** Initializes all starting players' ratings to the base value. */
export function initializeRatings(state: MatchState, config: MatchConfig): void {
  const homeIds =
    state.homeActivePlayerIds.length > 0
      ? state.homeActivePlayerIds
      : config.homeTeam.startingLineup;
  const awayIds =
    state.awayActivePlayerIds.length > 0
      ? state.awayActivePlayerIds
      : config.awayTeam.startingLineup;

  for (const playerId of homeIds) {
    state.playerRatings[playerId] = BASE_RATING;
  }
  for (const playerId of awayIds) {
    state.playerRatings[playerId] = BASE_RATING;
  }
}

/** Applies a list of events to update player ratings, with position adjustments. */
export function applyEvents(
  state: MatchState,
  events: readonly MatchEvent[],
  config: MatchConfig,
): void {
  for (const evt of events) {
    if (state.playerRatings[evt.playerId] == null) {
      continue;
    }

    const player = findPlayer(evt.playerId, config);
    let impact = evt.ratingImpact;

    if (player != null) {
      impact *= getPositionMultiplier(player.primaryPosition, evt.type);
    }

    state.playerRatings[evt.playerId] = Math.min(
      Math.max(state.playerRatings[evt.playerId] + impact, MIN_RATING),
      MAX_RATING,
    );
  }
}

/** Returns the MVP (highest rated) and SVP (second highest) player IDs. */
export function getMvpAndSvp(
  state: MatchState,
): { mvpId: number; svpId: number } {
  const sorted = Object.entries(state.playerRatings)
    .map(([id, rating]) => ({ id: Number(id), rating }))
    .sort((a, b) => b.rating - a.rating);

  const mvpId = sorted.length > 0 ? sorted[0].id : -1;
  const svpId = sorted.length > 1 ? sorted[1].id : -1;

  return { mvpId, svpId };
}

/** Position-based multiplier: defenders get more from tackles, attackers from goals, etc. */
export function getPositionMultiplier(
  position: Position,
  eventType: EventType,
): number {
  // Defenders value defensive actions more
  if (
    (position === Position.CB ||
      position === Position.LB ||
      position === Position.RB ||
      position === Position.LWB ||
      position === Position.RWB) &&
    eventType === EventType.Save
  ) {
    return 1.3;
  }
  if (
    (position === Position.CB ||
      position === Position.LB ||
      position === Position.RB ||
      position === Position.LWB ||
      position === Position.RWB) &&
    eventType === EventType.Goal
  ) {
    return 1.5; // rare, so extra reward
  }

  // Midfielders are balanced
  if (position === Position.CDM && eventType === EventType.Save) {
    return 1.2;
  }

  // Attackers value goals more
  if (
    (position === Position.ST ||
      position === Position.CF ||
      position === Position.LW ||
      position === Position.RW) &&
    eventType === EventType.Goal
  ) {
    return 1.2;
  }
  if (
    (position === Position.ST ||
      position === Position.CF ||
      position === Position.LW ||
      position === Position.RW) &&
    eventType === EventType.Assist
  ) {
    return 1.1;
  }

  // Goalkeepers value saves
  if (position === Position.GK && eventType === EventType.Save) {
    return 1.5;
  }

  return 1.0;
}

function findPlayer(playerId: number, config: MatchConfig): Player | null {
  return (
    config.homeTeam.players.find((p) => p.id === playerId) ??
    config.awayTeam.players.find((p) => p.id === playerId) ??
    null
  );
}
