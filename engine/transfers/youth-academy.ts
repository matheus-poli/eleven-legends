import { Position } from "@/engine/enums";
import type { Club, Player, PlayerAttributes } from "@/engine/models";
import type { IRng } from "@/engine/simulation/rng";

/**
 * Generates youth prospects for the academy recruitment system (card-based gacha).
 */

const YOUTH_POSITIONS: Position[] = [
  Position.GK,
  Position.CB,
  Position.LB,
  Position.RB,
  Position.CM,
  Position.CDM,
  Position.CAM,
  Position.LW,
  Position.RW,
  Position.ST,
  Position.CF,
];

/**
 * Generates 3 youth prospect cards. Player picks 1.
 */
export function generateProspects(
  rng: IRng,
  country: string,
  nextPlayerId: number,
): Array<{ prospect: Player; fee: number }> {
  const names = getNamePool(country);
  const prospects: Array<{ prospect: Player; fee: number }> = [];

  for (let i = 0; i < 3; i++) {
    const age = rng.nextInt(16, 19);
    const pos = YOUTH_POSITIONS[rng.nextInt(0, YOUTH_POSITIONS.length - 1)];

    const baseAttr = rng.nextInt(30, 55);
    const variance = 12;

    const firstName = names.firstNames[rng.nextInt(0, names.firstNames.length - 1)];
    const lastName = names.lastNames[rng.nextInt(0, names.lastNames.length - 1)];
    const name = `${firstName[0]}. ${lastName}`;

    const attrs = generateYouthAttributes(rng, pos, baseAttr, variance);

    const prospect: Player = {
      id: nextPlayerId + i,
      name,
      primaryPosition: pos,
      secondaryPosition: null,
      age,
      morale: rng.nextInt(50, 70),
      chemistry: rng.nextInt(30, 50),
      attributes: attrs,
      traits: [],
    };

    const ovr =
      pos === Position.GK
        ? goalkeeperOvrFromAttrs(attrs)
        : outfieldOvrFromAttrs(attrs);
    let fee = Math.round((ovr * 200) / 1000) * 1000;
    fee = Math.max(5_000, Math.min(15_000, fee));

    prospects.push({ prospect, fee });
  }

  return prospects;
}

/**
 * Returns the maximum player ID across all clubs (for generating new IDs).
 */
export function getMaxPlayerId(clubs: readonly Club[]): number {
  let maxId = 0;
  for (const club of clubs) {
    for (const player of club.team.players) {
      if (player.id > maxId) maxId = player.id;
    }
  }
  return maxId;
}

function generateYouthAttributes(
  rng: IRng,
  pos: Position,
  baseAttr: number,
  variance: number,
): PlayerAttributes {
  const attr = (): number =>
    clamp(baseAttr + rng.nextInt(-variance, variance), 15, 70);
  const high = (): number =>
    clamp(
      baseAttr + 8 + rng.nextInt(-Math.floor(variance / 2), variance),
      20,
      75,
    );
  const low = (): number =>
    clamp(
      baseAttr - 5 + rng.nextInt(-variance, Math.floor(variance / 2)),
      10,
      55,
    );

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
        speed:
          pos === Position.LB || pos === Position.RB ? high() : attr(),
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

function getNamePool(country: string): {
  firstNames: readonly string[];
  lastNames: readonly string[];
} {
  switch (country) {
    case "Brasil":
      return {
        firstNames: [
          "Lucas",
          "Gabriel",
          "Matheus",
          "Rafael",
          "Pedro",
          "João",
          "Bruno",
          "Vinícius",
          "Kaio",
          "Enzo",
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
          "Pereira",
          "Nascimento",
        ],
      };
    case "España":
      return {
        firstNames: [
          "Carlos",
          "Diego",
          "Alejandro",
          "Pablo",
          "Miguel",
          "Sergio",
          "Álvaro",
          "Javier",
          "Hugo",
          "Adrián",
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
          "Ruiz",
          "Torres",
        ],
      };
    case "England":
      return {
        firstNames: [
          "James",
          "Oliver",
          "Harry",
          "Jack",
          "George",
          "Charlie",
          "Thomas",
          "William",
          "Daniel",
          "Samuel",
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
          "Robinson",
          "Thompson",
        ],
      };
    case "Italia":
      return {
        firstNames: [
          "Marco",
          "Luca",
          "Alessandro",
          "Francesco",
          "Lorenzo",
          "Matteo",
          "Andrea",
          "Simone",
          "Giuseppe",
          "Davide",
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
          "Marino",
          "Greco",
        ],
      };
    default:
      return {
        firstNames: [
          "Alex",
          "Max",
          "Leo",
          "Tom",
          "Sam",
          "Ben",
          "Dan",
          "Chris",
          "Nick",
          "Ryan",
        ],
        lastNames: [
          "Young",
          "Green",
          "White",
          "Black",
          "Grey",
          "Stone",
          "Wood",
          "Field",
          "Brook",
          "Hill",
        ],
      };
  }
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

/** Quick outfield OVR from raw attributes. */
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

/** Quick GK OVR from raw attributes. */
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
