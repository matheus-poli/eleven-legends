namespace ElevenLegends.Data.Models;

/// <summary>
/// Tracks an active loan (player temporarily at another club).
/// </summary>
public sealed class LoanRecord
{
    public required int PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public required int OriginClubId { get; init; }
    public required int HostClubId { get; init; }
}
