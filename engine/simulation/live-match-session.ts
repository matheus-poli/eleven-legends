import { FieldZone, MatchPhase } from "@/engine/enums";
import type {
  MatchConfig,
  MatchEvent,
  MatchResult,
  MatchState,
  Substitution,
} from "@/engine/models";
import { createMatchState } from "@/engine/models";
import { applySubstitutions } from "./halftime-processor";
import { FIRST_HALF_TICKS, SECOND_HALF_TICKS } from "./match-simulator";
import { initializeRatings, getMvpAndSvp } from "./rating-calculator";
import { SeededRng } from "./rng";
import { initializeStamina, processTick } from "./tick-processor";

/**
 * Incremental match session that processes one tick at a time.
 * Wraps the same TickProcessor logic as MatchSimulator but exposes
 * each tick individually for real-time UI playback.
 */
export class LiveMatchSession {
  private readonly config: MatchConfig;
  private readonly rng: SeededRng;
  private readonly matchState: MatchState;

  /** Current tick within the half (0-44). */
  private halfTickCount: number = 0;

  get state(): MatchState {
    return this.matchState;
  }

  get matchConfig(): MatchConfig {
    return this.config;
  }

  get halfTick(): number {
    return this.halfTickCount;
  }

  get isHalfTimeReached(): boolean {
    return this.matchState.phase === MatchPhase.HalfTime;
  }

  get isSecondHalf(): boolean {
    return this.matchState.phase === MatchPhase.SecondHalf;
  }

  get isMatchFinished(): boolean {
    return this.matchState.phase === MatchPhase.Finished;
  }

  constructor(config: MatchConfig) {
    this.config = config;
    this.rng = new SeededRng(config.seed);
    this.matchState = LiveMatchSession.initializeState(config);
  }

  /**
   * Processes a single tick and returns events generated this tick.
   * Returns empty array if match is finished or at halftime.
   */
  processNextTick(): readonly MatchEvent[] {
    if (this.isMatchFinished || this.isHalfTimeReached) {
      return [];
    }

    const eventsBefore = this.matchState.events.length;
    processTick(this.matchState, this.config, this.rng);
    this.halfTickCount++;

    // Collect only the events from this tick
    const newEvents = this.matchState.events.slice(eventsBefore);

    // Check if half is over
    if (
      this.halfTickCount >= FIRST_HALF_TICKS &&
      this.matchState.phase === MatchPhase.FirstHalf
    ) {
      this.matchState.phase = MatchPhase.HalfTime;
    } else if (
      this.halfTickCount >= SECOND_HALF_TICKS &&
      this.matchState.phase === MatchPhase.SecondHalf
    ) {
      this.matchState.phase = MatchPhase.Finished;
    }

    return newEvents;
  }

  /** Starts the second half. Call after applying halftime effects. */
  startSecondHalf(): void {
    this.matchState.phase = MatchPhase.SecondHalf;
    this.matchState.ballZone = FieldZone.MidfieldCenter;
    this.halfTickCount = 0;
  }

  /**
   * Applies a live substitution. Returns true if successful.
   * Max 3 per team per half.
   */
  applySubstitution(sub: Substitution, isHomeTeam: boolean): boolean {
    const used = isHomeTeam
      ? this.matchState.homeSubstitutionsUsed
      : this.matchState.awaySubstitutionsUsed;
    if (used >= 3) return false;

    applySubstitutions(this.matchState, this.config, [sub], isHomeTeam);
    return true;
  }

  /**
   * Finalizes the match and returns the result.
   * Call only when isMatchFinished is true.
   */
  finalizeResult(): MatchResult {
    this.matchState.phase = MatchPhase.Finished;
    const { mvpId, svpId } = getMvpAndSvp(this.matchState);

    return {
      finalState: this.matchState,
      events: [...this.matchState.events],
      mvpPlayerId: mvpId,
      svpPlayerId: svpId,
    };
  }

  private static initializeState(config: MatchConfig): MatchState {
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
}
