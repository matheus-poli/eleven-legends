import type { LockerRoomCard } from "./locker-room-card";
import type { Substitution } from "./substitution";
import type { TacticalSetup } from "./tactical-setup";
import type { Team } from "./team";

/** Configuration for a match. Includes the seed for deterministic RNG. */
export interface MatchConfig {
  homeTeam: Team;
  awayTeam: Team;

  /** Seed for the RNG -- ensures reproducible results. */
  seed: number;

  /** Tactical setup for the home team. Null = use defaults. */
  homeTactics: TacticalSetup | null;

  /** Tactical setup for the away team. Null = use defaults. */
  awayTactics: TacticalSetup | null;

  /** Substitutions to apply at halftime for home team. */
  homeSubstitutions: readonly Substitution[] | null;

  /** Substitutions to apply at halftime for away team. */
  awaySubstitutions: readonly Substitution[] | null;

  /** Locker room card chosen at halftime for home team. Null = none. */
  homeCard: LockerRoomCard | null;

  /** Locker room card chosen at halftime for away team. Null = none. */
  awayCard: LockerRoomCard | null;
}
