import { ActionType, EventType } from "@/engine/enums";
import type { MatchEvent, Player } from "@/engine/models";
import type { ActionResult } from "./action-resolver";

/**
 * Generates match events from action results.
 */

/** Generates zero or more events from an action result. */
export function generate(
  result: ActionResult,
  tick: number,
  assistProvider?: Player | null,
): MatchEvent[] {
  const events: MatchEvent[] = [];

  if (result.isGoal) {
    events.push({
      tick,
      type: EventType.Goal,
      playerId: result.executor.id,
      secondaryPlayerId: assistProvider?.id ?? null,
      description: `GOAL! ${result.executor.name} scores!`,
      ratingImpact: 1.5,
    });

    if (assistProvider != null) {
      events.push({
        tick,
        type: EventType.Assist,
        playerId: assistProvider.id,
        secondaryPlayerId: null,
        description: `Assist by ${assistProvider.name}`,
        ratingImpact: 1.0,
      });
    }
  } else if (result.isShotOnTarget) {
    events.push({
      tick,
      type: EventType.ShotOnTarget,
      playerId: result.executor.id,
      secondaryPlayerId: null,
      description: `Shot on target by ${result.executor.name} -- saved!`,
      ratingImpact: 0.1,
    });

    // GK save event would be handled separately
  } else if (result.action === ActionType.Shot && !result.success) {
    events.push({
      tick,
      type: EventType.Shot,
      playerId: result.executor.id,
      secondaryPlayerId: null,
      description: `Shot off target by ${result.executor.name}`,
      ratingImpact: -0.1,
    });
  }

  if (result.isFoul) {
    events.push({
      tick,
      type: EventType.Foul,
      playerId: result.executor.id,
      secondaryPlayerId: result.opponent?.id ?? null,
      description: `Foul by ${result.executor.name}`,
      ratingImpact: -0.2,
    });
  }

  // Successful defensive actions
  if (
    result.success &&
    result.action === ActionType.Tackle &&
    !result.isFoul
  ) {
    events.push({
      tick,
      type: EventType.Save, // reusing as "defensive action" for now
      playerId: result.executor.id,
      secondaryPlayerId: null,
      description: `Great tackle by ${result.executor.name}`,
      ratingImpact: 0.3,
    });
  }

  if (result.success && result.action === ActionType.Interception) {
    events.push({
      tick,
      type: EventType.Save,
      playerId: result.executor.id,
      secondaryPlayerId: null,
      description: `Interception by ${result.executor.name}`,
      ratingImpact: 0.3,
    });
  }

  // Successful dribble
  if (result.success && result.action === ActionType.Dribble) {
    events.push({
      tick,
      type: EventType.Shot, // generic positive action event
      playerId: result.executor.id,
      secondaryPlayerId: null,
      description: `Successful dribble by ${result.executor.name}`,
      ratingImpact: 0.2,
    });
  }

  // Failed pass
  if (!result.success && result.action === ActionType.Pass) {
    events.push({
      tick,
      type: EventType.Shot, // generic event
      playerId: result.executor.id,
      secondaryPlayerId: null,
      description: `Bad pass by ${result.executor.name}`,
      ratingImpact: -0.2,
    });
  }

  // Turnover on failed dribble
  if (!result.success && result.action === ActionType.Dribble) {
    events.push({
      tick,
      type: EventType.Shot,
      playerId: result.executor.id,
      secondaryPlayerId: null,
      description: `Lost possession: ${result.executor.name}`,
      ratingImpact: -0.3,
    });
  }

  return events;
}
