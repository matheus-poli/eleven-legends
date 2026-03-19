using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// Mutable state of an in-progress match. Updated each tick.
/// </summary>
public sealed class MatchState
{
    public int ScoreHome { get; set; }
    public int ScoreAway { get; set; }

    /// <summary>Home team possession ratio. 0.0–1.0.</summary>
    public float PossessionHome { get; set; } = 0.5f;

    public int CurrentTick { get; set; }
    public MatchPhase Phase { get; set; } = MatchPhase.FirstHalf;

    /// <summary>Which team currently has the ball (home or away team ID).</summary>
    public int BallPossessionTeamId { get; set; }

    /// <summary>Current zone of the ball on the field.</summary>
    public FieldZone BallZone { get; set; } = FieldZone.MidfieldCenter;

    /// <summary>All events generated so far.</summary>
    public List<MatchEvent> Events { get; } = [];

    /// <summary>Current rating per player. Key = PlayerId, Value = rating (base 6.0).</summary>
    public Dictionary<int, float> PlayerRatings { get; } = new();

    /// <summary>Current stamina per player. Key = PlayerId, Value = stamina (0–100, degrades).</summary>
    public Dictionary<int, float> PlayerStamina { get; } = new();

    /// <summary>Ticks where home team had possession (for possession % calculation).</summary>
    public int HomePossessionTicks { get; set; }

    /// <summary>Total ticks played so far.</summary>
    public int TotalTicksPlayed { get; set; }
}
