using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// Full tactical setup for a match: formation, style, and lineup.
/// </summary>
public sealed record TacticalSetup
{
    public required Formation Formation { get; init; }
    public TacticalStyle Style { get; init; } = TacticalStyle.Balanced;

    /// <summary>Player IDs for the starting 11, matching Formation.Positions order.</summary>
    public required IReadOnlyList<int> StartingPlayerIds { get; init; }
}
