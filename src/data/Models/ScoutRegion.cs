namespace ElevenLegends.Data.Models;

/// <summary>
/// A scoutable region with its name, scouting cost, and name pools for player generation.
/// </summary>
public sealed record ScoutRegion
{
    public required string Name { get; init; }
    public required decimal Cost { get; init; }
    public required IReadOnlyList<string> FirstNames { get; init; }
    public required IReadOnlyList<string> LastNames { get; init; }
}
