import { CompetitionPhase, ManagerStatus } from "@/engine/enums";
import type { Club, ManagerState } from "@/engine/models";
import { isBankrupt } from "@/engine/economy/economy-processor";

/**
 * Manages the coach's career: reputation, dismissal, win condition.
 */

/** Reputation gain per phase survived. */
const REPUTATION_PER_PHASE = 5;

/** Reputation bonus for winning a title. */
const REPUTATION_FOR_TITLE = 20;

/** Reputation loss for early elimination. */
const REPUTATION_LOSS_ELIMINATION = 5;

/**
 * Updates reputation after a competition phase result.
 */
export function updateReputation(
  manager: ManagerState,
  phase: CompetitionPhase,
  advanced: boolean,
): void {
  if (advanced) {
    manager.reputation = Math.min(
      100,
      manager.reputation + REPUTATION_PER_PHASE,
    );

    if (
      phase === CompetitionPhase.Final ||
      phase === CompetitionPhase.MundialFinal
    ) {
      manager.reputation = Math.min(
        100,
        manager.reputation + REPUTATION_FOR_TITLE,
      );
    }
  } else {
    manager.reputation = Math.max(
      0,
      manager.reputation - REPUTATION_LOSS_ELIMINATION,
    );
  }
}

/**
 * Pays the manager's monthly salary. Call once per week for simplicity.
 */
export function paySalary(manager: ManagerState): void {
  manager.personalBalance += manager.salary;
}

/**
 * Checks if the manager should be dismissed (club bankrupt).
 * In the demo, dismissal is instant game over.
 */
export function checkDismissal(
  manager: ManagerState,
  club: Club,
): void {
  if (isBankrupt(club)) {
    manager.status = ManagerStatus.Dismissed;
  }
}

/**
 * Checks if the manager won the mundial -- triggers the win screen.
 */
export function checkVictory(
  manager: ManagerState,
  mundialChampionId: number | null,
): void {
  if (
    mundialChampionId !== null &&
    mundialChampionId === manager.clubId
  ) {
    manager.status = ManagerStatus.Winner;
  }
}

/**
 * In the demo, dismissal = game over directly.
 */
export function isGameOver(manager: ManagerState): boolean {
  return (
    manager.status === ManagerStatus.Dismissed ||
    manager.status === ManagerStatus.GameOver
  );
}

/**
 * Returns true if the manager won the mundial.
 */
export function isVictory(manager: ManagerState): boolean {
  return manager.status === ManagerStatus.Winner;
}
