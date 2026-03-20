using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// A locker room card presented at halftime. Player picks 1 of 3.
/// </summary>
public sealed record LockerRoomCard
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required CardEffect Effect { get; init; }

    /// <summary>Magnitude of the effect (e.g., +15 morale, +20 stamina).</summary>
    public required int Magnitude { get; init; }
}
