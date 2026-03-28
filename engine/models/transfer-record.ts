import { TransferType } from "@/engine/enums";

/** Represents a completed or pending transfer. */
export interface TransferRecord {
  type: TransferType;
  playerId: number;
  playerName: string;
  fromClubId: number | null;
  toClubId: number | null;
  fee: number;
  day: number;
}
