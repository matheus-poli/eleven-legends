using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// A training choice offered to the manager.
/// </summary>
public sealed record TrainingChoice
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required TrainingType Type { get; init; }
}

/// <summary>
/// Result of a training session with per-player events.
/// </summary>
public sealed record TrainingResult
{
    public required TrainingChoice Choice { get; init; }
    public required IReadOnlyList<TrainingPlayerEvent> Events { get; init; }
}

/// <summary>
/// An individual player event during training.
/// </summary>
public sealed record TrainingPlayerEvent
{
    public required int PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required string Description { get; init; }
    public int MoraleDelta { get; init; }
    public int ChemistryDelta { get; init; }
    public bool IsPositive { get; init; } = true;
}
