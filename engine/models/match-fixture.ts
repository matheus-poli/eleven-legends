import { CompetitionPhase } from "@/engine/enums";

/** Match result tuple: [homeGoals, awayGoals]. */
export type FixtureResult = [home: number, away: number];

/** A scheduled match between two clubs in a competition phase. */
export interface MatchFixture {
  day: number;
  homeClubId: number;
  awayClubId: number;
  phase: CompetitionPhase;

  /** Match result. Null until played. */
  result: FixtureResult | null;
}

/** Returns the winning club ID, or null if not yet played. */
export function winnerClubId(fixture: MatchFixture): number | null {
  if (fixture.result === null) return null;
  const [home, away] = fixture.result;
  if (home > away) return fixture.homeClubId;
  if (away > home) return fixture.awayClubId;
  // Draw goes to home team (simplified -- no penalty shootouts)
  return fixture.homeClubId;
}
