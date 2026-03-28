import { Position } from "@/engine/enums";
import type { Club, Player, PlayerAttributes } from "@/engine/models";
import type { IRng } from "@/engine/simulation/rng";
import { SeededRng } from "@/engine/simulation/rng";

/**
 * Generates 32 fictional clubs (4 countries x 8 teams x 18 players each).
 * All generation is seeded for deterministic, reproducible results.
 */

const COUNTRIES = ["Brasil", "España", "England", "Italia"];

const TEAM_NAMES: string[][] = [
  // Brasil
  [
    "Flamingos FC",
    "Palmares SC",
    "São Marcos EC",
    "Coríntios SC",
    "Cruzado EC",
    "Botafoguense FC",
    "Atleticano MG",
    "Gremista RS",
  ],
  // España
  [
    "Real Madriz CF",
    "FC Barcino",
    "Atlético Madriz",
    "Sevícia FC",
    "Valência CF",
    "Real Bétis CF",
    "Villarejo CF",
    "Athletic Bilbão",
  ],
  // England
  [
    "Redpool FC",
    "Mancastle United",
    "Chelsington FC",
    "Gunners FC",
    "Totterham FC",
    "Mancastle City",
    "Leicestershire FC",
    "Evertown FC",
  ],
  // Italia
  [
    "Juventa FC",
    "AC Milanello",
    "Inter Milanello",
    "AS Romagna",
    "SS Lazzio",
    "SSC Napolitano",
    "Fiorença FC",
    "Atalância FC",
  ],
];

const FIRST_NAMES: string[][] = [
  // Brasil
  [
    "Lucas",
    "Gabriel",
    "Matheus",
    "Pedro",
    "Rafael",
    "André",
    "Bruno",
    "Carlos",
    "Diego",
    "Eduardo",
    "Felipe",
    "Gustavo",
    "Hugo",
    "Igor",
    "João",
    "Kaio",
    "Leonardo",
    "Marcos",
    "Neto",
    "Oscar",
  ],
  // España
  [
    "Alejandro",
    "Carlos",
    "Diego",
    "Fernando",
    "Gonzalo",
    "Héctor",
    "Iván",
    "Javier",
    "Luis",
    "Miguel",
    "Pablo",
    "Raúl",
    "Sergio",
    "Tomás",
    "Álvaro",
    "Andrés",
    "Borja",
    "César",
    "Dani",
    "Enrique",
  ],
  // England
  [
    "James",
    "Thomas",
    "Oliver",
    "Harry",
    "Jack",
    "Charlie",
    "George",
    "William",
    "Henry",
    "Alexander",
    "Daniel",
    "Luke",
    "Ryan",
    "Marcus",
    "Jordan",
    "Kyle",
    "Aaron",
    "Ben",
    "Chris",
    "David",
  ],
  // Italia
  [
    "Alessandro",
    "Marco",
    "Lorenzo",
    "Francesco",
    "Andrea",
    "Gianluca",
    "Paolo",
    "Roberto",
    "Stefano",
    "Giuseppe",
    "Luca",
    "Matteo",
    "Nicola",
    "Fabio",
    "Davide",
    "Giovanni",
    "Simone",
    "Antonio",
    "Daniele",
    "Emanuele",
  ],
];

const LAST_NAMES: string[][] = [
  // Brasil
  [
    "Silva",
    "Santos",
    "Oliveira",
    "Souza",
    "Pereira",
    "Costa",
    "Rodrigues",
    "Ferreira",
    "Almeida",
    "Nascimento",
    "Lima",
    "Araújo",
    "Ribeiro",
    "Carvalho",
    "Gomes",
    "Martins",
    "Rocha",
    "Moura",
    "Barbosa",
    "Cavalcanti",
  ],
  // España
  [
    "García",
    "Rodríguez",
    "Martínez",
    "López",
    "González",
    "Hernández",
    "Pérez",
    "Sánchez",
    "Ramírez",
    "Torres",
    "Flores",
    "Rivera",
    "Gómez",
    "Díaz",
    "Ruiz",
    "Moreno",
    "Jiménez",
    "Álvarez",
    "Romero",
    "Navarro",
  ],
  // England
  [
    "Smith",
    "Johnson",
    "Williams",
    "Brown",
    "Jones",
    "Miller",
    "Davis",
    "Wilson",
    "Moore",
    "Taylor",
    "Anderson",
    "Jackson",
    "White",
    "Harris",
    "Martin",
    "Thompson",
    "Clark",
    "Walker",
    "Hall",
    "Young",
  ],
  // Italia
  [
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
    "Bruno",
    "Gallo",
    "Conti",
    "De Luca",
    "Costa",
    "Mancini",
    "Barbieri",
    "Fontana",
    "Santoro",
    "Mariani",
  ],
];

/** Starting 11: 1 GK + 4 DEF + 4 MID + 2 ST. */
const STARTING_FORMATION: Position[] = [
  Position.GK,
  Position.LB,
  Position.CB,
  Position.CB,
  Position.RB,
  Position.LM,
  Position.CM,
  Position.CM,
  Position.RM,
  Position.ST,
  Position.ST,
];

/** 7 reserves covering all position groups. */
const RESERVE_POSITIONS: Position[] = [
  Position.GK,
  Position.CB,
  Position.LB,
  Position.CM,
  Position.CAM,
  Position.RW,
  Position.CF,
];

/**
 * Generates all 32 clubs with deterministic seeded RNG.
 */
export function generate(seed: number): Club[] {
  const rng = new SeededRng(seed);
  const clubs: Club[] = [];
  let clubId = 1;
  let playerId = 1;

  for (
    let countryIdx = 0;
    countryIdx < COUNTRIES.length;
    countryIdx++
  ) {
    for (
      let teamIdx = 0;
      teamIdx < TEAM_NAMES[countryIdx].length;
      teamIdx++
    ) {
      const teamName = TEAM_NAMES[countryIdx][teamIdx];
      const teamRank = teamIdx + 1; // 1 = strongest, 8 = weakest

      const players: Player[] = [];
      const startingIds: number[] = [];

      for (const pos of STARTING_FORMATION) {
        players.push(
          generatePlayer(playerId, countryIdx, pos, teamRank, rng),
        );
        startingIds.push(playerId);
        playerId++;
      }

      for (const pos of RESERVE_POSITIONS) {
        const starter = generatePlayer(
          playerId,
          countryIdx,
          pos,
          teamRank,
          rng,
        );
        players.push({
          ...starter,
          attributes: reduceAttributes(starter.attributes, 5),
        });
        playerId++;
      }

      const initialBalance =
        (9 - teamRank) * 50_000 + rng.nextInt(0, 50_000);
      const reputation = clamp(
        90 - (teamRank - 1) * 8 + rng.nextInt(-5, 5),
        20,
        100,
      );

      clubs.push({
        id: clubId,
        name: teamName,
        country: COUNTRIES[countryIdx],
        team: {
          id: clubId,
          name: teamName,
          players,
          startingLineup: startingIds,
        },
        balance: initialBalance,
        reputation,
      });

      clubId++;
    }
  }

  return clubs;
}

function generatePlayer(
  id: number,
  countryIdx: number,
  position: Position,
  teamRank: number,
  rng: IRng,
): Player {
  const firstName = pick(FIRST_NAMES[countryIdx], rng);
  const lastName = pick(LAST_NAMES[countryIdx], rng);
  const name = `${firstName[0]}. ${lastName}`;

  const [attrMin, attrMax] = getQualityRange(teamRank);
  const attrs = generateAttributes(position, attrMin, attrMax, rng);

  return {
    id,
    name,
    primaryPosition: position,
    secondaryPosition: null,
    attributes: attrs,
    traits: [],
    age: rng.nextInt(18, 35),
    morale: rng.nextInt(40, 70),
    chemistry: rng.nextInt(40, 70),
  };
}

function getQualityRange(teamRank: number): [number, number] {
  switch (teamRank) {
    case 1:
    case 2:
      return [65, 85];
    case 3:
    case 4:
      return [55, 75];
    case 5:
    case 6:
      return [45, 65];
    default:
      return [35, 55];
  }
}

function generateAttributes(
  position: Position,
  min: number,
  max: number,
  rng: IRng,
): PlayerAttributes {
  const attr = (): number => rng.nextInt(min, max);
  const high = (): number =>
    rng.nextInt(Math.min(min + 10, max), Math.min(max + 10, 100));
  const low = (): number =>
    rng.nextInt(Math.max(min - 15, 0), Math.max(max - 15, min));

  if (position === Position.GK) {
    return {
      finishing: low(),
      passing: low(),
      dribbling: low(),
      firstTouch: low(),
      technique: low(),
      decisions: attr(),
      composure: high(),
      positioning: high(),
      anticipation: attr(),
      offTheBall: low(),
      speed: low(),
      acceleration: low(),
      stamina: attr(),
      strength: attr(),
      agility: high(),
      consistency: attr(),
      leadership: attr(),
      flair: low(),
      bigMatches: attr(),
      reflexes: high(),
      handling: high(),
      gkPositioning: high(),
      aerial: high(),
    };
  }

  const isDef =
    position === Position.CB ||
    position === Position.LB ||
    position === Position.RB ||
    position === Position.LWB ||
    position === Position.RWB;
  const isMid =
    position === Position.CDM ||
    position === Position.CM ||
    position === Position.CAM ||
    position === Position.LM ||
    position === Position.RM;
  const isAtt =
    position === Position.LW ||
    position === Position.RW ||
    position === Position.CF ||
    position === Position.ST;
  const isWide =
    position === Position.LB ||
    position === Position.RB ||
    position === Position.LWB ||
    position === Position.RWB ||
    position === Position.LM ||
    position === Position.RM ||
    position === Position.LW ||
    position === Position.RW;

  return {
    finishing: isAtt ? high() : isDef ? low() : attr(),
    passing: isMid ? high() : attr(),
    dribbling: isAtt || isWide ? high() : isDef ? low() : attr(),
    firstTouch: isAtt || isMid ? high() : attr(),
    technique: isAtt ? high() : attr(),
    decisions: isMid ? high() : attr(),
    composure: isAtt || isMid ? high() : attr(),
    positioning: isDef ? high() : attr(),
    anticipation: isDef ? high() : attr(),
    offTheBall: isAtt ? high() : isDef ? low() : attr(),
    speed: isWide ? high() : attr(),
    acceleration: isWide ? high() : attr(),
    stamina: attr(),
    strength: isDef ? high() : attr(),
    agility: isWide || isAtt ? high() : attr(),
    consistency: attr(),
    leadership: attr(),
    flair: isAtt ? high() : attr(),
    bigMatches: attr(),
    reflexes: low(),
    handling: low(),
    gkPositioning: low(),
    aerial: isDef ? high() : low(),
  };
}

function reduceAttributes(a: PlayerAttributes, r: number): PlayerAttributes {
  const reduce = (val: number): number => Math.max(val - r, 1);
  return {
    finishing: reduce(a.finishing),
    passing: reduce(a.passing),
    dribbling: reduce(a.dribbling),
    firstTouch: reduce(a.firstTouch),
    technique: reduce(a.technique),
    decisions: reduce(a.decisions),
    composure: reduce(a.composure),
    positioning: reduce(a.positioning),
    anticipation: reduce(a.anticipation),
    offTheBall: reduce(a.offTheBall),
    speed: reduce(a.speed),
    acceleration: reduce(a.acceleration),
    stamina: reduce(a.stamina),
    strength: reduce(a.strength),
    agility: reduce(a.agility),
    consistency: reduce(a.consistency),
    leadership: reduce(a.leadership),
    flair: reduce(a.flair),
    bigMatches: reduce(a.bigMatches),
    reflexes: reduce(a.reflexes),
    handling: reduce(a.handling),
    gkPositioning: reduce(a.gkPositioning),
    aerial: reduce(a.aerial),
  };
}

function pick(array: string[], rng: IRng): string {
  return array[rng.nextInt(0, array.length - 1)];
}

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}
