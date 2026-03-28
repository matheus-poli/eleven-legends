using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Calculates player ratings during and after a match.
/// Base rating: 6.0. Adjusted by events and position context.
/// </summary>
public static class RatingCalculator
{
    public const float BaseRating = 6.0f;
    public const float MinRating = 0.0f;
    public const float MaxRating = 10.0f;

    /// <summary>
    /// Initializes all starting players' ratings to the base value.
    /// </summary>
    public static void InitializeRatings(MatchState state, MatchConfig config)
    {
        var homeIds = state.HomeActivePlayerIds.Count > 0
            ? state.HomeActivePlayerIds
            : (IReadOnlyList<int>)config.HomeTeam.StartingLineup;
        var awayIds = state.AwayActivePlayerIds.Count > 0
            ? state.AwayActivePlayerIds
            : (IReadOnlyList<int>)config.AwayTeam.StartingLineup;

        foreach (int playerId in homeIds)
            state.PlayerRatings[playerId] = BaseRating;
        foreach (int playerId in awayIds)
            state.PlayerRatings[playerId] = BaseRating;
    }

    /// <summary>
    /// Applies a list of events to update player ratings, with position adjustments.
    /// </summary>
    public static void ApplyEvents(MatchState state, IEnumerable<MatchEvent> events, MatchConfig config)
    {
        foreach (MatchEvent evt in events)
        {
            if (!state.PlayerRatings.ContainsKey(evt.PlayerId))
                continue;

            Player? player = FindPlayer(evt.PlayerId, config);
            float impact = evt.RatingImpact;

            if (player != null)
                impact *= GetPositionMultiplier(player.PrimaryPosition, evt.Type);

            state.PlayerRatings[evt.PlayerId] = Math.Clamp(
                state.PlayerRatings[evt.PlayerId] + impact,
                MinRating,
                MaxRating);
        }
    }

    /// <summary>
    /// Returns the MVP (highest rated) and SVP (second highest) player IDs.
    /// </summary>
    public static (int MvpId, int SvpId) GetMvpAndSvp(MatchState state)
    {
        var sorted = state.PlayerRatings
            .OrderByDescending(kvp => kvp.Value)
            .Take(2)
            .ToList();

        int mvpId = sorted.Count > 0 ? sorted[0].Key : -1;
        int svpId = sorted.Count > 1 ? sorted[1].Key : -1;

        return (mvpId, svpId);
    }

    /// <summary>
    /// Position-based multiplier: defenders get more from tackles, attackers from goals, etc.
    /// </summary>
    public static float GetPositionMultiplier(Position position, EventType eventType)
    {
        return (position, eventType) switch
        {
            // Defenders value defensive actions more
            (Position.CB or Position.LB or Position.RB or Position.LWB or Position.RWB, EventType.Save) => 1.3f,
            (Position.CB or Position.LB or Position.RB or Position.LWB or Position.RWB, EventType.Goal) => 1.5f, // rare, so extra reward

            // Midfielders are balanced
            (Position.CDM, EventType.Save) => 1.2f,

            // Attackers value goals more
            (Position.ST or Position.CF or Position.LW or Position.RW, EventType.Goal) => 1.2f,
            (Position.ST or Position.CF or Position.LW or Position.RW, EventType.Assist) => 1.1f,

            // Goalkeepers value saves
            (Position.GK, EventType.Save) => 1.5f,

            _ => 1.0f
        };
    }

    private static Player? FindPlayer(int playerId, MatchConfig config)
    {
        return config.HomeTeam.Players.FirstOrDefault(p => p.Id == playerId)
            ?? config.AwayTeam.Players.FirstOrDefault(p => p.Id == playerId);
    }
}
