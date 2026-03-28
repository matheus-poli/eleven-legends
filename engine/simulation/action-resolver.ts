import { ActionType, Position } from "@/engine/enums";
import type { MatchState, Player, Team } from "@/engine/models";
import type { IRng } from "./rng";
import { calculate } from "./success-calculator";

/** Result of resolving an action in a tick. */
export interface ActionResult {
  action: ActionType;
  executor: Player;
  success: boolean;

  /** The defending player involved (for tackles/interceptions). */
  opponent: Player | null;

  /** Whether this action resulted in a goal. */
  isGoal: boolean;

  /** Whether this was a shot on target (but not necessarily a goal). */
  isShotOnTarget: boolean;

  /** Whether this action resulted in a foul. */
  isFoul: boolean;
}

/**
 * Resolves the full outcome of an action, including secondary effects
 * (goals, fouls, shots on target).
 */

/** Resolves a tick action for the attacking team. */
export function resolveAttack(
  executor: Player,
  action: ActionType,
  assignedPosition: Position,
  defendingTeam: Team,
  state: MatchState,
  rng: IRng,
  bonusModifier: number = 0,
  defenseActiveIds?: readonly number[],
): ActionResult {
  const success = calculate(
    executor,
    action,
    assignedPosition,
    rng,
    bonusModifier,
  );

  if (action === ActionType.Shot) {
    return resolveShotAction(
      executor,
      assignedPosition,
      success,
      defendingTeam,
      state,
      rng,
      defenseActiveIds,
    );
  }

  return {
    action,
    executor,
    success,
    opponent: null,
    isGoal: false,
    isShotOnTarget: false,
    isFoul: false,
  };
}

/** Resolves a tick action for the defending team (tackle/interception). */
export function resolveDefense(
  executor: Player,
  action: ActionType,
  assignedPosition: Position,
  attacker: Player,
  rng: IRng,
): ActionResult {
  const success = calculate(executor, action, assignedPosition, rng);
  const isFoul =
    !success && action === ActionType.Tackle && rng.nextInt(0, 100) < 30;

  return {
    action,
    executor,
    success,
    opponent: attacker,
    isGoal: false,
    isShotOnTarget: false,
    isFoul,
  };
}

function resolveShotAction(
  executor: Player,
  _assignedPosition: Position,
  shotSuccess: boolean,
  defendingTeam: Team,
  state: MatchState,
  rng: IRng,
  defenseActiveIds?: readonly number[],
): ActionResult {
  if (!shotSuccess) {
    return {
      action: ActionType.Shot,
      executor,
      success: false,
      opponent: null,
      isGoal: false,
      isShotOnTarget: false,
      isFoul: false,
    };
  }

  // Shot was on target -- does the goalkeeper save it?
  const isSaved = tryGoalkeeperSave(
    defendingTeam,
    state,
    rng,
    defenseActiveIds,
  );

  return {
    action: ActionType.Shot,
    executor,
    success: !isSaved,
    opponent: null,
    isGoal: !isSaved,
    isShotOnTarget: true,
    isFoul: false,
  };
}

function tryGoalkeeperSave(
  defendingTeam: Team,
  state: MatchState,
  rng: IRng,
  activeIds?: readonly number[],
): boolean {
  const playerIds = activeIds ?? defendingTeam.startingLineup;
  const activeSet = new Set(playerIds);
  const goalkeeper = defendingTeam.players.find(
    (p) => activeSet.has(p.id) && p.primaryPosition === Position.GK,
  );

  if (goalkeeper == null) {
    return false; // no GK = always concedes
  }

  let saveChance =
    (goalkeeper.attributes.reflexes + goalkeeper.attributes.gkPositioning) / 2;
  const stam = state.playerStamina[goalkeeper.id];
  const staminaFactor = stam != null ? stam / 100 : 1;
  saveChance *= staminaFactor;

  const rngValue = rng.nextFloat(-10, 10);
  const threshold = 40; // GK needs to beat this to save -- favors saves for realism

  return saveChance + rngValue >= threshold;
}
