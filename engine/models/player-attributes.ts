import { Position } from "@/engine/enums";

/** All player attributes on a 0-100 scale. */
export interface PlayerAttributes {
  // Technical
  finishing: number;
  passing: number;
  dribbling: number;
  firstTouch: number;
  technique: number;

  // Mental
  decisions: number;
  composure: number;
  positioning: number;
  anticipation: number;
  offTheBall: number;

  // Physical
  speed: number;
  acceleration: number;
  stamina: number;
  strength: number;
  agility: number;

  // Special
  consistency: number;
  leadership: number;
  flair: number;
  bigMatches: number;

  // Goalkeeper-exclusive
  reflexes: number;
  handling: number;
  gkPositioning: number;
  aerial: number;
}

/** Returns the overall average of outfield attributes (excludes GK-specific). */
export function outfieldOverall(a: PlayerAttributes): number {
  return (
    (a.finishing +
      a.passing +
      a.dribbling +
      a.firstTouch +
      a.technique +
      a.decisions +
      a.composure +
      a.positioning +
      a.anticipation +
      a.offTheBall +
      a.speed +
      a.acceleration +
      a.stamina +
      a.strength +
      a.agility +
      a.consistency +
      a.leadership +
      a.flair +
      a.bigMatches) /
    19
  );
}

/** Returns the overall average of goalkeeper attributes. */
export function goalkeeperOverall(a: PlayerAttributes): number {
  return (
    (a.reflexes +
      a.handling +
      a.gkPositioning +
      a.aerial +
      a.composure +
      a.decisions +
      a.positioning +
      a.leadership +
      a.consistency) /
    9
  );
}

function weighted(pairs: [number, number][]): number {
  let sum = 0;
  let totalWeight = 0;
  for (const [attr, weight] of pairs) {
    sum += attr * weight;
    totalWeight += weight;
  }
  return sum / totalWeight;
}

/**
 * Returns a position-weighted overall rating. Each position emphasizes
 * different attributes so a CB rates differently than a ST.
 */
export function overallForPosition(
  a: PlayerAttributes,
  pos: Position,
): number {
  if (pos === Position.GK) {
    return goalkeeperOverall(a);
  }

  switch (pos) {
    case Position.CB:
      return weighted([
        [a.positioning, 2],
        [a.anticipation, 2],
        [a.strength, 2],
        [a.aerial, 2],
        [a.composure, 1.5],
        [a.leadership, 1.5],
        [a.speed, 1],
        [a.passing, 1],
        [a.decisions, 1],
        [a.stamina, 1],
      ]);

    case Position.LB:
    case Position.RB:
      return weighted([
        [a.speed, 2],
        [a.acceleration, 2],
        [a.positioning, 1.5],
        [a.anticipation, 1.5],
        [a.stamina, 1.5],
        [a.dribbling, 1],
        [a.passing, 1.5],
        [a.strength, 1],
        [a.composure, 1],
        [a.agility, 1],
      ]);

    case Position.LWB:
    case Position.RWB:
      return weighted([
        [a.speed, 2],
        [a.stamina, 2],
        [a.acceleration, 1.5],
        [a.dribbling, 1.5],
        [a.passing, 1.5],
        [a.positioning, 1],
        [a.anticipation, 1],
        [a.technique, 1],
        [a.strength, 1],
        [a.agility, 1.5],
      ]);

    case Position.CDM:
      return weighted([
        [a.positioning, 2],
        [a.anticipation, 2],
        [a.strength, 2],
        [a.decisions, 1.5],
        [a.passing, 1.5],
        [a.composure, 1.5],
        [a.stamina, 1],
        [a.leadership, 1],
        [a.speed, 1],
        [a.firstTouch, 1],
      ]);

    case Position.CM:
      return weighted([
        [a.passing, 2],
        [a.decisions, 2],
        [a.composure, 1.5],
        [a.firstTouch, 1.5],
        [a.stamina, 1.5],
        [a.positioning, 1],
        [a.technique, 1],
        [a.offTheBall, 1],
        [a.dribbling, 1],
        [a.anticipation, 1],
      ]);

    case Position.CAM:
      return weighted([
        [a.passing, 2],
        [a.technique, 2],
        [a.decisions, 1.5],
        [a.firstTouch, 1.5],
        [a.flair, 1.5],
        [a.composure, 1.5],
        [a.dribbling, 1],
        [a.offTheBall, 1],
        [a.finishing, 1],
        [a.acceleration, 1],
      ]);

    case Position.LM:
    case Position.RM:
      return weighted([
        [a.speed, 2],
        [a.dribbling, 2],
        [a.stamina, 1.5],
        [a.passing, 1.5],
        [a.acceleration, 1.5],
        [a.technique, 1],
        [a.firstTouch, 1],
        [a.agility, 1],
        [a.offTheBall, 1],
        [a.composure, 1],
      ]);

    case Position.LW:
    case Position.RW:
      return weighted([
        [a.speed, 2],
        [a.dribbling, 2],
        [a.acceleration, 2],
        [a.technique, 1.5],
        [a.flair, 1.5],
        [a.finishing, 1],
        [a.firstTouch, 1],
        [a.agility, 1],
        [a.offTheBall, 1],
        [a.composure, 1],
      ]);

    case Position.CF:
      return weighted([
        [a.finishing, 2],
        [a.composure, 2],
        [a.firstTouch, 1.5],
        [a.technique, 1.5],
        [a.offTheBall, 1.5],
        [a.decisions, 1],
        [a.dribbling, 1],
        [a.passing, 1],
        [a.flair, 1],
        [a.strength, 1],
      ]);

    case Position.ST:
      return weighted([
        [a.finishing, 2],
        [a.offTheBall, 2],
        [a.composure, 1.5],
        [a.speed, 1.5],
        [a.strength, 1.5],
        [a.firstTouch, 1],
        [a.dribbling, 1],
        [a.anticipation, 1],
        [a.agility, 1],
        [a.bigMatches, 1],
      ]);

    default:
      return outfieldOverall(a);
  }
}
