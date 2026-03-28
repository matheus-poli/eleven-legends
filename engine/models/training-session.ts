import { TrainingType } from "@/engine/enums";

/** A training choice offered to the manager. */
export interface TrainingChoice {
  name: string;
  description: string;
  type: TrainingType;
}

/** Result of a training session with per-player events. */
export interface TrainingResult {
  choice: TrainingChoice;
  events: readonly TrainingPlayerEvent[];
}

/** An individual player event during training. */
export interface TrainingPlayerEvent {
  playerId: number;
  playerName: string;
  description: string;
  moraleDelta: number;
  chemistryDelta: number;
  isPositive: boolean;
}
