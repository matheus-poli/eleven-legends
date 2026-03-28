import { TrainingType } from "@/engine/enums";
import type {
  Club,
  Player,
  Team,
  TrainingChoice,
  TrainingPlayerEvent,
  TrainingResult,
} from "@/engine/models";
import type { IRng } from "./rng";

/**
 * Processes training day choices and generates player events.
 * Inspired by Uma Musume / Inazuma Eleven training mechanics.
 */

const ALL_CHOICES: TrainingChoice[] = [
  {
    name: "Intense Drills",
    description:
      "Push the squad hard. Big gains but risk of morale loss.",
    type: TrainingType.IntenseDrills,
  },
  {
    name: "Tactical Session",
    description:
      "Work on team play. Builds chemistry between players.",
    type: TrainingType.TacticalSession,
  },
  {
    name: "Light Training",
    description: "Easy session. Small morale boost, no risks.",
    type: TrainingType.LightTraining,
  },
  {
    name: "Rest Day",
    description:
      "Let the squad recover. Stressed players recover morale.",
    type: TrainingType.RestDay,
  },
  {
    name: "Youth Showcase",
    description:
      "Give reserves the spotlight. Potential breakthroughs.",
    type: TrainingType.YouthFocus,
  },
];

/** Generates 3 random training choices for the manager to pick from. */
export function generateChoices(rng: IRng): readonly TrainingChoice[] {
  // Assign a random key to each choice, then sort by that key (mirrors C# OrderBy)
  const keyed = ALL_CHOICES.map((c) => ({ choice: c, key: rng.nextInt(0, 1000) }));
  keyed.sort((a, b) => a.key - b.key);
  return keyed.slice(0, 3).map((k) => k.choice);
}

/**
 * Processes the chosen training and returns events.
 * Returns a new club with updated player morale/chemistry.
 */
export function processTraining(
  choice: TrainingChoice,
  club: Club,
  rng: IRng,
): { result: TrainingResult; updatedClub: Club } {
  const events: TrainingPlayerEvent[] = [];
  const team = club.team;
  const starterSet = new Set(team.startingLineup);

  switch (choice.type) {
    case TrainingType.IntenseDrills:
      events.push(...processIntenseDrills(team, starterSet, rng));
      break;
    case TrainingType.TacticalSession:
      events.push(...processTacticalSession(team, starterSet, rng));
      break;
    case TrainingType.LightTraining:
      events.push(...processLightTraining(team, rng));
      break;
    case TrainingType.RestDay:
      events.push(...processRestDay(team, rng));
      break;
    case TrainingType.YouthFocus:
      events.push(...processYouthFocus(team, starterSet, rng));
      break;
  }

  // Apply morale/chemistry changes
  const updatedPlayers = team.players.map((p) => {
    const moraleDelta = events
      .filter((e) => e.playerId === p.id)
      .reduce((sum, e) => sum + e.moraleDelta, 0);
    const chemDelta = events
      .filter((e) => e.playerId === p.id)
      .reduce((sum, e) => sum + e.chemistryDelta, 0);

    if (moraleDelta === 0 && chemDelta === 0) return p;

    return {
      ...p,
      morale: Math.min(Math.max(p.morale + moraleDelta, 0), 100),
      chemistry: Math.min(Math.max(p.chemistry + chemDelta, 0), 100),
    };
  });

  const updatedClub: Club = {
    ...club,
    team: {
      ...team,
      players: updatedPlayers,
    },
  };

  return {
    result: { choice, events },
    updatedClub,
  };
}

function processIntenseDrills(
  team: Team,
  starters: Set<number>,
  rng: IRng,
): TrainingPlayerEvent[] {
  const events: TrainingPlayerEvent[] = [];

  for (const p of team.players) {
    const isStarter = starters.has(p.id);
    const roll = rng.nextInt(0, 100);

    if (isStarter) {
      if (roll < 60) {
        // 60% positive
        events.push({
          playerId: p.id,
          playerName: p.name,
          description: `${p.name} had an excellent training session!`,
          moraleDelta: rng.nextInt(3, 6),
          chemistryDelta: 0,
          isPositive: true,
        });
      } else if (roll < 85) {
        // 25% neutral
        events.push({
          playerId: p.id,
          playerName: p.name,
          description: `${p.name} trained solidly.`,
          moraleDelta: 1,
          chemistryDelta: 0,
          isPositive: true,
        });
      } else {
        // 15% negative -- overtraining
        events.push({
          playerId: p.id,
          playerName: p.name,
          description: `${p.name} is exhausted from intense training.`,
          moraleDelta: rng.nextInt(-5, -2),
          chemistryDelta: 0,
          isPositive: false,
        });
      }
    } else {
      // Reserves also train but with less drama
      if (roll < 40) {
        events.push({
          playerId: p.id,
          playerName: p.name,
          description: `${p.name} showed determination in training.`,
          moraleDelta: rng.nextInt(1, 3),
          chemistryDelta: 0,
          isPositive: true,
        });
      }
    }
  }

  // Random team event
  if (rng.nextInt(0, 100) < 30) {
    const random = team.players[rng.nextInt(0, team.players.length - 1)];
    events.push({
      playerId: random.id,
      playerName: random.name,
      description: `${random.name} had a breakthrough moment in finishing drills!`,
      moraleDelta: 5,
      chemistryDelta: 0,
      isPositive: true,
    });
  }

  return events;
}

function processTacticalSession(
  team: Team,
  starters: Set<number>,
  rng: IRng,
): TrainingPlayerEvent[] {
  const events: TrainingPlayerEvent[] = [];

  // Chemistry boost for starters
  for (const p of team.players.filter((pl) => starters.has(pl.id))) {
    const chemGain = rng.nextInt(2, 5);
    events.push({
      playerId: p.id,
      playerName: p.name,
      description: `${p.name} improved team understanding.`,
      moraleDelta: 1,
      chemistryDelta: chemGain,
      isPositive: true,
    });
  }

  // Random pair bonding event
  if (team.players.length >= 2 && rng.nextInt(0, 100) < 50) {
    const p1 = team.players[rng.nextInt(0, team.players.length - 1)];
    const p2 = team.players[rng.nextInt(0, team.players.length - 1)];
    if (p1.id !== p2.id) {
      events.push({
        playerId: p1.id,
        playerName: p1.name,
        description: `${p1.name} and ${p2.name} developed a great connection!`,
        chemistryDelta: 4,
        moraleDelta: 2,
        isPositive: true,
      });
      events.push({
        playerId: p2.id,
        playerName: p2.name,
        description: `${p2.name} built rapport with ${p1.name}.`,
        chemistryDelta: 4,
        moraleDelta: 2,
        isPositive: true,
      });
    }
  }

  return events;
}

function processLightTraining(
  team: Team,
  rng: IRng,
): TrainingPlayerEvent[] {
  const events: TrainingPlayerEvent[] = [];

  for (const p of team.players) {
    if (rng.nextInt(0, 100) < 70) {
      // 70% get small boost
      events.push({
        playerId: p.id,
        playerName: p.name,
        description: `${p.name} enjoyed a relaxed session.`,
        moraleDelta: rng.nextInt(1, 3),
        chemistryDelta: 0,
        isPositive: true,
      });
    }
  }

  return events;
}

function processRestDay(
  team: Team,
  rng: IRng,
): TrainingPlayerEvent[] {
  const events: TrainingPlayerEvent[] = [];

  for (const p of team.players) {
    if (p.morale < 50) {
      // Low morale players benefit most
      const recovery = rng.nextInt(5, 10);
      events.push({
        playerId: p.id,
        playerName: p.name,
        description: `${p.name} feels refreshed after rest.`,
        moraleDelta: recovery,
        chemistryDelta: 0,
        isPositive: true,
      });
    } else if (rng.nextInt(0, 100) < 40) {
      events.push({
        playerId: p.id,
        playerName: p.name,
        description: `${p.name} recharged mentally.`,
        moraleDelta: rng.nextInt(1, 3),
        chemistryDelta: 0,
        isPositive: true,
      });
    }
  }

  // Random off-day event
  if (rng.nextInt(0, 100) < 20) {
    const random = team.players[rng.nextInt(0, team.players.length - 1)];
    events.push({
      playerId: random.id,
      playerName: random.name,
      description: `The press praised ${random.name}'s recent form!`,
      moraleDelta: 3,
      chemistryDelta: 0,
      isPositive: true,
    });
  }

  return events;
}

function processYouthFocus(
  team: Team,
  starters: Set<number>,
  rng: IRng,
): TrainingPlayerEvent[] {
  const events: TrainingPlayerEvent[] = [];
  const reserves = team.players.filter((p) => !starters.has(p.id));

  for (const p of reserves) {
    const roll = rng.nextInt(0, 100);

    if (roll < 40) {
      // 40% breakthrough
      events.push({
        playerId: p.id,
        playerName: p.name,
        description: `${p.name} impressed everyone in training!`,
        moraleDelta: rng.nextInt(5, 8),
        chemistryDelta: 0,
        isPositive: true,
      });
    } else if (roll < 70) {
      events.push({
        playerId: p.id,
        playerName: p.name,
        description: `${p.name} worked hard to prove themselves.`,
        moraleDelta: rng.nextInt(2, 4),
        chemistryDelta: 0,
        isPositive: true,
      });
    } else if (roll < 85) {
      events.push({
        playerId: p.id,
        playerName: p.name,
        description: `${p.name} is frustrated about lack of game time.`,
        moraleDelta: rng.nextInt(-4, -1),
        chemistryDelta: 0,
        isPositive: false,
      });
    }
  }

  // Starters get a small morale hit (bored by easy session)
  for (const p of team.players.filter((pl) => starters.has(pl.id))) {
    if (rng.nextInt(0, 100) < 30) {
      events.push({
        playerId: p.id,
        playerName: p.name,
        description: `${p.name} found the youth session unchallenging.`,
        moraleDelta: -1,
        chemistryDelta: 0,
        isPositive: false,
      });
    }
  }

  return events;
}
