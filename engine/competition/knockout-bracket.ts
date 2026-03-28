import { CompetitionPhase } from "@/engine/enums";
import type { MatchFixture } from "@/engine/models";
import { winnerClubId } from "@/engine/models";

/**
 * Manages a knockout bracket for 8 teams: Quarters -> Semis -> Final.
 */
export class KnockoutBracket {
  private readonly initialTeamIds: number[];
  private fixtures: MatchFixture[] = [];
  private _currentPhase: CompetitionPhase = CompetitionPhase.Quarterfinals;
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

  constructor(seededTeamIds: readonly number[]) {
    if (seededTeamIds.length !== 8) {
      throw new Error("Knockout bracket requires exactly 8 teams.");
    }
    this.initialTeamIds = [...seededTeamIds];
  }

  /**
   * Restores a bracket from saved state. Used by the persistence layer.
   */
  static restore(
    initialTeamIds: readonly number[],
    fixtures: MatchFixture[],
    currentPhase: CompetitionPhase,
    advancingTeams: number[],
    championId: number | null,
  ): KnockoutBracket {
    const bracket = new KnockoutBracket(initialTeamIds);
    bracket.fixtures = [...fixtures];
    bracket._currentPhase = currentPhase;
    bracket.advancingTeams = [...advancingTeams];
    bracket._championId = championId;
    return bracket;
  }

  /**
   * Generates fixtures for the current phase. Returns them for scheduling.
   */
  generateNextRound(startDay: number): MatchFixture[] {
    if (this.isFinished) return [];

    const teams =
      this._currentPhase === CompetitionPhase.Quarterfinals
        ? this.initialTeamIds
        : this.advancingTeams;

    const roundFixtures = this.generatePairings(teams, startDay);
    this.fixtures.push(...roundFixtures);
    return roundFixtures;
  }

  /**
   * Records results and advances the bracket. Call after all fixtures in the round are played.
   */
  advanceRound(): void {
    this.advancingTeams = this.fixtures
      .filter(
        (f) =>
          f.phase === this._currentPhase && winnerClubId(f) !== null,
      )
      .map((f) => winnerClubId(f)!);

    switch (this._currentPhase) {
      case CompetitionPhase.Quarterfinals:
        this._currentPhase = CompetitionPhase.Semifinals;
        break;
      case CompetitionPhase.Semifinals:
        this._currentPhase = CompetitionPhase.Final;
        break;
      case CompetitionPhase.Final:
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

  private generatePairings(
    teamIds: number[],
    startDay: number,
  ): MatchFixture[] {
    const fixtures: MatchFixture[] = [];
    for (let i = 0; i < teamIds.length; i += 2) {
      fixtures.push({
        day: startDay,
        homeClubId: teamIds[i],
        awayClubId: teamIds[i + 1],
        phase: this._currentPhase,
        result: null,
      });
    }
    return fixtures;
  }
}
