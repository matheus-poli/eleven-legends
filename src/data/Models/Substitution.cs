namespace ElevenLegends.Data.Models;

/// <summary>
/// A substitution to be made at halftime.
/// </summary>
public sealed record Substitution
{
    public required int PlayerOutId { get; init; }
    public required int PlayerInId { get; init; }
}
