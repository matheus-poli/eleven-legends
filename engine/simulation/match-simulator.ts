import { FieldZone, MatchPhase } from "@/engine/enums";
import type { MatchConfig, MatchResult, MatchState } from "@/engine/models";
import { createMatchState } from "@/engine/models";
import { applyCard, applySubstitutions } from "./halftime-processor";
import { initializeRatings, getMvpAndSvp } from "./rating-calculator";
import type { IRng } from "./rng";
import { SeededRng } from "./rng";
import { initializeStamina, processTick } from "./tick-processor";

/**
 * Orchestrates a full 90-tick match simulation.
 * Supports both full simulation and split (first half -> halftime decisions -> second half).
 */

export const FIRST_HALF_TICKS = 45;
export const SECOND_HALF_TICKS = 45;
export const TOTAL_TICKS = FIRST_HALF_TICKS + SECOND_HALF_TICKS;

/**
 * Simulates a complete match and returns the result.
 * If MatchConfig contains tactics/cards/subs, they are applied automatically at halftime.
 */
export function simulate(config: MatchConfig): MatchResult {
  const rng = new SeededRng(config.seed);
  const state = initializeState(config);

  // First half
  runHalf(state, config, rng, FIRST_HALF_TICKS);

  // Halftime: apply cards and subs from config
  state.phase = MatchPhase.HalfTime;
  applyConfigHalftimeEffects(state, config);

  // Second half
  state.phase = MatchPhase.SecondHalf;
  state.ballZone = FieldZone.MidfieldCenter;
  runHalf(state, config, rng, SECOND_HALF_TICKS);

  return finalizeResult(state);
}

/**
 * Simulates only the first half. Returns state and RNG for continuation.
 * Use for interactive matches where the player makes halftime decisions.
 */
export function simulateFirstHalf(
  config: MatchConfig,
): { state: MatchState; rng: SeededRng } {
  const rng = new SeededRng(config.seed);
  const state = initializeState(config);

  runHalf(state, config, rng, FIRST_HALF_TICKS);
  state.phase = MatchPhase.HalfTime;

  return { state, rng };
}

/**
 * Simulates the second half after halftime decisions.
 * Call HalftimeProcessor methods before this to apply cards/subs.
 */
export function simulateSecondHalf(
  state: MatchState,
  config: MatchConfig,
  rng: SeededRng,
): MatchResult {
  state.phase = MatchPhase.SecondHalf;
  state.ballZone = FieldZone.MidfieldCenter;
  runHalf(state, config, rng, SECOND_HALF_TICKS);

  return finalizeResult(state);
}

function initializeState(config: MatchConfig): MatchState {
  const state = createMatchState({
    ballPossessionTeamId: config.homeTeam.id,
    ballZone: FieldZone.MidfieldCenter,
    phase: MatchPhase.FirstHalf,
    homeActivePlayerIds: [
      ...(config.homeTactics?.startingPlayerIds ??
        config.homeTeam.startingLineup),
    ],
    awayActivePlayerIds: [
      ...(config.awayTactics?.startingPlayerIds ??
        config.awayTeam.startingLineup),
    ],
  });

  initializeRatings(state, config);
  initializeStamina(state, config);

  return state;
}

function runHalf(
  state: MatchState,
  config: MatchConfig,
  rng: IRng,
  ticks: number,
): void {
  for (let tick = 0; tick < ticks; tick++) {
    processTick(state, config, rng);
  }
}

function applyConfigHalftimeEffects(
  state: MatchState,
  config: MatchConfig,
): void {
  if (config.homeCard != null) {
    applyCard(state, config, config.homeCard, true);
  }
  if (config.awayCard != null) {
    applyCard(state, config, config.awayCard, false);
  }
  if (config.homeSubstitutions != null && config.homeSubstitutions.length > 0) {
    applySubstitutions(state, config, config.homeSubstitutions, true);
  }
  if (config.awaySubstitutions != null && config.awaySubstitutions.length > 0) {
    applySubstitutions(state, config, config.awaySubstitutions, false);
  }
}

function finalizeResult(state: MatchState): MatchResult {
  state.phase = MatchPhase.Finished;
  const { mvpId, svpId } = getMvpAndSvp(state);

  return {
    finalState: state,
    events: [...state.events],
    mvpPlayerId: mvpId,
    svpPlayerId: svpId,
  };
}
