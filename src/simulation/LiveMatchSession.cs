using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Incremental match session that processes one tick at a time.
/// Wraps the same TickProcessor logic as MatchSimulator but exposes
/// each tick individually for real-time UI playback.
/// </summary>
public sealed class LiveMatchSession
{
    private readonly MatchConfig _config;
    private readonly SeededRng _rng;

    public MatchState State { get; }
    public MatchConfig Config => _config;

    /// <summary>Current tick within the half (0-44).</summary>
    public int HalfTick { get; private set; }

    public bool IsHalfTimeReached => State.Phase == MatchPhase.HalfTime;
    public bool IsSecondHalf => State.Phase == MatchPhase.SecondHalf;
    public bool IsMatchFinished => State.Phase == MatchPhase.Finished;

    public LiveMatchSession(MatchConfig config)
    {
        _config = config;
        _rng = new SeededRng(config.Seed);

        State = InitializeState(config);
    }

    /// <summary>
    /// Processes a single tick and returns events generated this tick.
    /// Returns empty list if match is finished or at halftime.
    /// </summary>
    public IReadOnlyList<MatchEvent> ProcessNextTick()
    {
        if (IsMatchFinished || IsHalfTimeReached)
            return [];

        int eventsBefore = State.Events.Count;
        TickProcessor.ProcessTick(State, _config, _rng);
        HalfTick++;

        // Collect only the events from this tick
        List<MatchEvent> newEvents = State.Events.GetRange(eventsBefore, State.Events.Count - eventsBefore);

        // Check if half is over
        if (HalfTick >= MatchSimulator.FirstHalfTicks && State.Phase == MatchPhase.FirstHalf)
        {
            State.Phase = MatchPhase.HalfTime;
        }
        else if (HalfTick >= MatchSimulator.SecondHalfTicks && State.Phase == MatchPhase.SecondHalf)
        {
            State.Phase = MatchPhase.Finished;
        }

        return newEvents;
    }

    /// <summary>
    /// Starts the second half. Call after applying halftime effects.
    /// </summary>
    public void StartSecondHalf()
    {
        State.Phase = MatchPhase.SecondHalf;
        State.BallZone = FieldZone.MidfieldCenter;
        HalfTick = 0;
    }

    /// <summary>
    /// Applies a live substitution. Returns true if successful.
    /// Max 3 per team per half.
    /// </summary>
    public bool ApplySubstitution(Substitution sub, bool isHomeTeam)
    {
        int used = isHomeTeam ? State.HomeSubstitutionsUsed : State.AwaySubstitutionsUsed;
        if (used >= 3) return false;

        HalftimeProcessor.ApplySubstitutions(State, _config,
            [sub], isHomeTeam);
        return true;
    }

    /// <summary>
    /// Finalizes the match and returns the result.
    /// Call only when IsMatchFinished is true.
    /// </summary>
    public MatchResult FinalizeResult()
    {
        State.Phase = MatchPhase.Finished;
        var (mvpId, svpId) = RatingCalculator.GetMvpAndSvp(State);

        return new MatchResult
        {
            FinalState = State,
            Events = State.Events.ToList(),
            MvpPlayerId = mvpId,
            SvpPlayerId = svpId,
        };
    }

    private static MatchState InitializeState(MatchConfig config)
    {
        var state = new MatchState
        {
            BallPossessionTeamId = config.HomeTeam.Id,
            BallZone = FieldZone.MidfieldCenter,
            Phase = MatchPhase.FirstHalf,
            HomeActivePlayerIds = [.. config.HomeTactics?.StartingPlayerIds ?? config.HomeTeam.StartingLineup],
            AwayActivePlayerIds = [.. config.AwayTactics?.StartingPlayerIds ?? config.AwayTeam.StartingLineup],
        };

        RatingCalculator.InitializeRatings(state, config);
        TickProcessor.InitializeStamina(state, config);

        return state;
    }
}
