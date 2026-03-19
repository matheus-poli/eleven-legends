namespace ElevenLegends.Data.Models;

/// <summary>
/// Configuration for a match. Includes the seed for deterministic RNG.
/// </summary>
public sealed record MatchConfig
{
    public required Team HomeTeam { get; init; }
    public required Team AwayTeam { get; init; }

    /// <summary>Seed for the RandomNumberGenerator — ensures reproducible results.</summary>
    public required int Seed { get; init; }
}
