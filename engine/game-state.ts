import { DayType } from "@/engine/enums";
import type {
  Club,
  LoanRecord,
  MatchConfig,
  MatchFixture,
  MatchResult,
  ManagerState,
  TacticalSetup,
  TransferRecord,
} from "@/engine/models";
import { winnerClubId } from "@/engine/models";
import { SeededRng } from "@/engine/simulation/rng";
import { CompetitionManager } from "@/engine/competition/competition-manager";
import type { SeasonDay } from "@/engine/competition/season-calendar";
import { buildTemplate } from "@/engine/competition/season-calendar";
import {
  processMatchDay,
  processWeeklySalary,
} from "@/engine/economy/economy-processor";
import { processDay as processAITransferDay } from "@/engine/transfers/ai-transfer-agent";
import { getMaxPlayerId } from "@/engine/transfers/youth-academy";
import {
  updateReputation,
  paySalary,
  checkDismissal,
  checkVictory,
  isGameOver,
  isVictory,
} from "@/engine/career-manager";

/**
 * Summary of what happened during a single day advance.
 */
export interface DayResult {
  day: SeasonDay;
  fixtures: MatchFixture[];
  transferRecords: TransferRecord[];
  gameOver: boolean;
  victory: boolean;
  finished: boolean;
}

/**
 * Context for an interactive match day. Created by prepareMatchDay(), consumed by finishDay().
 */
export interface MatchDayContext {
  allFixtures: MatchFixture[];
  playerFixture: MatchFixture | null;
  playerMatchSeed: number;
  isMundial: boolean;
}

/**
 * Central game state. Tracks the current day, clubs, competitions, and manager.
 * Call advanceDay() for auto-mode, or use prepareMatchDay/finishDay for interactive mode.
 */
export class GameState {
  private readonly _clubs: Club[];
  private _competition: CompetitionManager;
  private _calendar: SeasonDay[];
  private readonly _baseSeed: number;

  private _currentDayIndex: number = 0;
  private _nationalMatchDayCount: number = 0;
  private _mundialMatchDayCount: number = 0;
  private _daysSinceSalary: number = 0;
  private _nextPlayerId: number;
  private _transferDayCount: number = 0;

  readonly manager: ManagerState;
  readonly transferHistory: TransferRecord[] = [];
  readonly activeLoans: LoanRecord[] = [];

  get clubs(): readonly Club[] {
    return this._clubs;
  }

  get competition(): CompetitionManager {
    return this._competition;
  }

  get calendar(): readonly SeasonDay[] {
    return this._calendar;
  }

  get currentDayIndex(): number {
    return this._currentDayIndex;
  }

  get currentDay(): SeasonDay {
    return this._calendar[this._currentDayIndex];
  }

  get isSeasonOver(): boolean {
    return this._currentDayIndex >= this._calendar.length;
  }

  /** Returns the player's club. */
  get playerClub(): Club {
    return this._clubs.find((c) => c.id === this.manager.clubId)!;
  }

  // Exposed for persistence
  get baseSeed(): number {
    return this._baseSeed;
  }
  get nationalMatchDayCount(): number {
    return this._nationalMatchDayCount;
  }
  get mundialMatchDayCount(): number {
    return this._mundialMatchDayCount;
  }
  get daysSinceSalary(): number {
    return this._daysSinceSalary;
  }
  get nextPlayerId(): number {
    return this._nextPlayerId;
  }
  get transferDayCount(): number {
    return this._transferDayCount;
  }

  constructor(clubs: Club[], manager: ManagerState, seed: number) {
    this._clubs = clubs;
    this.manager = manager;
    this._baseSeed = seed;
    this._competition = new CompetitionManager(clubs, seed);
    this._calendar = buildTemplate();
    this._nextPlayerId = getMaxPlayerId(clubs) + 1;
  }

  /**
   * Restores a GameState from saved data. Used by the persistence layer.
   */
  static restore(
    clubs: Club[],
    manager: ManagerState,
    baseSeed: number,
    competition: CompetitionManager,
    calendar: SeasonDay[],
    currentDayIndex: number,
    nationalMatchDayCount: number,
    mundialMatchDayCount: number,
    daysSinceSalary: number,
    nextPlayerId: number,
    transferDayCount: number,
    transferHistory: TransferRecord[],
    activeLoans: LoanRecord[],
  ): GameState {
    const gs = new GameState(clubs, manager, baseSeed);

    // Replace auto-created state with saved state
    gs._currentDayIndex = currentDayIndex;
    gs._nationalMatchDayCount = nationalMatchDayCount;
    gs._mundialMatchDayCount = mundialMatchDayCount;
    gs._daysSinceSalary = daysSinceSalary;
    gs._nextPlayerId = nextPlayerId;
    gs._transferDayCount = transferDayCount;
    gs.transferHistory.push(...transferHistory);
    gs.activeLoans.push(...activeLoans);

    return gs;
  }

  /**
   * Restores using a pre-built CompetitionManager.
   */
  replaceCompetition(competition: CompetitionManager): void {
    this._competition = competition;
  }

  /**
   * Replaces the calendar with saved calendar data.
   */
  replaceCalendar(calendar: SeasonDay[]): void {
    this._calendar = calendar;
  }

  /**
   * Advances one day automatically (no interactive decisions).
   */
  advanceDay(): DayResult {
    if (
      this.isSeasonOver ||
      isGameOver(this.manager) ||
      isVictory(this.manager)
    ) {
      return {
        day: this.currentDay,
        fixtures: [],
        transferRecords: [],
        gameOver: false,
        victory: false,
        finished: true,
      };
    }

    const day = this.currentDay;
    const result: DayResult = {
      day,
      fixtures: [],
      transferRecords: [],
      gameOver: false,
      victory: false,
      finished: false,
    };

    switch (day.type) {
      case DayType.Training:
        this.processTraining();
        break;

      case DayType.MatchDay:
        this.processNationalMatchDay(result);
        break;

      case DayType.MundialMatchDay:
        this.processMundialMatchDay(result);
        break;

      case DayType.TransferWindow:
        this.processTransferDay(result);
        break;
    }

    this.finishDayCommon(result);
    return result;
  }

  /**
   * Returns true if the player's club is still alive in the current competition phase.
   * Use this to decide whether to show "Play Match" or "Advance Day" in the UI.
   */
  isPlayerInCurrentCompetition(): boolean {
    const day = this.currentDay;
    if (day.type === DayType.MatchDay) {
      return this._competition.isTeamInNationals(this.manager.clubId);
    }
    if (day.type === DayType.MundialMatchDay) {
      const mb = this._competition.mundialBracket;
      return mb !== null && mb.hasTeam(this.manager.clubId);
    }
    return false;
  }

  /**
   * Prepares match day: generates fixtures, simulates all non-player matches.
   * Returns the player's fixture (if any) and all fixtures for the day.
   * Use this for interactive mode -- call finishDay() after resolving the player's match.
   */
  prepareMatchDay(): MatchDayContext {
    const day = this.currentDay;
    const isMundial = day.type === DayType.MundialMatchDay;

    let fixtures: MatchFixture[];
    let daySeed: number;

    if (isMundial) {
      this._mundialMatchDayCount++;
      fixtures = this._competition.generateMundialRound(day.day);
      daySeed = this._baseSeed + day.day * 1000 + 500;
    } else {
      this._nationalMatchDayCount++;
      fixtures = this._competition.generateNationalRound(day.day);
      daySeed = this._baseSeed + day.day * 1000;
    }

    // Find player's fixture
    const playerFixture =
      fixtures.find(
        (f) =>
          f.homeClubId === this.manager.clubId ||
          f.awayClubId === this.manager.clubId,
      ) ?? null;

    // Simulate all non-player fixtures (or all fixtures if player is eliminated)
    const otherFixtures = fixtures.filter((f) => f !== playerFixture);
    this._competition.simulateFixtures(otherFixtures, daySeed);

    // Compute the seed for the player's match
    const playerMatchSeed =
      playerFixture !== null
        ? daySeed + fixtures.indexOf(playerFixture)
        : 0;

    return {
      allFixtures: fixtures,
      playerFixture,
      playerMatchSeed,
      isMundial,
    };
  }

  /**
   * Finishes a match day after the player's match has been resolved.
   * Records the player match result, processes economy, career, and advances the day.
   */
  finishDay(
    ctx: MatchDayContext,
    playerResult: MatchResult | null,
  ): DayResult {
    // Record player match result
    if (ctx.playerFixture !== null && playerResult !== null) {
      ctx.playerFixture.result = [
        playerResult.finalState.scoreHome,
        playerResult.finalState.scoreAway,
      ];
    }

    const result: DayResult = {
      day: this.currentDay,
      fixtures: ctx.allFixtures,
      transferRecords: [],
      gameOver: false,
      victory: false,
      finished: false,
    };

    // Economy for ALL fixtures
    for (const fixture of ctx.allFixtures) {
      const home = this._clubs.find(
        (c) => c.id === fixture.homeClubId,
      )!;
      const away = this._clubs.find(
        (c) => c.id === fixture.awayClubId,
      )!;
      const phase = fixture.phase;
      const homeWon = winnerClubId(fixture) === home.id;

      processMatchDay(home, phase, homeWon);
      processMatchDay(away, phase, !homeWon);
    }

    // Advance competition brackets
    if (ctx.isMundial) {
      this._competition.advanceMundialRound();

      // Update manager reputation
      if (ctx.playerFixture !== null) {
        const advanced =
          winnerClubId(ctx.playerFixture) === this.manager.clubId;
        updateReputation(
          this.manager,
          ctx.playerFixture.phase,
          advanced,
        );
      }
    } else {
      this._competition.advanceNationalRounds();

      if (
        this._competition.areNationalsFinished() &&
        this._competition.mundialBracket === null
      ) {
        this._competition.createMundial();
      }
    }

    this.finishDayCommon(result);
    return result;
  }

  /**
   * Returns the MatchConfig for the player's match based on tactical setup.
   */
  buildPlayerMatchConfig(
    ctx: MatchDayContext,
    tactics: TacticalSetup | null,
  ): MatchConfig {
    if (ctx.playerFixture === null) {
      throw new Error("No player fixture on this day.");
    }

    const homeClub = this._clubs.find(
      (c) => c.id === ctx.playerFixture!.homeClubId,
    )!;
    const awayClub = this._clubs.find(
      (c) => c.id === ctx.playerFixture!.awayClubId,
    )!;
    const isHome =
      ctx.playerFixture.homeClubId === this.manager.clubId;

    return {
      homeTeam: homeClub.team,
      awayTeam: awayClub.team,
      seed: ctx.playerMatchSeed,
      homeTactics: isHome ? tactics : null,
      awayTactics: isHome ? null : tactics,
      homeSubstitutions: null,
      awaySubstitutions: null,
      homeCard: null,
      awayCard: null,
    };
  }

  /**
   * Returns the next available player ID and increments the counter.
   */
  getNextPlayerId(count: number = 1): number {
    const id = this._nextPlayerId;
    this._nextPlayerId += count;
    return id;
  }

  /**
   * Records a player transfer in the history.
   */
  recordTransfer(record: TransferRecord): void {
    this.transferHistory.push(record);
  }

  /**
   * Records an active loan.
   */
  recordLoan(loan: LoanRecord): void {
    this.activeLoans.push(loan);
  }

  private processTraining(): void {
    for (const club of this._clubs) {
      const updatedPlayers = club.team.players.map((p) => ({
        ...p,
        morale: Math.min(100, p.morale + 2),
      }));

      club.team = {
        ...club.team,
        players: updatedPlayers,
      };
    }
  }

  private processTransferDay(result: DayResult): void {
    this._transferDayCount++;
    const daySeed = this._baseSeed + this.currentDay.day * 2000;
    const rng = new SeededRng(daySeed);

    // AI clubs make transfers
    const aiRecords = processAITransferDay(
      this._clubs,
      this.manager.clubId,
      rng,
    );
    this.transferHistory.push(...aiRecords);
    result.transferRecords = aiRecords;
  }

  private processNationalMatchDay(result: DayResult): void {
    this._nationalMatchDayCount++;
    const fixtures = this._competition.generateNationalRound(
      this.currentDay.day,
    );
    const daySeed = this._baseSeed + this.currentDay.day * 1000;
    this._competition.simulateFixtures(fixtures, daySeed);
    result.fixtures = fixtures;

    for (const fixture of fixtures) {
      const home = this._clubs.find(
        (c) => c.id === fixture.homeClubId,
      )!;
      const away = this._clubs.find(
        (c) => c.id === fixture.awayClubId,
      )!;
      const phase = fixture.phase;
      const homeWon = winnerClubId(fixture) === home.id;

      processMatchDay(home, phase, homeWon);
      processMatchDay(away, phase, !homeWon);
    }

    this._competition.advanceNationalRounds();

    if (
      this._competition.areNationalsFinished() &&
      this._competition.mundialBracket === null
    ) {
      this._competition.createMundial();
    }
  }

  private processMundialMatchDay(result: DayResult): void {
    this._mundialMatchDayCount++;
    const fixtures = this._competition.generateMundialRound(
      this.currentDay.day,
    );
    const daySeed =
      this._baseSeed + this.currentDay.day * 1000 + 500;
    this._competition.simulateFixtures(fixtures, daySeed);
    result.fixtures = fixtures;

    for (const fixture of fixtures) {
      const home = this._clubs.find(
        (c) => c.id === fixture.homeClubId,
      )!;
      const away = this._clubs.find(
        (c) => c.id === fixture.awayClubId,
      )!;
      const phase = fixture.phase;
      const homeWon = winnerClubId(fixture) === home.id;

      processMatchDay(home, phase, homeWon);
      processMatchDay(away, phase, !homeWon);
    }

    this._competition.advanceMundialRound();

    const playerFixture = fixtures.find(
      (f) =>
        f.homeClubId === this.manager.clubId ||
        f.awayClubId === this.manager.clubId,
    );
    if (playerFixture != null) {
      const advanced =
        winnerClubId(playerFixture) === this.manager.clubId;
      updateReputation(this.manager, playerFixture.phase, advanced);
    }
  }

  private finishDayCommon(result: DayResult): void {
    // Weekly salary (every 7 days)
    this._daysSinceSalary++;
    if (this._daysSinceSalary >= 7) {
      this._daysSinceSalary = 0;
      for (const club of this._clubs) {
        processWeeklySalary(club);
      }
      paySalary(this.manager);
    }

    // Check game end conditions
    checkDismissal(this.manager, this.playerClub);
    if (this._competition.isSeasonComplete()) {
      checkVictory(this.manager, this._competition.getMundialChampion());
    }

    this._currentDayIndex++;

    if (isGameOver(this.manager)) {
      result.gameOver = true;
    }
    if (isVictory(this.manager)) {
      result.victory = true;
    }
    if (this._currentDayIndex >= this._calendar.length) {
      result.finished = true;
    }
  }
}
