import { FieldZone, MatchPhase } from "@/engine/enums";
import type { MatchEvent } from "./match-event";

/**
 * Mutable state of an in-progress match. Updated each tick.
 */
export interface MatchState {
  scoreHome: number;
  scoreAway: number;

  /** Home team possession ratio. 0.0-1.0. */
  possessionHome: number;

  currentTick: number;
  phase: MatchPhase;

  /** Which team currently has the ball (home or away team ID). */
  ballPossessionTeamId: number;

  /** Current zone of the ball on the field. */
  ballZone: FieldZone;

  /** All events generated so far. */
  events: MatchEvent[];

  /** Current rating per player. Key = PlayerId, Value = rating (base 6.0). */
  playerRatings: Record<number, number>;

  /** Current stamina per player. Key = PlayerId, Value = stamina (0-100, degrades). */
  playerStamina: Record<number, number>;

  /** Ticks where home team had possession (for possession % calculation). */
  homePossessionTicks: number;

  /** Total ticks played so far. */
  totalTicksPlayed: number;

  /** Number of substitutions used by each team. */
  homeSubstitutionsUsed: number;
  awaySubstitutionsUsed: number;

  /** Active player IDs on the pitch for each team (updated on substitution). */
  homeActivePlayerIds: number[];
  awayActivePlayerIds: number[];

  /** Bonus modifiers from halftime cards, applied to success calculations in 2nd half. */
  homeBonusModifier: number;
  awayBonusModifier: number;
}

/** Creates a new MatchState with sensible defaults. */
export function createMatchState(
  overrides: Partial<MatchState> = {},
): MatchState {
  return {
    scoreHome: 0,
    scoreAway: 0,
    possessionHome: 0.5,
    currentTick: 0,
    phase: MatchPhase.FirstHalf,
    ballPossessionTeamId: 0,
    ballZone: FieldZone.MidfieldCenter,
    events: [],
    playerRatings: {},
    playerStamina: {},
    homePossessionTicks: 0,
    totalTicksPlayed: 0,
    homeSubstitutionsUsed: 0,
    awaySubstitutionsUsed: 0,
    homeActivePlayerIds: [],
    awayActivePlayerIds: [],
    homeBonusModifier: 0,
    awayBonusModifier: 0,
    ...overrides,
  };
}
