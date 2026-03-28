// Main barrel file -- re-exports from all engine sub-modules

// Enums
export {
  ActionType,
  CardEffect,
  CompetitionPhase,
  DayType,
  EventType,
  FieldZone,
  ManagerStatus,
  MatchPhase,
  Position,
  TacticalStyle,
  TrainingType,
  TransferType,
} from "./enums";

// Models
export type {
  Player,
  PlayerAttributes,
  Club,
  Team,
  Formation,
  TacticalSetup,
  MatchState,
  MatchConfig,
  MatchResult,
  MatchEvent,
  MatchFixture,
  FixtureResult,
  Substitution,
  TransferRecord,
  LoanRecord,
  LockerRoomCard,
  ManagerState,
  ScoutRegion,
  TrainingChoice,
  TrainingResult,
  TrainingPlayerEvent,
} from "./models";

export {
  outfieldOverall,
  goalkeeperOverall,
  overallForPosition,
  createMatchState,
  winnerClubId,
  scoreHome,
  scoreAway,
  F442,
  F433,
  F352,
  F4231,
  F532,
  FORMATION_PRESETS,
  getFormationPositions,
} from "./models";

// Simulation
export type { IRng } from "./simulation";
export {
  SeededRng,
  simulate,
  simulateFirstHalf,
  simulateSecondHalf,
  FIRST_HALF_TICKS,
  SECOND_HALF_TICKS,
  TOTAL_TICKS,
  LiveMatchSession,
  processTick,
  initializeStamina,
  applyCard,
  applySubstitutions,
  optimalLineup,
  averageOverall,
  generateTrainingChoices,
  processTraining,
} from "./simulation";

// Competition
export type { SeasonDay } from "./competition";
export {
  buildTemplate,
  KnockoutBracket,
  MundialBracket,
  CompetitionManager,
} from "./competition";

// Economy
export {
  calculateMatchRevenue,
  calculateWeeklySalary,
  getPhasePrize,
  processMatchDay,
  processWeeklySalary,
  isBankrupt,
} from "./economy";

// Transfers
export {
  calculatePlayerValue,
  estimateWeeklySalary,
  MIN_SQUAD_SIZE,
  MAX_SQUAD_SIZE,
  getAvailablePlayers,
  getSellablePlayers,
  getLoanablePlayersIn,
  salaryReserve,
  executeBuy,
  executeSell,
  executeLoanIn,
  executeLoanOut,
  addFreeAgent,
  canRemovePlayer,
  processAITransferDay,
  getRegions,
  scout,
  generateProspects,
  getMaxPlayerId,
} from "./transfers";

// Generators
export { generateTeams } from "./generators";

// Locker Room Card Generator
export { generate as generateLockerRoomCards } from "./locker-room-card-generator";

// Career Manager
export {
  updateReputation,
  paySalary,
  checkDismissal,
  checkVictory,
  isGameOver,
  isVictory,
} from "./career-manager";

// Game State
export type { DayResult, MatchDayContext } from "./game-state";
export { GameState } from "./game-state";
