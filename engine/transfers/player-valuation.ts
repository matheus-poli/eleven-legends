import { Position } from "@/engine/enums";
import type { Player } from "@/engine/models";
import { goalkeeperOverall, outfieldOverall } from "@/engine/models";

/**
 * Calculates market value for players based on overall rating, age, and club reputation.
 */

/**
 * Calculates a player's market value in the transfer market.
 */
export function calculate(player: Player, clubReputation: number = 50): number {
  const overall =
    player.primaryPosition === Position.GK
      ? goalkeeperOverall(player.attributes)
      : outfieldOverall(player.attributes);

  // Exponential base value from overall (55 ovr ~ 30k, 65 ~ 80k, 75 ~ 300k, 85 ~ 1.2M)
  const baseValue = Math.pow(overall / 10.0, 4.0) * 30;

  // Age factor: peak 25-28, young premium, old discount
  const ageFactor = getAgeFactor(player.age);

  // Club reputation adds 0-30% markup
  const repBonus = 1 + clubReputation / 500;

  const value = baseValue * ageFactor * repBonus;

  // Minimum value, rounded to nearest 1000
  return Math.max(5_000, Math.round(value / 1000) * 1000);
}

/**
 * Gets a salary estimate for a player (weekly cost).
 */
export function estimateWeeklySalary(player: Player): number {
  const overall =
    player.primaryPosition === Position.GK
      ? goalkeeperOverall(player.attributes)
      : outfieldOverall(player.attributes);

  return overall * 10;
}

function getAgeFactor(age: number): number {
  if (age <= 17) return 0.6;
  switch (age) {
    case 18:
      return 0.8;
    case 19:
      return 0.9;
    case 20:
      return 1.0;
    case 21:
      return 1.05;
    case 22:
      return 1.1;
    case 23:
      return 1.15;
    case 24:
      return 1.2;
    case 25:
    case 26:
    case 27:
    case 28:
      return 1.2;
    case 29:
      return 1.1;
    case 30:
      return 0.95;
    case 31:
      return 0.8;
    case 32:
      return 0.65;
    case 33:
      return 0.5;
    case 34:
      return 0.35;
    default:
      return 0.25;
  }
}
