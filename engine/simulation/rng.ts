import seedrandom from "seedrandom";

/**
 * Abstraction for random number generation. Always inject -- never use global RNG.
 * Implementations must be seeded for deterministic/reproducible results.
 */
export interface IRng {
  /** Returns a random integer in [minInclusive, maxInclusive]. */
  nextInt(min: number, max: number): number;

  /** Returns a random float in [min, max]. */
  nextFloat(min: number, max: number): number;
}

/**
 * Seeded RNG implementation using seedrandom for deterministic results.
 */
export class SeededRng implements IRng {
  private rng: seedrandom.PRNG;

  constructor(seed: number) {
    this.rng = seedrandom(seed.toString());
  }

  nextInt(min: number, max: number): number {
    return Math.floor(this.rng() * (max - min + 1)) + min;
  }

  nextFloat(min: number, max: number): number {
    return this.rng() * (max - min) + min;
  }
}
