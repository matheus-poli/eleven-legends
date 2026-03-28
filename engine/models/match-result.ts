import type { MatchEvent } from "./match-event";
import type { MatchState } from "./match-state";

/** Final result of a simulated match. */
export interface MatchResult {
  finalState: MatchState;
  events: readonly MatchEvent[];

  /** Player ID with the highest rating (Man of the Match). */
  mvpPlayerId: number;

  /** Player ID with the second highest rating. */
  svpPlayerId: number;
}

/** Convenience accessor for the home score. */
export function scoreHome(result: MatchResult): number {
  return result.finalState.scoreHome;
}

/** Convenience accessor for the away score. */
export function scoreAway(result: MatchResult): number {
  return result.finalState.scoreAway;
}
