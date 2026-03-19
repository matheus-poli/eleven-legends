namespace ElevenLegends.Data.Models;

/// <summary>
/// A club with its team, finances, and reputation.
/// </summary>
public sealed class Club
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Country { get; init; }
    public required Team Team { get; set; }

    /// <summary>Club financial balance. Can go negative (triggers bankruptcy).</summary>
    public decimal Balance { get; set; }

    /// <summary>Club reputation 0–100. Affects revenue and job proposals.</summary>
    public int Reputation { get; set; }
}
