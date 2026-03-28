import { EventType } from "@/engine/enums";

/** A single event that occurred during a match tick. */
export interface MatchEvent {
  tick: number;
  type: EventType;

  /** The player who performed the action. */
  playerId: number;

  /** Optional secondary player (e.g., assist provider, fouled player). */
  secondaryPlayerId: number | null;

  /** Human-readable description of the event. */
  description: string;

  /** Rating impact for the primary player. */
  ratingImpact: number;
}
