import { ActionType, EventType, FieldZone } from "@/engine/enums";
import type { MatchConfig, MatchEvent, MatchState, Player, Team } from "@/engine/models";
import { resolveAttack, resolveDefense } from "./action-resolver";
import { advanceBallZone, selectAction, selectExecutor } from "./action-selector";
import { generate } from "./event-generator";
import { resolve as resolvePossession } from "./possession-resolver";
import { applyEvents } from "./rating-calculator";
import type { IRng } from "./rng";

/**
 * Processes a single tick of a match. Each tick = 1 minute of game time.
 * Follows the simulation loop: possession -> action -> executor -> success -> result -> event -> rating -> stamina.
 */

const STAMINA_DRAIN_PER_TICK = 0.8;

/** Processes one tick and updates the MatchState accordingly. */
export function processTick(
  state: MatchState,
  config: MatchConfig,
  rng: IRng,
): void {
  state.currentTick++;
  state.totalTicksPlayed++;

  // 1. Resolve possession
  const possessionTeamId = resolvePossession(state, config, rng);
  state.ballPossessionTeamId = possessionTeamId;

  if (possessionTeamId === config.homeTeam.id) {
    state.homePossessionTicks++;
  }

  // Update possession percentage
  if (state.totalTicksPlayed > 0) {
    state.possessionHome =
      state.homePossessionTicks / state.totalTicksPlayed;
  }

  const isHomeAttacking = possessionTeamId === config.homeTeam.id;
  const attackingTeam = isHomeAttacking ? config.homeTeam : config.awayTeam;
  const defendingTeam = isHomeAttacking ? config.awayTeam : config.homeTeam;

  // Use active player IDs for executor selection (supports substitutions)
  const attackActiveIds = isHomeAttacking
    ? state.homeActivePlayerIds
    : state.awayActivePlayerIds;
  const defenseActiveIds = isHomeAttacking
    ? state.awayActivePlayerIds
    : state.homeActivePlayerIds;

  // 2. Choose action for the attacking team
  const attackAction = selectAction(state.ballZone, true, rng);

  // 3. Select executor (use active IDs if available)
  const executor = selectExecutor(
    attackingTeam,
    attackAction,
    state,
    rng,
    attackActiveIds,
  );

  // 4-5. Calculate success and apply result (with bonus modifier)
  const bonusModifier = isHomeAttacking
    ? state.homeBonusModifier
    : state.awayBonusModifier;
  const attackResult = resolveAttack(
    executor,
    attackAction,
    executor.primaryPosition,
    defendingTeam,
    state,
    rng,
    bonusModifier,
    defenseActiveIds,
  );

  // Track the last successful passer for potential assists
  let assistProvider: Player | null = null;
  if (attackResult.isGoal && attackAction === ActionType.Shot) {
    // Look for recent successful pass/cross in the last few events
    assistProvider = findAssistProvider(state, attackingTeam, executor.id);
  }

  // 6. Generate events
  const events: MatchEvent[] = generate(
    attackResult,
    state.currentTick,
    assistProvider,
  );

  // Apply goal to score
  if (attackResult.isGoal) {
    if (possessionTeamId === config.homeTeam.id) {
      state.scoreHome++;
    } else {
      state.scoreAway++;
    }

    // Kickoff: conceding team gets possession from midfield
    state.ballZone = FieldZone.MidfieldCenter;
    state.ballPossessionTeamId =
      possessionTeamId === config.homeTeam.id
        ? config.awayTeam.id
        : config.homeTeam.id;
  } else if (attackResult.isShotOnTarget) {
    // Saved shot: defending team gets possession from defense
    state.ballZone = FieldZone.DefenseCenter;
    state.ballPossessionTeamId = defendingTeam.id;
  } else if (!attackResult.success) {
    // Failed action: ball retreats, possible turnover
    state.ballZone = advanceBallZone(state.ballZone, false, rng);
  } else {
    // Successful non-shot action: advance ball zone
    state.ballZone = advanceBallZone(state.ballZone, true, rng);
  }

  // Also process defensive reaction if the attack failed and no goal/foul
  if (!attackResult.success && !attackResult.isGoal && !attackResult.isFoul) {
    const defenseAction = selectAction(state.ballZone, false, rng);
    const defender = selectExecutor(defendingTeam, defenseAction, state, rng);
    const defenseResult = resolveDefense(
      defender,
      defenseAction,
      defender.primaryPosition,
      executor,
      rng,
    );

    const defenseEvents = generate(defenseResult, state.currentTick);
    events.push(...defenseEvents);

    // Foul from tackle gives attacking team a free kick
    if (defenseResult.isFoul) {
      events.push({
        tick: state.currentTick,
        type: EventType.FreeKick,
        playerId: executor.id,
        secondaryPlayerId: null,
        description: `Free kick for ${attackingTeam.name}`,
        ratingImpact: 0,
      });
    }
  }

  // 7. Update ratings
  state.events.push(...events);
  applyEvents(state, events, config);

  // 8. Degrade stamina for all starting players
  degradeStamina(state, config);
}

/** Initializes stamina for all starting players based on their Stamina attribute. */
export function initializeStamina(
  state: MatchState,
  config: MatchConfig,
): void {
  const homeIds =
    state.homeActivePlayerIds.length > 0
      ? state.homeActivePlayerIds
      : config.homeTeam.startingLineup;
  const awayIds =
    state.awayActivePlayerIds.length > 0
      ? state.awayActivePlayerIds
      : config.awayTeam.startingLineup;

  for (const playerId of homeIds) {
    const player = config.homeTeam.players.find((p) => p.id === playerId);
    if (player != null) {
      state.playerStamina[playerId] = player.attributes.stamina;
    }
  }
  for (const playerId of awayIds) {
    const player = config.awayTeam.players.find((p) => p.id === playerId);
    if (player != null) {
      state.playerStamina[playerId] = player.attributes.stamina;
    }
  }
}

function degradeStamina(state: MatchState, config: MatchConfig): void {
  // Use active player IDs (supports substitutions)
  const homeIds =
    state.homeActivePlayerIds.length > 0
      ? state.homeActivePlayerIds
      : config.homeTeam.startingLineup;
  const awayIds =
    state.awayActivePlayerIds.length > 0
      ? state.awayActivePlayerIds
      : config.awayTeam.startingLineup;

  const allIds = [...homeIds, ...awayIds];
  for (const playerId of allIds) {
    if (state.playerStamina[playerId] != null) {
      state.playerStamina[playerId] = Math.max(
        0,
        state.playerStamina[playerId] - STAMINA_DRAIN_PER_TICK,
      );
    }
  }
}

function findAssistProvider(
  state: MatchState,
  attackingTeam: Team,
  scorerId: number,
): Player | null {
  // Look at recent events for a successful pass/cross by a teammate
  const recentEvent = state.events
    .filter(
      (e) =>
        e.tick >= state.currentTick - 3 &&
        e.playerId !== scorerId &&
        e.ratingImpact > 0,
    )
    .at(-1);

  if (recentEvent == null) return null;

  const startingSet = new Set(attackingTeam.startingLineup);
  return (
    attackingTeam.players.find(
      (p) => p.id === recentEvent.playerId && startingSet.has(p.id),
    ) ?? null
  );
}
