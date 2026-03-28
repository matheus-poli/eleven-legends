import type { Formation } from "./formation";
import { F442, F433, F352, F4231, F532 } from "./formation";

/**
 * Normalized (0-1) pitch coordinate.
 * X: 0=left, 1=right. Y: 0=top (attacking), 1=bottom (GK end).
 * Positions spread wide enough for ~100px cards at 1080p.
 */
export type PitchCoord = [x: number, y: number];

// 4-4-2: GK, LB, CB, CB, RB, LM, CM, CM, RM, ST, ST
const POSITIONS_442: readonly PitchCoord[] = [
  [0.5, 0.9], // GK
  [0.1, 0.72], // LB
  [0.33, 0.76], // CB
  [0.67, 0.76], // CB
  [0.9, 0.72], // RB
  [0.1, 0.46], // LM
  [0.35, 0.5], // CM
  [0.65, 0.5], // CM
  [0.9, 0.46], // RM
  [0.33, 0.2], // ST
  [0.67, 0.2], // ST
];

// 4-3-3: GK, LB, CB, CB, RB, CM, CM, CAM, LW, ST, RW
const POSITIONS_433: readonly PitchCoord[] = [
  [0.5, 0.9], // GK
  [0.1, 0.72], // LB
  [0.33, 0.76], // CB
  [0.67, 0.76], // CB
  [0.9, 0.72], // RB
  [0.28, 0.5], // CM
  [0.72, 0.5], // CM
  [0.5, 0.4], // CAM
  [0.1, 0.22], // LW
  [0.5, 0.12], // ST
  [0.9, 0.22], // RW
];

// 3-5-2: GK, CB, CB, CB, LM, CDM, CM, CAM, RM, ST, ST
const POSITIONS_352: readonly PitchCoord[] = [
  [0.5, 0.9], // GK
  [0.22, 0.74], // CB
  [0.5, 0.78], // CB
  [0.78, 0.74], // CB
  [0.07, 0.46], // LM
  [0.5, 0.58], // CDM
  [0.28, 0.44], // CM
  [0.5, 0.32], // CAM
  [0.93, 0.46], // RM
  [0.33, 0.14], // ST
  [0.67, 0.14], // ST
];

// 4-2-3-1: GK, LB, CB, CB, RB, CDM, CDM, LW, CAM, RW, ST
const POSITIONS_4231: readonly PitchCoord[] = [
  [0.5, 0.9], // GK
  [0.1, 0.72], // LB
  [0.33, 0.76], // CB
  [0.67, 0.76], // CB
  [0.9, 0.72], // RB
  [0.33, 0.56], // CDM
  [0.67, 0.56], // CDM
  [0.1, 0.32], // LW
  [0.5, 0.36], // CAM
  [0.9, 0.32], // RW
  [0.5, 0.12], // ST
];

// 5-3-2: GK, LWB, CB, CB, CB, RWB, CM, CM, CAM, ST, ST
const POSITIONS_532: readonly PitchCoord[] = [
  [0.5, 0.9], // GK
  [0.07, 0.64], // LWB
  [0.28, 0.76], // CB
  [0.5, 0.78], // CB
  [0.72, 0.76], // CB
  [0.93, 0.64], // RWB
  [0.28, 0.46], // CM
  [0.72, 0.46], // CM
  [0.5, 0.34], // CAM
  [0.33, 0.14], // ST
  [0.67, 0.14], // ST
];

/** Returns the pitch coordinates for each position slot in a given formation. */
export function getFormationPositions(
  formation: Formation,
): readonly PitchCoord[] {
  if (formation === F442) return POSITIONS_442;
  if (formation === F433) return POSITIONS_433;
  if (formation === F352) return POSITIONS_352;
  if (formation === F4231) return POSITIONS_4231;
  if (formation === F532) return POSITIONS_532;
  return POSITIONS_442;
}
