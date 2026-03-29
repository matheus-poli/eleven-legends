import { create } from "zustand";
import { GameState } from "@/engine/game-state";
import { type Club } from "@/engine/models/club";
import { ManagerStatus } from "@/engine/enums/manager-status";

interface GameStore {
  /** Current game state — null until a new game is started */
  gameState: GameState | null;

  /** Start a new game with the selected club */
  startGame: (clubs: Club[], clubId: number, seed: number) => void;

  /** Get the current game state (throws if not started) */
  getState: () => GameState;

  /** Clear game (return to menu) */
  clearGame: () => void;
}

export const useGameStore = create<GameStore>((set, get) => ({
  gameState: null,

  startGame: (clubs, clubId, seed) => {
    const managerState = {
      name: "You",
      clubId,
      reputation: 50,
      personalBalance: 0,
      salary: 1000,
      status: ManagerStatus.Employed,
    };

    const gameState = new GameState(clubs, managerState, seed);
    set({ gameState });
  },

  getState: () => {
    const state = get().gameState;
    if (!state) throw new Error("Game not started");
    return state;
  },

  clearGame: () => set({ gameState: null }),
}));
