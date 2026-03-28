import { Position } from "@/engine/enums";
import type { PlayerAttributes } from "./player-attributes";

/** Represents a player in the squad. */
export interface Player {
  id: number;
  name: string;
  primaryPosition: Position;
  secondaryPosition: Position | null;
  attributes: PlayerAttributes;
  traits: readonly string[];
  age: number;

  /** Emotional state affecting performance. 0-100. */
  morale: number;

  /** Cohesion bonus. 0-100. */
  chemistry: number;
}
