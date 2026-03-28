import { CardEffect } from "@/engine/enums";

/** A locker room card presented at halftime. Player picks 1 of 3. */
export interface LockerRoomCard {
  name: string;
  description: string;
  effect: CardEffect;

  /** Magnitude of the effect (e.g., +15 morale, +20 stamina). */
  magnitude: number;
}
