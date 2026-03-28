namespace ElevenLegends.Data.Models;

/// <summary>
/// A team with its squad and starting lineup.
/// </summary>
public sealed record Team
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<Player> Players { get; init; }

    /// <summary>List of player IDs in the starting 11, ordered by position.</summary>
    public required IReadOnlyList<int> StartingLineup { get; init; }
}
