import { ActionType, Position } from "@/engine/enums";
import type { Player, PlayerAttributes } from "@/engine/models";
import type { IRng } from "./rng";

/**
 * Calculates success chance for a given action.
 * Formula: success_chance = attribute + chemistry_bonus + morale_bonus + trait_bonus + rng_value
 */

/**
 * Calculates the total success chance and compares against a threshold.
 * Returns true if the action succeeds, false otherwise.
 */
export function calculate(
  player: Player,
  action: ActionType,
  assignedPosition: Position,
  rng: IRng,
  bonusModifier: number = 0,
): boolean {
  const successChance =
    calculateRaw(player, action, assignedPosition, rng) + bonusModifier;
  const threshold = getThreshold(action);
  return successChance >= threshold;
}

/**
 * Returns the raw success value (before threshold comparison).
 * Useful for testing and debugging.
 */
export function calculateRaw(
  player: Player,
  action: ActionType,
  assignedPosition: Position,
  rng: IRng,
): number {
  const attribute = getPrimaryAttribute(player.attributes, action);
  const chemistryBonus = getChemistryBonus(player.chemistry);
  const moraleBonus = getMoraleBonus(player.morale);
  const traitBonus = getTraitBonus(player.traits, action);
  const rngValue = rng.nextFloat(-15, 15);
  const positionPenalty = getPositionPenalty(
    player.primaryPosition,
    player.secondaryPosition,
    assignedPosition,
  );

  return (
    attribute * (1 - positionPenalty) +
    chemistryBonus +
    moraleBonus +
    traitBonus +
    rngValue
  );
}

/** Maps an action to its primary attribute. */
export function getPrimaryAttribute(
  attributes: PlayerAttributes,
  action: ActionType,
): number {
  switch (action) {
    case ActionType.Pass:
      return attributes.passing;
    case ActionType.Dribble:
      return attributes.dribbling;
    case ActionType.Shot:
      return attributes.finishing;
    case ActionType.Cross:
      return attributes.technique;
    case ActionType.Tackle:
      return attributes.strength;
    case ActionType.Interception:
      return attributes.anticipation;
    default:
      throw new Error(`Unknown action type: ${action}`);
  }
}

/** Chemistry bonus: 0-20 mapped from chemistry 0-100. */
export function getChemistryBonus(chemistry: number): number {
  return (chemistry / 100) * 20;
}

/** Morale bonus: -10 to +10 mapped from morale 0-100 (50 = neutral). */
export function getMoraleBonus(morale: number): number {
  return ((morale - 50) / 50) * 10;
}

/** Trait bonus: checks if any of the player's traits match the action context. Returns 0-15. */
export function getTraitBonus(
  traits: readonly string[],
  action: ActionType,
): number {
  let bonus = 0;

  for (const trait of traits) {
    bonus += getSingleTraitBonus(trait, action);
  }

  return Math.min(bonus, 15);
}

/**
 * Position penalty based on how far the assigned position is from the player's natural positions.
 * Primary: 0%, Secondary: 10%, Related: 20%, Out of position: 35%.
 */
export function getPositionPenalty(
  primary: Position,
  secondary: Position | null,
  assigned: Position,
): number {
  if (assigned === primary) return 0;
  if (secondary != null && assigned === secondary) return 0.1;
  if (arePositionsRelated(primary, assigned)) return 0.2;
  return 0.35;
}

/** Thresholds vary by action -- shots are hardest, passes are easiest. */
export function getThreshold(action: ActionType): number {
  switch (action) {
    case ActionType.Pass:
      return 40;
    case ActionType.Dribble:
      return 55;
    case ActionType.Shot:
      return 85;
    case ActionType.Cross:
      return 50;
    case ActionType.Tackle:
      return 45;
    case ActionType.Interception:
      return 45;
    default:
      return 50;
  }
}

function getSingleTraitBonus(trait: string, action: ActionType): number {
  if (trait === "Finesse Shot" && action === ActionType.Shot) return 8;
  if (trait === "Power Shot" && action === ActionType.Shot) return 10;
  if (trait === "Close Control" && action === ActionType.Dribble) return 8;
  if (trait === "Through Pass" && action === ActionType.Pass) return 8;
  if (trait === "Interceptor" && action === ActionType.Interception) return 10;
  if (trait === "Aerial Dominance" && action === ActionType.Tackle) return 5;
  if (trait === "Clinical Finisher" && action === ActionType.Shot) return 12;
  if (trait === "Playmaker" && action === ActionType.Pass) return 7;
  if (trait === "Hard Tackler" && action === ActionType.Tackle) return 8;
  if (trait === "Whipped Crosses" && action === ActionType.Cross) return 8;
  return 0;
}

/** Determines if two positions are in the same general area (adapted position = -20%). */
function arePositionsRelated(a: Position, b: Position): boolean {
  return getPositionGroup(a) === getPositionGroup(b);
}

function getPositionGroup(position: Position): number {
  switch (position) {
    case Position.GK:
      return 0;
    case Position.CB:
    case Position.LB:
    case Position.RB:
    case Position.LWB:
    case Position.RWB:
      return 1;
    case Position.CDM:
    case Position.CM:
    case Position.CAM:
    case Position.LM:
    case Position.RM:
      return 2;
    case Position.LW:
    case Position.RW:
    case Position.CF:
    case Position.ST:
      return 3;
    default:
      return -1;
  }
}
