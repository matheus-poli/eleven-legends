using ElevenLegends.Data.Enums;

namespace ElevenLegends.Data.Models;

/// <summary>
/// A scheduled match between two clubs in a competition phase.
/// </summary>
public sealed class MatchFixture
{
    public required int Day { get; init; }
    public required int HomeClubId { get; init; }
    public required int AwayClubId { get; init; }
    public required CompetitionPhase Phase { get; init; }

    /// <summary>Match result: (HomeGoals, AwayGoals). Null until played.</summary>
    public (int Home, int Away)? Result { get; set; }

    /// <summary>The winning club ID, or null if not yet played.</summary>
    public int? WinnerClubId => Result switch
    {
        null => null,
        var r when r.Value.Home > r.Value.Away => HomeClubId,
        var r when r.Value.Away > r.Value.Home => AwayClubId,
        // Draw goes to home team (simplified for demo — no penalty shootouts)
        _ => HomeClubId
    };
}
