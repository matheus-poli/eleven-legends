import { Position } from "@/engine/enums";
import type { Club, LoanRecord, Player, Team } from "@/engine/models";
import { calculateWeeklySalary } from "@/engine/economy/economy-processor";
import { calculate as calculateValuation } from "./player-valuation";

/**
 * Handles transfer operations: buy, sell, loan in, loan out.
 * Validates squad size rules (min 14, max 22, at least 1 GK).
 */

export const MIN_SQUAD_SIZE = 14;
export const MAX_SQUAD_SIZE = 22;

/**
 * Returns ALL players from other clubs with transfer prices.
 * Starters have a release clause multiplier (3-5x).
 * Clubs at minimum squad can only sell non-essential players.
 */
export function getAvailablePlayers(
  clubs: readonly Club[],
  excludeClubId: number,
): Array<{ player: Player; club: Club; price: number }> {
  const available: Array<{ player: Player; club: Club; price: number }> = [];

  for (const club of clubs) {
    if (club.id === excludeClubId) continue;

    const starterSet = new Set(club.team.startingLineup);
    for (const player of club.team.players) {
      const basePrice = calculateValuation(player, club.reputation);

      // Starters have a massive release clause
      const isStarter = starterSet.has(player.id);
      const price = isStarter ? basePrice * 4 : basePrice;

      // Can't buy if it would leave club below minimum
      if (!canRemovePlayer(club, player)) continue;

      available.push({ player, club, price });
    }
  }

  return available;
}

/**
 * Returns players from a club that can be sold.
 */
export function getSellablePlayers(
  club: Club,
): Array<{ player: Player; price: number }> {
  if (club.team.players.length <= MIN_SQUAD_SIZE) return [];

  const results: Array<{ player: Player; price: number }> = [];
  for (const player of club.team.players) {
    if (!canRemovePlayer(club, player)) continue;
    const price = calculateValuation(player, club.reputation);
    results.push({ player, price });
  }
  return results;
}

/**
 * Returns players from other clubs available for loan.
 */
export function getLoanablePlayersIn(
  clubs: readonly Club[],
  excludeClubId: number,
): Array<{ player: Player; club: Club; fee: number }> {
  const available: Array<{ player: Player; club: Club; fee: number }> = [];

  for (const club of clubs) {
    if (club.id === excludeClubId) continue;
    if (club.team.players.length <= MIN_SQUAD_SIZE) continue;

    const starterSet = new Set(club.team.startingLineup);
    for (const player of club.team.players) {
      if (starterSet.has(player.id)) continue;
      const rawFee =
        Math.round(
          (calculateValuation(player, club.reputation) * 0.3) / 1000,
        ) * 1000;
      const fee = Math.max(2_000, rawFee);
      available.push({ player, club, fee });
    }
  }

  return available;
}

/**
 * Minimum balance to keep after a purchase (reserve for 1 week of salary).
 */
export function salaryReserve(club: Club): number {
  return calculateWeeklySalary(club);
}

/**
 * Executes a player purchase. Returns true if successful.
 */
export function executeBuy(
  buyer: Club,
  seller: Club,
  player: Player,
  price: number,
): boolean {
  if (buyer.team.players.length >= MAX_SQUAD_SIZE) return false;
  const reserve = salaryReserve(buyer);
  if (buyer.balance - price < reserve) return false;
  if (!seller.team.players.some((p) => p.id === player.id)) return false;
  if (!canRemovePlayer(seller, player)) return false;

  buyer.balance -= price;
  seller.balance += price;

  removePlayerFromClub(seller, player);
  addPlayerToClub(buyer, player);

  return true;
}

/**
 * Executes a player sale. Returns true if successful.
 */
export function executeSell(
  seller: Club,
  buyer: Club,
  player: Player,
  price: number,
): boolean {
  return executeBuy(buyer, seller, player, price);
}

/**
 * Executes a loan-in: player moves temporarily to the host club.
 * Returns the LoanRecord if successful.
 */
export function executeLoanIn(
  host: Club,
  source: Club,
  player: Player,
  fee: number,
): LoanRecord | null {
  if (host.team.players.length >= MAX_SQUAD_SIZE) return null;
  if (host.balance < fee) return null;
  if (!source.team.players.some((p) => p.id === player.id)) return null;
  if (!canRemovePlayer(source, player)) return null;

  host.balance -= fee;
  source.balance += fee;

  removePlayerFromClub(source, player);
  addPlayerToClub(host, player);

  return {
    playerId: player.id,
    playerName: player.name,
    originClubId: source.id,
    hostClubId: host.id,
  };
}

/**
 * Executes a loan-out: player is sent to another club.
 * Returns the LoanRecord if successful.
 */
export function executeLoanOut(
  source: Club,
  host: Club,
  player: Player,
): LoanRecord | null {
  if (host.team.players.length >= MAX_SQUAD_SIZE) return null;
  if (!source.team.players.some((p) => p.id === player.id)) return null;
  if (!canRemovePlayer(source, player)) return null;

  removePlayerFromClub(source, player);
  addPlayerToClub(host, player);

  return {
    playerId: player.id,
    playerName: player.name,
    originClubId: source.id,
    hostClubId: host.id,
  };
}

/**
 * Adds a free agent / youth recruit to a club.
 */
export function addFreeAgent(
  club: Club,
  player: Player,
  fee: number = 0,
): boolean {
  if (club.team.players.length >= MAX_SQUAD_SIZE) return false;
  const reserve = fee > 0 ? salaryReserve(club) : 0;
  if (fee > 0 && club.balance - fee < reserve) return false;

  if (fee > 0) {
    club.balance -= fee;
  }

  addPlayerToClub(club, player);
  return true;
}

/**
 * Checks if a player can be removed from a club without violating rules.
 */
export function canRemovePlayer(club: Club, player: Player): boolean {
  if (club.team.players.length <= MIN_SQUAD_SIZE) return false;

  if (player.primaryPosition === Position.GK) {
    const gkCount = club.team.players.filter(
      (p) => p.primaryPosition === Position.GK,
    ).length;
    if (gkCount <= 1) return false;
  }

  return true;
}

function removePlayerFromClub(club: Club, player: Player): void {
  const newPlayers = club.team.players.filter((p) => p.id !== player.id);
  const newLineup = club.team.startingLineup.filter((id) => id !== player.id);
  const mutableLineup = [...newLineup];

  // If we removed a starter, fill from reserves
  if (
    club.team.startingLineup.includes(player.id) &&
    newPlayers.length >= 11
  ) {
    const lineupSet = new Set(mutableLineup);
    const reserve = newPlayers.find((p) => !lineupSet.has(p.id));
    if (reserve != null) {
      mutableLineup.push(reserve.id);
    }
  }

  club.team = {
    ...club.team,
    players: newPlayers,
    startingLineup: mutableLineup,
  };
}

function addPlayerToClub(club: Club, player: Player): void {
  const newPlayers = [...club.team.players, player];
  const newLineup = [...club.team.startingLineup];

  if (newLineup.length < 11) {
    newLineup.push(player.id);
  }

  club.team = {
    ...club.team,
    players: newPlayers,
    startingLineup: newLineup,
  };
}
