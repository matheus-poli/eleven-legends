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

    /// <summary>Tactical setup for the home team. Null = use defaults.</summary>
    public TacticalSetup? HomeTactics { get; init; }

    /// <summary>Tactical setup for the away team. Null = use defaults.</summary>
    public TacticalSetup? AwayTactics { get; init; }

    /// <summary>Substitutions to apply at halftime for home team.</summary>
    public IReadOnlyList<Substitution>? HomeSubstitutions { get; init; }

    /// <summary>Substitutions to apply at halftime for away team.</summary>
    public IReadOnlyList<Substitution>? AwaySubstitutions { get; init; }

    /// <summary>Locker room card chosen at halftime for home team. Null = none.</summary>
    public LockerRoomCard? HomeCard { get; init; }

    /// <summary>Locker room card chosen at halftime for away team. Null = none.</summary>
    public LockerRoomCard? AwayCard { get; init; }
}
