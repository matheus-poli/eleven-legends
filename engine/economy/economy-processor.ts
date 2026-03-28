import { CompetitionPhase, Position } from "@/engine/enums";
import type { Club } from "@/engine/models";
import { goalkeeperOverall, outfieldOverall } from "@/engine/models";

/**
 * Processes daily economy: match revenue, weekly salary, phase prizes, bankruptcy.
 */

/** Base match-day revenue per reputation point. */
const REVENUE_PER_REPUTATION = 200;

/** Prize money per competition phase. */
const PHASE_PRIZES: Partial<Record<CompetitionPhase, number>> = {
  [CompetitionPhase.Quarterfinals]: 10_000,
  [CompetitionPhase.Semifinals]: 25_000,
  [CompetitionPhase.Final]: 50_000,
  [CompetitionPhase.MundialSemifinals]: 100_000,
  [CompetitionPhase.MundialFinal]: 200_000,
};

/**
 * Processes match-day revenue for a club (gate receipts based on reputation).
 */
export function calculateMatchRevenue(club: Club): number {
  return club.reputation * REVENUE_PER_REPUTATION;
}

/**
 * Calculates the weekly salary bill based on squad average overall.
 */
export function calculateWeeklySalary(club: Club): number {
  const players = club.team.players;
  if (players.length === 0) return 0;

  let totalOverall = 0;
  for (const p of players) {
    totalOverall +=
      p.primaryPosition === Position.GK
        ? goalkeeperOverall(p.attributes)
        : outfieldOverall(p.attributes);
  }
  const avgOverall = totalOverall / players.length;

  // Salary scales with squad quality: better teams pay more
  return avgOverall * players.length * 10;
}

/**
 * Returns the prize for advancing past a competition phase.
 */
export function getPhasePrize(phase: CompetitionPhase): number {
  return PHASE_PRIZES[phase] ?? 0;
}

/**
 * Applies match-day economics to a club.
 */
export function processMatchDay(
  club: Club,
  phase: CompetitionPhase,
  won: boolean,
): void {
  club.balance += calculateMatchRevenue(club);
  if (won) {
    club.balance += getPhasePrize(phase);
  }
}

/**
 * Deducts weekly salary. Call once per week (every 7 days).
 */
export function processWeeklySalary(club: Club): void {
  club.balance -= calculateWeeklySalary(club);
}

/**
 * Returns true if the club is bankrupt (negative balance).
 */
export function isBankrupt(club: Club): boolean {
  return club.balance < 0;
}
