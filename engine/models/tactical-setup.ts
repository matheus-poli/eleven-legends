import { TacticalStyle } from "@/engine/enums";
import type { Formation } from "./formation";

/** Full tactical setup for a match: formation, style, and lineup. */
export interface TacticalSetup {
  formation: Formation;
  style: TacticalStyle;

  /** Player IDs for the starting 11, matching formation.positions order. */
  startingPlayerIds: readonly number[];
}
