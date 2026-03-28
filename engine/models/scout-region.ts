/** A scoutable region with its name, scouting cost, and name pools for player generation. */
export interface ScoutRegion {
  name: string;
  cost: number;
  firstNames: readonly string[];
  lastNames: readonly string[];
}
