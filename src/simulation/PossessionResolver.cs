using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Resolves which team has possession at each tick, based on midfield strength and context.
/// </summary>
public static class PossessionResolver
{
    /// <summary>
    /// Determines which team has possession for this tick.
    /// Factors: midfield attribute average, current possession momentum, RNG.
    /// </summary>
    /// <returns>Team ID of the team with possession.</returns>
    public static int Resolve(MatchState state, MatchConfig config, IRng rng)
    {
        float homeStrength = CalculateMidfieldStrength(config.HomeTeam);
        float awayStrength = CalculateMidfieldStrength(config.AwayTeam);

        float total = homeStrength + awayStrength;
        if (total <= 0f) total = 1f;

        float homeChance = homeStrength / total;

        // Momentum: the team that already has the ball has a slight advantage
        if (state.BallPossessionTeamId == config.HomeTeam.Id)
            homeChance += 0.05f;
        else if (state.BallPossessionTeamId == config.AwayTeam.Id)
            homeChance -= 0.05f;

        homeChance = Math.Clamp(homeChance, 0.15f, 0.85f);

        float roll = rng.NextFloat(0f, 1f);
        return roll < homeChance ? config.HomeTeam.Id : config.AwayTeam.Id;
    }

    /// <summary>
    /// Calculates midfield strength as the average of passing + decisions + composure
    /// for starting midfielders, weighted by stamina.
    /// </summary>
    public static float CalculateMidfieldStrength(Team team)
    {
        var midfielders = GetStartingPlayersInZone(team, PositionZone.Midfield);
        if (midfielders.Count == 0)
            return 30f; // fallback for teams without midfielders

        float total = 0f;
        foreach (Player player in midfielders)
        {
            total += (player.Attributes.Passing + player.Attributes.Decisions + player.Attributes.Composure) / 3f;
        }
        return total / midfielders.Count;
    }

    private enum PositionZone { Defense, Midfield, Attack }

    private static List<Player> GetStartingPlayersInZone(Team team, PositionZone zone)
    {
        var startingSet = new HashSet<int>(team.StartingLineup);
        return team.Players
            .Where(p => startingSet.Contains(p.Id) && GetZone(p.PrimaryPosition) == zone)
            .ToList();
    }

    private static PositionZone GetZone(Position pos)
    {
        return pos switch
        {
            Position.GK or Position.CB or Position.LB or Position.RB
                or Position.LWB or Position.RWB => PositionZone.Defense,
            Position.CDM or Position.CM or Position.CAM
                or Position.LM or Position.RM => PositionZone.Midfield,
            Position.LW or Position.RW or Position.CF or Position.ST => PositionZone.Attack,
            _ => PositionZone.Midfield
        };
    }
}
