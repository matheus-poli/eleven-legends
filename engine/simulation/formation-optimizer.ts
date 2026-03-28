import { Position } from "@/engine/enums";
import type { Formation, Player } from "@/engine/models";
import { overallForPosition } from "@/engine/models";

/**
 * Greedy optimizer that assigns the best-fit player to each formation slot
 * based on position-specific overall ratings.
 */

/**
 * Returns 11 player IDs in formation-position order, maximizing
 * each slot's overallForPosition. Greedy: picks best available per slot.
 */
export function optimalLineup(
  squad: readonly Player[],
  formation: Formation,
): readonly number[] {
  const available = new Set(squad.map((p) => p.id));
  const playerMap = new Map(squad.map((p) => [p.id, p]));
  const result: number[] = new Array(formation.positions.length).fill(0);

  // Sort slots by scarcity: GK first (fewest candidates), then others
  const slotOrder = Array.from(
    { length: formation.positions.length },
    (_, i) => i,
  ).sort((a, b) => {
    const aIsGk = formation.positions[a] === Position.GK ? 0 : 1;
    const bIsGk = formation.positions[b] === Position.GK ? 0 : 1;
    if (aIsGk !== bIsGk) return aIsGk - bIsGk;
    return (
      countCandidates(squad, formation.positions[a]) -
      countCandidates(squad, formation.positions[b])
    );
  });

  for (const slotIdx of slotOrder) {
    const slotPos = formation.positions[slotIdx];
    let bestId = -1;
    let bestOvr = -Infinity;

    for (const pid of available) {
      const p = playerMap.get(pid)!;
      let ovr = overallForPosition(p.attributes, slotPos);

      // Prefer players whose primary/secondary matches the slot
      if (p.primaryPosition === slotPos) {
        ovr += 5;
      } else if (p.secondaryPosition === slotPos) {
        ovr += 2;
      }

      if (ovr > bestOvr) {
        bestOvr = ovr;
        bestId = pid;
      }
    }

    result[slotIdx] = bestId;
    available.delete(bestId);
  }

  return result;
}

/** Calculates the average position-specific overall of an optimal lineup. */
export function averageOverall(
  squad: readonly Player[],
  formation: Formation,
): number {
  const lineup = optimalLineup(squad, formation);
  const playerMap = new Map(squad.map((p) => [p.id, p]));

  let total = 0;
  for (let i = 0; i < lineup.length; i++) {
    const player = playerMap.get(lineup[i]);
    if (player != null) {
      total += overallForPosition(player.attributes, formation.positions[i]);
    }
  }

  return total / lineup.length;
}

function countCandidates(
  squad: readonly Player[],
  pos: Position,
): number {
  return squad.filter(
    (p) => p.primaryPosition === pos || p.secondaryPosition === pos,
  ).length;
}
