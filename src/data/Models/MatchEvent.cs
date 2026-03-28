using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// A single event that occurred during a match tick.
/// </summary>
public sealed record MatchEvent
{
    public required int Tick { get; init; }
    public required EventType Type { get; init; }

    /// <summary>The player who performed the action.</summary>
    public required int PlayerId { get; init; }

    /// <summary>Optional secondary player (e.g., assist provider, fouled player).</summary>
    public int? SecondaryPlayerId { get; init; }

    /// <summary>Human-readable description of the event.</summary>
    public string Description { get; init; } = "";

    /// <summary>Rating impact for the primary player.</summary>
    public float RatingImpact { get; init; }
}
