namespace ElevenLegends.Data.Models;

/// <summary>
/// Final result of a simulated match.
/// </summary>
public sealed record MatchResult
{
    public required MatchState FinalState { get; init; }
    public required IReadOnlyList<MatchEvent> Events { get; init; }

    /// <summary>Player ID with the highest rating (Man of the Match).</summary>
    public required int MvpPlayerId { get; init; }

    /// <summary>Player ID with the second highest rating.</summary>
    public required int SvpPlayerId { get; init; }

    public int ScoreHome => FinalState.ScoreHome;
    public int ScoreAway => FinalState.ScoreAway;
}
