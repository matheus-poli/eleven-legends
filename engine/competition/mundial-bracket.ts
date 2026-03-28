import { CompetitionPhase } from "@/engine/enums";
import type { MatchFixture } from "@/engine/models";
import { winnerClubId } from "@/engine/models";

/**
 * Manages a mundial bracket for 4 national champions: Semis -> Final.
 */
export class MundialBracket {
  private readonly initialTeamIds: number[];
  private fixtures: MatchFixture[] = [];
  private _currentPhase: CompetitionPhase =
    CompetitionPhase.MundialSemifinals;
  private advancingTeams: number[] = [];
  private _championId: number | null = null;

  get allFixtures(): readonly MatchFixture[] {
    return this.fixtures;
  }

  get currentPhase(): CompetitionPhase {
    return this._currentPhase;
  }

  get championId(): number | null {
    return this._championId;
  }

  get isFinished(): boolean {
    return this._currentPhase === CompetitionPhase.Finished;
  }

  constructor(nationalChampionIds: readonly number[]) {
    if (nationalChampionIds.length !== 4) {
      throw new Error("Mundial bracket requires exactly 4 teams.");
    }
    this.initialTeamIds = [...nationalChampionIds];
  }

  /**
   * Restores a mundial bracket from saved state. Used by the persistence layer.
   */
  static restore(
    initialTeamIds: readonly number[],
    fixtures: MatchFixture[],
    currentPhase: CompetitionPhase,
    advancingTeams: number[],
    championId: number | null,
  ): MundialBracket {
    const bracket = new MundialBracket(initialTeamIds);
    bracket.fixtures = [...fixtures];
    bracket._currentPhase = currentPhase;
    bracket.advancingTeams = [...advancingTeams];
    bracket._championId = championId;
    return bracket;
  }

  /**
   * Generates fixtures for the current phase.
   */
  generateNextRound(startDay: number): MatchFixture[] {
    if (this.isFinished) return [];

    const teams =
      this._currentPhase === CompetitionPhase.MundialSemifinals
        ? this.initialTeamIds
        : this.advancingTeams;

    const roundFixtures: MatchFixture[] = [];
    for (let i = 0; i < teams.length; i += 2) {
      roundFixtures.push({
        day: startDay,
        homeClubId: teams[i],
        awayClubId: teams[i + 1],
        phase: this._currentPhase,
        result: null,
      });
    }

    this.fixtures.push(...roundFixtures);
    return roundFixtures;
  }

  /**
   * Records results and advances the bracket.
   */
  advanceRound(): void {
    this.advancingTeams = this.fixtures
      .filter(
        (f) =>
          f.phase === this._currentPhase && winnerClubId(f) !== null,
      )
      .map((f) => winnerClubId(f)!);

    switch (this._currentPhase) {
      case CompetitionPhase.MundialSemifinals:
        this._currentPhase = CompetitionPhase.MundialFinal;
        break;
      case CompetitionPhase.MundialFinal:
        this._currentPhase = CompetitionPhase.Finished;
        break;
      default:
        break;
    }

    if (
      this._currentPhase === CompetitionPhase.Finished &&
      this.advancingTeams.length === 1
    ) {
      this._championId = this.advancingTeams[0];
    }
  }
}
