using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// Represents a player in the squad.
/// </summary>
public sealed record Player
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required Position PrimaryPosition { get; init; }
    public Position? SecondaryPosition { get; init; }
    public required PlayerAttributes Attributes { get; init; }
    public IReadOnlyList<string> Traits { get; init; } = [];
    public int Age { get; init; }

    /// <summary>Emotional state affecting performance. 0–100.</summary>
    public int Morale { get; init; } = 50;

    /// <summary>Cohesion bonus. 0–100.</summary>
    public int Chemistry { get; init; } = 50;
}
