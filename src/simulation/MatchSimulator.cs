using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Orchestrates a full 90-tick match simulation.
/// </summary>
public static class MatchSimulator
{
    public const int FirstHalfTicks = 45;
    public const int SecondHalfTicks = 45;
    public const int TotalTicks = FirstHalfTicks + SecondHalfTicks;

    /// <summary>
    /// Simulates a complete match and returns the result.
    /// </summary>
    public static MatchResult Simulate(MatchConfig config)
    {
        var rng = new SeededRng(config.Seed);
        var state = new MatchState
        {
            BallPossessionTeamId = config.HomeTeam.Id,
            BallZone = FieldZone.MidfieldCenter,
            Phase = MatchPhase.FirstHalf
        };

        // Initialize ratings and stamina
        RatingCalculator.InitializeRatings(state, config);
        TickProcessor.InitializeStamina(state, config);

        // First half: ticks 1–45
        for (int tick = 0; tick < FirstHalfTicks; tick++)
        {
            TickProcessor.ProcessTick(state, config, rng);
        }

        // Half time
        state.Phase = MatchPhase.HalfTime;
        // Future: locker room card selection happens here

        // Second half: ticks 46–90
        state.Phase = MatchPhase.SecondHalf;
        state.BallZone = FieldZone.MidfieldCenter;
        for (int tick = 0; tick < SecondHalfTicks; tick++)
        {
            TickProcessor.ProcessTick(state, config, rng);
        }

        // Final
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
