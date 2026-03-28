import { Position } from "@/engine/enums";
import type { Player, PlayerAttributes, ScoutRegion } from "@/engine/models";
import type { IRng } from "@/engine/simulation/rng";

/**
 * Regional scouting system. Pay to scout a region, reveals free agents.
 */

const REGIONS: ScoutRegion[] = [
  {
    name: "Brasil",
    cost: 5_000,
    firstNames: [
      "Lucas",
      "Gabriel",
      "Matheus",
      "Rafael",
      "Pedro",
      "João",
      "Bruno",
      "Vinícius",
    ],
    lastNames: [
      "Silva",
      "Santos",
      "Oliveira",
      "Souza",
      "Lima",
      "Costa",
      "Ferreira",
      "Almeida",
    ],
  },
  {
    name: "España",
    cost: 5_000,
    firstNames: [
      "Carlos",
      "Diego",
      "Alejandro",
      "Pablo",
      "Miguel",
      "Sergio",
      "Álvaro",
      "Javier",
    ],
    lastNames: [
      "García",
      "Rodríguez",
      "Martínez",
      "López",
      "Hernández",
      "Fernández",
      "Sánchez",
      "Pérez",
    ],
  },
  {
    name: "England",
    cost: 4_500,
    firstNames: [
      "James",
      "Oliver",
      "Harry",
      "Jack",
      "George",
      "Charlie",
      "Thomas",
      "William",
    ],
    lastNames: [
      "Smith",
      "Jones",
      "Williams",
      "Brown",
      "Taylor",
      "Johnson",
      "Wilson",
      "Davies",
    ],
  },
  {
    name: "Italia",
    cost: 4_500,
    firstNames: [
      "Marco",
      "Luca",
      "Alessandro",
      "Francesco",
      "Lorenzo",
      "Matteo",
      "Andrea",
      "Simone",
    ],
    lastNames: [
      "Rossi",
      "Russo",
      "Ferrari",
      "Esposito",
      "Bianchi",
      "Romano",
      "Colombo",
      "Ricci",
    ],
  },
  {
    name: "Africa",
    cost: 2_000,
    firstNames: [
      "Kwame",
      "Abdou",
      "Moussa",
      "Ibrahim",
      "Youssef",
      "Amara",
      "Kofi",
      "Sekou",
    ],
    lastNames: [
      "Diallo",
      "Touré",
      "Traoré",
      "Coulibaly",
      "Diop",
      "Camara",
      "Sylla",
      "Keita",
    ],
  },
  {
    name: "Asia",
    cost: 2_500,
    firstNames: [
      "Takumi",
      "Hiroshi",
      "Jin",
      "Wei",
      "Min-Jun",
      "Ryu",
      "Haruto",
      "Yuto",
    ],
    lastNames: [
      "Tanaka",
      "Kim",
      "Park",
      "Chen",
      "Lee",
      "Yamamoto",
      "Suzuki",
      "Watanabe",
    ],
  },
  {
    name: "Americas",
    cost: 2_500,
    firstNames: [
      "Santiago",
      "Matías",
      "Nicolás",
      "Sebastián",
      "Tomás",
      "Valentín",
      "Emiliano",
      "Thiago",
    ],
    lastNames: [
      "González",
      "Muñoz",
      "Rojas",
      "Díaz",
      "Reyes",
      "Morales",
      "Jiménez",
      "Vargas",
    ],
  },
];

/**
 * Returns all available scout regions.
 */
export function getRegions(): readonly ScoutRegion[] {
  return REGIONS;
}

/**
 * Scouts a region and reveals 3-5 players with optional sign fees.
 * Better players (OVR 55+) have signing fees.
 */
export function scout(
  rng: IRng,
  region: ScoutRegion,
  nextPlayerId: number,
): Array<{ player: Player; signFee: number }> {
  const count = rng.nextInt(3, 5);
  const results: Array<{ player: Player; signFee: number }> = [];

  for (let i = 0; i < count; i++) {
    const pos = pickRandomPosition(rng);
    const age = rng.nextInt(19, 33);
    const baseAttr = rng.nextInt(35, 70);

    const firstName =
      region.firstNames[rng.nextInt(0, region.firstNames.length - 1)];
    const lastName =
      region.lastNames[rng.nextInt(0, region.lastNames.length - 1)];
    const name = `${firstName[0]}. ${lastName}`;

    const attrs = generateScoutAttributes(rng, pos, baseAttr);

    const player: Player = {
      id: nextPlayerId + i,
      name,
      primaryPosition: pos,
      secondaryPosition: null,
      age,
      morale: rng.nextInt(40, 65),
      chemistry: rng.nextInt(20, 45),
      attributes: attrs,
      traits: [],
    };

    // Players with OVR 55+ have a signing fee
    const ovr =
      pos === Position.GK
        ? goalkeeperOvrFromAttrs(attrs)
        : outfieldOvrFromAttrs(attrs);
    const signFee =
      ovr >= 55 ? Math.round((ovr * 150) / 1000) * 1000 : 0;

    results.push({ player, signFee });
  }

  return results;
}

function pickRandomPosition(rng: IRng): Position {
  const positions: Position[] = [
    Position.GK,
    Position.CB,
    Position.CB,
    Position.LB,
    Position.RB,
    Position.CM,
    Position.CM,
    Position.CDM,
    Position.CAM,
    Position.LW,
    Position.RW,
    Position.ST,
  ];
  return positions[rng.nextInt(0, positions.length - 1)];
}

function generateScoutAttributes(
  rng: IRng,
  pos: Position,
  baseAttr: number,
): PlayerAttributes {
  const attr = (): number =>
    clamp(baseAttr + rng.nextInt(-10, 10), 15, 90);
  const high = (): number =>
    clamp(baseAttr + 8 + rng.nextInt(-5, 10), 20, 95);
  const low = (): number =>
    clamp(baseAttr - 8 + rng.nextInt(-10, 5), 10, 70);

  switch (pos) {
    case Position.GK:
      return {
        finishing: low(),
        passing: low(),
        dribbling: low(),
        firstTouch: attr(),
        technique: low(),
        decisions: attr(),
        composure: high(),
        positioning: attr(),
        anticipation: attr(),
        offTheBall: low(),
        speed: attr(),
        acceleration: attr(),
        stamina: attr(),
        strength: attr(),
        agility: attr(),
        consistency: attr(),
        leadership: attr(),
        flair: low(),
        bigMatches: attr(),
        reflexes: high(),
        handling: high(),
        gkPositioning: high(),
        aerial: high(),
      };
    case Position.CB:
    case Position.LB:
    case Position.RB:
      return {
        finishing: low(),
        passing: attr(),
        dribbling: low(),
        firstTouch: attr(),
        technique: attr(),
        decisions: high(),
        composure: high(),
        positioning: high(),
        anticipation: high(),
        offTheBall: attr(),
        speed: attr(),
        acceleration: attr(),
        stamina: high(),
        strength: high(),
        agility: attr(),
        consistency: attr(),
        leadership: attr(),
        flair: low(),
        bigMatches: attr(),
        reflexes: low(),
        handling: low(),
        gkPositioning: low(),
        aerial: high(),
      };
    case Position.ST:
    case Position.CF:
      return {
        finishing: high(),
        passing: attr(),
        dribbling: high(),
        firstTouch: high(),
        technique: high(),
        decisions: attr(),
        composure: high(),
        positioning: attr(),
        anticipation: attr(),
        offTheBall: high(),
        speed: high(),
        acceleration: high(),
        stamina: attr(),
        strength: attr(),
        agility: high(),
        consistency: attr(),
        leadership: low(),
        flair: high(),
        bigMatches: attr(),
        reflexes: low(),
        handling: low(),
        gkPositioning: low(),
        aerial: attr(),
      };
    case Position.LW:
    case Position.RW:
      return {
        finishing: attr(),
        passing: attr(),
        dribbling: high(),
        firstTouch: high(),
        technique: high(),
        decisions: attr(),
        composure: attr(),
        positioning: attr(),
        anticipation: attr(),
        offTheBall: high(),
        speed: high(),
        acceleration: high(),
        stamina: attr(),
        strength: low(),
        agility: high(),
        consistency: attr(),
        leadership: low(),
        flair: high(),
        bigMatches: attr(),
        reflexes: low(),
        handling: low(),
        gkPositioning: low(),
        aerial: low(),
      };
    default:
      // Midfield positions (CM, CDM, CAM, LM, RM)
      return {
        finishing: attr(),
        passing: high(),
        dribbling: attr(),
        firstTouch: high(),
        technique: high(),
        decisions: high(),
        composure: high(),
        positioning: attr(),
        anticipation: attr(),
        offTheBall: attr(),
        speed: attr(),
        acceleration: attr(),
        stamina: high(),
        strength: attr(),
        agility: attr(),
        consistency: attr(),
        leadership: attr(),
        flair: attr(),
        bigMatches: attr(),
        reflexes: low(),
        handling: low(),
        gkPositioning: low(),
        aerial: attr(),
      };
  }
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

/** Quick outfield OVR from raw attributes (matches the model function). */
function outfieldOvrFromAttrs(a: PlayerAttributes): number {
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

/** Quick GK OVR from raw attributes (matches the model function). */
function goalkeeperOvrFromAttrs(a: PlayerAttributes): number {
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
