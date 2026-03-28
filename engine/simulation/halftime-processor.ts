import { CardEffect } from "@/engine/enums";
import type {
  LockerRoomCard,
  MatchConfig,
  MatchState,
  Substitution,
} from "@/engine/models";
import { BASE_RATING } from "./rating-calculator";

/**
 * Applies halftime effects: locker room cards and substitutions.
 */

/** Applies a locker room card's effects to the match state. */
export function applyCard(
  state: MatchState,
  config: MatchConfig,
  card: LockerRoomCard,
  isHomeTeam: boolean,
): void {
  const team = isHomeTeam ? config.homeTeam : config.awayTeam;
  const activeIds = isHomeTeam
    ? state.homeActivePlayerIds
    : state.awayActivePlayerIds;
  const playerIds =
    activeIds.length > 0 ? activeIds : (team.startingLineup as readonly number[]);

  switch (card.effect) {
    case CardEffect.MoraleBoost:
      // Bonus modifier applied to all success calculations in 2nd half
      if (isHomeTeam) {
        state.homeBonusModifier += card.magnitude * 0.15;
      } else {
        state.awayBonusModifier += card.magnitude * 0.15;
      }
      break;

    case CardEffect.StaminaRecovery:
      for (const pid of playerIds) {
        const stam = state.playerStamina[pid];
        if (stam != null) {
          state.playerStamina[pid] = Math.min(100, stam + card.magnitude);
        }
      }
      break;

    case CardEffect.TeamBuff:
      if (isHomeTeam) {
        state.homeBonusModifier += card.magnitude;
      } else {
        state.awayBonusModifier += card.magnitude;
      }
      break;

    case CardEffect.OpponentDebuff:
      // Apply negative modifier to the opponent
      if (isHomeTeam) {
        state.awayBonusModifier -= card.magnitude * 0.5;
      } else {
        state.homeBonusModifier -= card.magnitude * 0.5;
      }
      break;
  }
}

/** Processes substitutions: swaps players in active lineup, initializes stamina for subs. */
export function applySubstitutions(
  state: MatchState,
  config: MatchConfig,
  subs: readonly Substitution[],
  isHomeTeam: boolean,
): void {
  const team = isHomeTeam ? config.homeTeam : config.awayTeam;
  const activeIds = isHomeTeam
    ? state.homeActivePlayerIds
    : state.awayActivePlayerIds;

  for (const sub of subs) {
    const subsUsed = isHomeTeam
      ? state.homeSubstitutionsUsed
      : state.awaySubstitutionsUsed;
    if (subsUsed >= 3) break;

    const outIdx = activeIds.indexOf(sub.playerOutId);
    if (outIdx < 0) continue;

    const incoming = team.players.find((p) => p.id === sub.playerInId);
    if (incoming == null) continue;

    // Swap in active lineup
    activeIds[outIdx] = sub.playerInId;

    // Initialize stamina and rating for incoming player
    state.playerStamina[sub.playerInId] = incoming.attributes.stamina;
    state.playerRatings[sub.playerInId] = BASE_RATING;

    if (isHomeTeam) {
      state.homeSubstitutionsUsed++;
    } else {
      state.awaySubstitutionsUsed++;
    }
  }
}
