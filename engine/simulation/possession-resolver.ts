import { Position, TacticalStyle } from "@/engine/enums";
import type { MatchConfig, MatchState, Player, Team } from "@/engine/models";
import type { IRng } from "./rng";

/**
 * Resolves which team has possession at each tick, based on midfield strength and context.
 */

enum PositionZone {
  Defense = "Defense",
  Midfield = "Midfield",
  Attack = "Attack",
}

/**
 * Determines which team has possession for this tick.
 * Factors: midfield attribute average, current possession momentum, RNG.
 * Returns the team ID of the team with possession.
 */
export function resolve(
  state: MatchState,
  config: MatchConfig,
  rng: IRng,
): number {
  const homeStrength = calculateMidfieldStrength(config.homeTeam);
  const awayStrength = calculateMidfieldStrength(config.awayTeam);

  let total = homeStrength + awayStrength;
  if (total <= 0) total = 1;

  let homeChance = homeStrength / total;

  // Momentum: the team that already has the ball has a slight advantage
  if (state.ballPossessionTeamId === config.homeTeam.id) {
    homeChance += 0.05;
  } else if (state.ballPossessionTeamId === config.awayTeam.id) {
    homeChance -= 0.05;
  }

  // Tactical style modifier
  homeChance += getTacticalModifier(config.homeTactics?.style ?? null);
  homeChance -= getTacticalModifier(config.awayTactics?.style ?? null);

  homeChance = Math.min(Math.max(homeChance, 0.15), 0.85);

  const roll = rng.nextFloat(0, 1);
  return roll < homeChance ? config.homeTeam.id : config.awayTeam.id;
}

function getTacticalModifier(style: TacticalStyle | null): number {
  switch (style) {
    case TacticalStyle.Attacking:
      return 0.06;
    case TacticalStyle.Defensive:
      return -0.06;
    default:
      return 0;
  }
}

/**
 * Calculates midfield strength as the average of passing + decisions + composure
 * for starting midfielders, weighted by stamina.
 */
export function calculateMidfieldStrength(team: Team): number {
  const midfielders = getStartingPlayersInZone(team, PositionZone.Midfield);
  if (midfielders.length === 0) {
    return 30; // fallback for teams without midfielders
  }

  let total = 0;
  for (const player of midfielders) {
    total +=
      (player.attributes.passing +
        player.attributes.decisions +
        player.attributes.composure) /
      3;
  }
  return total / midfielders.length;
}

function getStartingPlayersInZone(team: Team, zone: PositionZone): Player[] {
  const startingSet = new Set(team.startingLineup);
  return team.players.filter(
    (p) => startingSet.has(p.id) && getZone(p.primaryPosition) === zone,
  );
}

function getZone(pos: Position): PositionZone {
  switch (pos) {
    case Position.GK:
    case Position.CB:
    case Position.LB:
    case Position.RB:
    case Position.LWB:
    case Position.RWB:
      return PositionZone.Defense;
    case Position.CDM:
    case Position.CM:
    case Position.CAM:
    case Position.LM:
    case Position.RM:
      return PositionZone.Midfield;
    case Position.LW:
    case Position.RW:
    case Position.CF:
    case Position.ST:
      return PositionZone.Attack;
    default:
      return PositionZone.Midfield;
  }
}
