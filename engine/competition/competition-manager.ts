import type { Club, MatchConfig, MatchFixture } from "@/engine/models";
import { simulate } from "@/engine/simulation";
import { KnockoutBracket } from "./knockout-bracket";
import { MundialBracket } from "./mundial-bracket";

/**
 * Orchestrates the entire season: national knockouts per country + mundial.
 */
export class CompetitionManager {
  private nationalBracketsMap: Map<string, KnockoutBracket>;
  private _mundialBracket: MundialBracket | null = null;
  private readonly clubs: Club[];
  private readonly _baseSeed: number;

  get nationalBrackets(): ReadonlyMap<string, KnockoutBracket> {
    return this.nationalBracketsMap;
  }

  get mundialBracket(): MundialBracket | null {
    return this._mundialBracket;
  }

  get baseSeed(): number {
    return this._baseSeed;
  }

  constructor(clubs: Club[], baseSeed: number) {
    this.clubs = clubs;
    this._baseSeed = baseSeed;
    this.nationalBracketsMap = new Map<string, KnockoutBracket>();

    // Group clubs by country and create national brackets
    const byCountry = new Map<string, number[]>();
    for (const club of clubs) {
      const list = byCountry.get(club.country) ?? [];
      list.push(club.id);
      byCountry.set(club.country, list);
    }

    for (const [country, teamIds] of byCountry) {
      this.nationalBracketsMap.set(country, new KnockoutBracket(teamIds));
    }
  }

  /**
   * Restores a competition manager from saved state. Used by the persistence layer.
   */
  static restore(
    clubs: Club[],
    baseSeed: number,
    nationalBrackets: Map<string, KnockoutBracket>,
    mundialBracket: MundialBracket | null,
  ): CompetitionManager {
    const cm = new CompetitionManager(clubs, baseSeed);
    cm.nationalBracketsMap.clear();
    for (const [key, bracket] of nationalBrackets) {
      cm.nationalBracketsMap.set(key, bracket);
    }
    cm._mundialBracket = mundialBracket;
    return cm;
  }

  /**
   * Generates fixtures for the current national round across all countries.
   */
  generateNationalRound(matchDay: number): MatchFixture[] {
    const allFixtures: MatchFixture[] = [];
    for (const bracket of this.nationalBracketsMap.values()) {
      if (!bracket.isFinished) {
        allFixtures.push(...bracket.generateNextRound(matchDay));
      }
    }
    return allFixtures;
  }

  /**
   * Simulates all fixtures for a given day and records results.
   */
  simulateFixtures(
    fixtures: readonly MatchFixture[],
    daySeed: number,
  ): void {
    for (let i = 0; i < fixtures.length; i++) {
      const fixture = fixtures[i];
      const home = this.clubs.find((c) => c.id === fixture.homeClubId)!;
      const away = this.clubs.find((c) => c.id === fixture.awayClubId)!;

      const config: MatchConfig = {
        homeTeam: home.team,
        awayTeam: away.team,
        seed: daySeed + i,
        homeTactics: null,
        awayTactics: null,
        homeSubstitutions: null,
        awaySubstitutions: null,
        homeCard: null,
        awayCard: null,
      };

      const result = simulate(config);
      fixture.result = [
        result.finalState.scoreHome,
        result.finalState.scoreAway,
      ];
    }
  }

  /**
   * Advances all national brackets after a round is played.
   */
  advanceNationalRounds(): void {
    for (const bracket of this.nationalBracketsMap.values()) {
      if (!bracket.isFinished) {
        bracket.advanceRound();
      }
    }
  }

  /**
   * Returns true if all national brackets are finished.
   */
  areNationalsFinished(): boolean {
    for (const bracket of this.nationalBracketsMap.values()) {
      if (!bracket.isFinished) return false;
    }
    return true;
  }

  /**
   * Creates the mundial bracket from national champions.
   * Call only after all nationals are finished.
   */
  createMundial(): void {
    if (!this.areNationalsFinished()) {
      throw new Error(
        "Cannot create mundial before nationals finish.",
      );
    }

    const champions: number[] = [];
    for (const bracket of this.nationalBracketsMap.values()) {
      champions.push(bracket.championId!);
    }

    this._mundialBracket = new MundialBracket(champions);
  }

  /**
   * Generates fixtures for the current mundial round.
   */
  generateMundialRound(matchDay: number): MatchFixture[] {
    if (this._mundialBracket === null) {
      throw new Error("Mundial not yet created.");
    }
    return this._mundialBracket.generateNextRound(matchDay);
  }

  /**
   * Advances the mundial bracket after a round.
   */
  advanceMundialRound(): void {
    this._mundialBracket?.advanceRound();
  }

  /**
   * Gets the national champion of the country where the given club plays.
   */
  getNationalChampion(country: string): number | null {
    const bracket = this.nationalBracketsMap.get(country);
    return bracket?.championId ?? null;
  }

  /**
   * Gets the mundial champion, or null if not yet decided.
   */
  getMundialChampion(): number | null {
    return this._mundialBracket?.championId ?? null;
  }

  /**
   * Returns true if the entire season (nationals + mundial) is complete.
   */
  isSeasonComplete(): boolean {
    return (
      this.areNationalsFinished() &&
      (this._mundialBracket?.isFinished ?? false)
    );
  }
}
