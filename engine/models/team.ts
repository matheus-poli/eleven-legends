import type { Player } from "./player";

/** A team with its squad and starting lineup. */
export interface Team {
  id: number;
  name: string;
  players: readonly Player[];

  /** List of player IDs in the starting 11, ordered by position. */
  startingLineup: readonly number[];
}
