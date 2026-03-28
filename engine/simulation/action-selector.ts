import { ActionType, FieldZone, Position } from "@/engine/enums";
import type { MatchState, Player, Team } from "@/engine/models";
import type { IRng } from "./rng";
import { getPrimaryAttribute } from "./success-calculator";

/**
 * Selects which action the attacking/defending team will attempt in a tick,
 * and which player will execute it.
 */

/** Chooses the action based on the current field zone and match context. */
export function selectAction(
  ballZone: FieldZone,
  hasPossession: boolean,
  rng: IRng,
): ActionType {
  if (!hasPossession) {
    // Defending team attempts tackle or interception
    const roll = rng.nextInt(0, 100);
    return roll < 55 ? ActionType.Tackle : ActionType.Interception;
  }

  // Attacking team actions depend on field zone
  switch (ballZone) {
    case FieldZone.DefenseLeft:
    case FieldZone.DefenseCenter:
    case FieldZone.DefenseRight:
      return selectDefensiveZoneAction(rng);
    case FieldZone.MidfieldLeft:
    case FieldZone.MidfieldCenter:
    case FieldZone.MidfieldRight:
      return selectMidfieldZoneAction(rng);
    case FieldZone.AttackLeft:
    case FieldZone.AttackCenter:
    case FieldZone.AttackRight:
      return selectAttackZoneAction(ballZone, rng);
    default:
      return ActionType.Pass;
  }
}

/**
 * Selects the best player to execute the action from the team's starting lineup.
 * Picks the player with the highest relevant attribute (with some RNG variance).
 */
export function selectExecutor(
  team: Team,
  action: ActionType,
  state: MatchState,
  rng: IRng,
  activePlayerIds?: readonly number[],
): Player {
  const activeSet = new Set(
    activePlayerIds != null && activePlayerIds.length > 0
      ? activePlayerIds
      : team.startingLineup,
  );
  let candidates = team.players.filter(
    (p) => activeSet.has(p.id) && isEligibleForAction(p, action, state),
  );

  if (candidates.length === 0) {
    candidates = team.players.filter((p) => activeSet.has(p.id));
  }

  // Weighted selection: higher attribute = higher chance of being selected
  let totalWeight = 0;
  const weights: number[] = new Array(candidates.length);
  for (let i = 0; i < candidates.length; i++) {
    const attr = getPrimaryAttribute(candidates[i].attributes, action);
    const weight = attr + rng.nextFloat(0, 20); // some randomness in selection
    weights[i] = Math.max(weight, 1);
    totalWeight += weights[i];
  }

  const roll = rng.nextFloat(0, totalWeight);
  let cumulative = 0;
  for (let i = 0; i < candidates.length; i++) {
    cumulative += weights[i];
    if (roll <= cumulative) {
      return candidates[i];
    }
  }

  return candidates[candidates.length - 1];
}

/**
 * Advances the ball zone. On success, the ball moves forward with some probability
 * of staying in the same zone (realistic buildup play). On failure, ball retreats.
 */
export function advanceBallZone(
  current: FieldZone,
  success: boolean,
  rng: IRng,
): FieldZone {
  if (success) {
    // Successful action doesn't always advance -- sometimes it maintains position
    const advanceRoll = rng.nextInt(0, 100);

    switch (current) {
      // From defense: 60% advance, 40% stay
      case FieldZone.DefenseLeft:
        return advanceRoll < 60
          ? chooseRandom(rng, FieldZone.MidfieldLeft, FieldZone.MidfieldCenter)
          : current;
      case FieldZone.DefenseCenter:
        return advanceRoll < 60 ? FieldZone.MidfieldCenter : current;
      case FieldZone.DefenseRight:
        return advanceRoll < 60
          ? chooseRandom(
              rng,
              FieldZone.MidfieldRight,
              FieldZone.MidfieldCenter,
            )
          : current;
      // From midfield: 30% advance, 70% stay (realistic buildup)
      case FieldZone.MidfieldLeft:
        return advanceRoll < 30
          ? chooseRandom(rng, FieldZone.AttackLeft, FieldZone.AttackCenter)
          : current;
      case FieldZone.MidfieldCenter:
        return advanceRoll < 30
          ? chooseRandom(
              rng,
              FieldZone.AttackLeft,
              FieldZone.AttackCenter,
              FieldZone.AttackRight,
            )
          : current;
      case FieldZone.MidfieldRight:
        return advanceRoll < 30
          ? chooseRandom(rng, FieldZone.AttackRight, FieldZone.AttackCenter)
          : current;
      // Already in attack zone -- stay
      default:
        return current;
    }
  } else {
    // Failed action: ball retreats
    switch (current) {
      case FieldZone.AttackLeft:
        return chooseRandom(
          rng,
          FieldZone.MidfieldLeft,
          FieldZone.MidfieldCenter,
        );
      case FieldZone.AttackCenter:
        return FieldZone.MidfieldCenter;
      case FieldZone.AttackRight:
        return chooseRandom(
          rng,
          FieldZone.MidfieldRight,
          FieldZone.MidfieldCenter,
        );
      case FieldZone.MidfieldLeft:
        return chooseRandom(
          rng,
          FieldZone.DefenseLeft,
          FieldZone.DefenseCenter,
        );
      case FieldZone.MidfieldCenter:
        return FieldZone.DefenseCenter;
      case FieldZone.MidfieldRight:
        return chooseRandom(
          rng,
          FieldZone.DefenseRight,
          FieldZone.DefenseCenter,
        );
      // Already in defense zone -- stay
      default:
        return current;
    }
  }
}

function selectDefensiveZoneAction(rng: IRng): ActionType {
  const roll = rng.nextInt(0, 100);
  return roll < 80 ? ActionType.Pass : ActionType.Dribble;
}

function selectMidfieldZoneAction(rng: IRng): ActionType {
  const roll = rng.nextInt(0, 100);
  if (roll < 60) return ActionType.Pass;
  if (roll < 80) return ActionType.Dribble;
  if (roll < 95) return ActionType.Cross;
  return ActionType.Shot; // long shot -- rare
}

function selectAttackZoneAction(zone: FieldZone, rng: IRng): ActionType {
  const roll = rng.nextInt(0, 100);

  // Wing zones favor crosses
  if (zone === FieldZone.AttackLeft || zone === FieldZone.AttackRight) {
    if (roll < 40) return ActionType.Cross;
    if (roll < 55) return ActionType.Pass;
    if (roll < 75) return ActionType.Dribble;
    return ActionType.Shot;
  }

  // Center zone: balanced with moderate shot chance
  if (roll < 30) return ActionType.Shot;
  if (roll < 55) return ActionType.Pass;
  if (roll < 80) return ActionType.Dribble;
  return ActionType.Cross;
}

function isEligibleForAction(
  player: Player,
  action: ActionType,
  state: MatchState,
): boolean {
  // GK shouldn't be selected for shots (unless extreme situation)
  if (
    player.primaryPosition === Position.GK &&
    (action === ActionType.Shot || action === ActionType.Dribble)
  ) {
    return false;
  }

  // Defenders shouldn't shoot unless in attack zone
  if (
    (player.primaryPosition === Position.CB ||
      player.primaryPosition === Position.LB ||
      player.primaryPosition === Position.RB) &&
    action === ActionType.Shot &&
    state.ballZone !== FieldZone.AttackLeft &&
    state.ballZone !== FieldZone.AttackCenter &&
    state.ballZone !== FieldZone.AttackRight
  ) {
    return false;
  }

  return true;
}

function chooseRandom(rng: IRng, ...options: FieldZone[]): FieldZone {
  const index = rng.nextInt(0, options.length - 1);
  return options[index];
}
