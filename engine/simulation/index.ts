// Simulation engine barrel file

// RNG
export type { IRng } from "./rng";
export { SeededRng } from "./rng";

// Success calculation
export {
  calculate as calculateSuccess,
  calculateRaw as calculateSuccessRaw,
  getPrimaryAttribute,
  getChemistryBonus,
  getMoraleBonus,
  getTraitBonus,
  getPositionPenalty,
  getThreshold,
} from "./success-calculator";

// Possession
export {
  resolve as resolvePossession,
  calculateMidfieldStrength,
} from "./possession-resolver";

// Action selection
export {
  selectAction,
  selectExecutor,
  advanceBallZone,
} from "./action-selector";

// Action resolution
export type { ActionResult } from "./action-resolver";
export {
  resolveAttack,
  resolveDefense,
} from "./action-resolver";

// Event generation
export { generate as generateEvents } from "./event-generator";

// Rating calculation
export {
  BASE_RATING,
  MIN_RATING,
  MAX_RATING,
  initializeRatings,
  applyEvents as applyRatingEvents,
  getMvpAndSvp,
  getPositionMultiplier,
} from "./rating-calculator";

// Tick processing
export {
  processTick,
  initializeStamina,
} from "./tick-processor";

// Match simulation
export {
  FIRST_HALF_TICKS,
  SECOND_HALF_TICKS,
  TOTAL_TICKS,
  simulate,
  simulateFirstHalf,
  simulateSecondHalf,
} from "./match-simulator";

// Live match session
export { LiveMatchSession } from "./live-match-session";

// Halftime processing
export {
  applyCard,
  applySubstitutions,
} from "./halftime-processor";

// Formation optimizer
export {
  optimalLineup,
  averageOverall,
} from "./formation-optimizer";

// Training processor
export {
  generateChoices as generateTrainingChoices,
  processTraining,
} from "./training-processor";
