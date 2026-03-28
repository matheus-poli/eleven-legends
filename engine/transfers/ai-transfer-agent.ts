import { Position, TransferType } from "@/engine/enums";
import type { Club, TransferRecord } from "@/engine/models";
import { goalkeeperOverall, outfieldOverall } from "@/engine/models";
import type { IRng } from "@/engine/simulation/rng";
import {
  getAvailablePlayers,
  getSellablePlayers,
  executeBuy,
  executeSell,
  MAX_SQUAD_SIZE,
} from "./transfer-market";

/**
 * Simple AI logic for automated club transfer decisions during the window.
 */

/**
 * Processes one transfer window day for all AI clubs.
 * Each AI club may buy or sell one player per day.
 */
export function processDay(
  clubs: readonly Club[],
  playerClubId: number,
  rng: IRng,
): TransferRecord[] {
  const records: TransferRecord[] = [];

  for (const club of clubs) {
    if (club.id === playerClubId) continue;

    // Try to buy if squad is small
    if (club.team.players.length < 16 && club.balance > 20_000) {
      const available = getAvailablePlayers(clubs, club.id);
      if (available.length > 0) {
        // Pick a random affordable player
        const affordable = available.filter(
          (a) => a.price <= club.balance * 0.3,
        );

        if (affordable.length > 0) {
          const pick = affordable[rng.nextInt(0, affordable.length - 1)];
          if (executeBuy(club, pick.club, pick.player, pick.price)) {
            records.push({
              type: TransferType.Buy,
              playerId: pick.player.id,
              playerName: pick.player.name,
              fromClubId: pick.club.id,
              toClubId: club.id,
              fee: pick.price,
              day: 0,
            });
          }
        }
      }
    }

    // Try to sell if squad is large and player is below average
    if (club.team.players.length > 20) {
      const sellable = getSellablePlayers(club);
      if (sellable.length > 0) {
        let totalOverall = 0;
        for (const p of club.team.players) {
          totalOverall +=
            p.primaryPosition === Position.GK
              ? goalkeeperOverall(p.attributes)
              : outfieldOverall(p.attributes);
        }
        const avgOverall = totalOverall / club.team.players.length;

        const weakest = sellable
          .filter((s) => {
            const ovr =
              s.player.primaryPosition === Position.GK
                ? goalkeeperOverall(s.player.attributes)
                : outfieldOverall(s.player.attributes);
            return ovr < avgOverall;
          })
          .sort((a, b) => a.price - b.price)[0];

        if (weakest != null) {
          // Find a buyer (any AI club with budget and space)
          const buyer = clubs.find(
            (c) =>
              c.id !== club.id &&
              c.id !== playerClubId &&
              c.team.players.length < MAX_SQUAD_SIZE &&
              c.balance >= weakest.price,
          );

          if (buyer != null) {
            if (
              executeSell(club, buyer, weakest.player, weakest.price)
            ) {
              records.push({
                type: TransferType.Sell,
                playerId: weakest.player.id,
                playerName: weakest.player.name,
                fromClubId: club.id,
                toClubId: buyer.id,
                fee: weakest.price,
                day: 0,
              });
            }
          }
        }
      }
    }
  }

  return records;
}
