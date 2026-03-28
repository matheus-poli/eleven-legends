using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// The manager's career state: employment, reputation, finances.
/// </summary>
public sealed class ManagerState
{
    public required string Name { get; init; }
    public ManagerStatus Status { get; set; } = ManagerStatus.Employed;
    public int ClubId { get; set; }

    /// <summary>Manager reputation 0–100. Affects job proposals.</summary>
    public int Reputation { get; set; } = 50;

    /// <summary>Manager personal balance (salary + bonuses).</summary>
    public decimal PersonalBalance { get; set; } = 5_000m;

    /// <summary>Monthly salary from current club.</summary>
    public decimal Salary { get; set; } = 2_000m;
}
