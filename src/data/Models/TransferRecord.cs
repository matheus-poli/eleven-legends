using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// Represents a completed or pending transfer.
/// </summary>
public sealed record TransferRecord
{
    public required TransferType Type { get; init; }
    public required int PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public int? FromClubId { get; init; }
    public int? ToClubId { get; init; }
    public decimal Fee { get; init; }
    public int Day { get; init; }
}
