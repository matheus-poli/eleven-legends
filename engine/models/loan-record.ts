/** Tracks an active loan (player temporarily at another club). */
export interface LoanRecord {
  playerId: number;
  playerName: string;
  originClubId: number;
  hostClubId: number;
}
