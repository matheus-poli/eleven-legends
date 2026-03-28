using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Orchestrates a full 90-tick match simulation.
/// Supports both full simulation and split (first half → halftime decisions → second half).
/// </summary>
public static class MatchSimulator
{
    public const int FirstHalfTicks = 45;
    public const int SecondHalfTicks = 45;
    public const int TotalTicks = FirstHalfTicks + SecondHalfTicks;

    /// <summary>
    /// Simulates a complete match and returns the result.
    /// If MatchConfig contains tactics/cards/subs, they are applied automatically at halftime.
    /// </summary>
    public static MatchResult Simulate(MatchConfig config)
    {
        var rng = new SeededRng(config.Seed);
        var state = InitializeState(config);

        // First half
        RunHalf(state, config, rng, FirstHalfTicks);

        // Halftime: apply cards and subs from config
        state.Phase = MatchPhase.HalfTime;
        ApplyConfigHalftimeEffects(state, config);

        // Second half
        state.Phase = MatchPhase.SecondHalf;
        state.BallZone = FieldZone.MidfieldCenter;
        RunHalf(state, config, rng, SecondHalfTicks);

        return FinalizeResult(state);
    }

    /// <summary>
    /// Simulates only the first half. Returns state and RNG for continuation.
    /// Use for interactive matches where the player makes halftime decisions.
    /// </summary>
    public static (MatchState State, SeededRng Rng) SimulateFirstHalf(MatchConfig config)
    {
        var rng = new SeededRng(config.Seed);
        var state = InitializeState(config);

        RunHalf(state, config, rng, FirstHalfTicks);
        state.Phase = MatchPhase.HalfTime;

        return (state, rng);
    }

    /// <summary>
    /// Simulates the second half after halftime decisions.
    /// Call HalftimeProcessor methods before this to apply cards/subs.
    /// </summary>
    public static MatchResult SimulateSecondHalf(MatchState state, MatchConfig config, SeededRng rng)
    {
        state.Phase = MatchPhase.SecondHalf;
        state.BallZone = FieldZone.MidfieldCenter;
        RunHalf(state, config, rng, SecondHalfTicks);

        return FinalizeResult(state);
    }

    private static MatchState InitializeState(MatchConfig config)
    {
        var state = new MatchState
        {
            BallPossessionTeamId = config.HomeTeam.Id,
            BallZone = FieldZone.MidfieldCenter,
            Phase = MatchPhase.FirstHalf,
            HomeActivePlayerIds = [.. config.HomeTactics?.StartingPlayerIds ?? config.HomeTeam.StartingLineup],
            AwayActivePlayerIds = [.. config.AwayTactics?.StartingPlayerIds ?? config.AwayTeam.StartingLineup]
        };

        RatingCalculator.InitializeRatings(state, config);
        TickProcessor.InitializeStamina(state, config);

        return state;
    }

    private static void RunHalf(MatchState state, MatchConfig config, IRng rng, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            TickProcessor.ProcessTick(state, config, rng);
        }
    }

    private static void ApplyConfigHalftimeEffects(MatchState state, MatchConfig config)
    {
        if (config.HomeCard != null)
            HalftimeProcessor.ApplyCard(state, config, config.HomeCard, isHomeTeam: true);
        if (config.AwayCard != null)
            HalftimeProcessor.ApplyCard(state, config, config.AwayCard, isHomeTeam: false);
        if (config.HomeSubstitutions is { Count: > 0 })
            HalftimeProcessor.ApplySubstitutions(state, config, config.HomeSubstitutions, isHomeTeam: true);
        if (config.AwaySubstitutions is { Count: > 0 })
            HalftimeProcessor.ApplySubstitutions(state, config, config.AwaySubstitutions, isHomeTeam: false);
    }

    private static MatchResult FinalizeResult(MatchState state)
    {
        state.Phase = MatchPhase.Finished;
        var (mvpId, svpId) = RatingCalculator.GetMvpAndSvp(state);

        return new MatchResult
        {
            FinalState = state,
            Events = state.Events.ToList(),
            MvpPlayerId = mvpId,
            SvpPlayerId = svpId
        };
    }
}
