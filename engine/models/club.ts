import type { Team } from "./team";

/** A club with its team, finances, and reputation. */
export interface Club {
  id: number;
  name: string;
  country: string;
  team: Team;

  /** Club financial balance. Can go negative (triggers bankruptcy). */
  balance: number;

  /** Club reputation 0-100. Affects revenue and job proposals. */
  reputation: number;
}
