using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Competition;

/// <summary>
/// Represents a single day in the season calendar.
/// </summary>
public sealed record SeasonDay
{
    public required int Day { get; init; }
    public required DayType Type { get; init; }
    public IReadOnlyList<MatchFixture> Fixtures { get; init; } = [];
}

/// <summary>
/// Builds the full season calendar: national knockouts → mundial.
/// Structure: Training → Quarters → Training → Semis → Training → Final →
///            Training → Mundial Semis → Training → Mundial Final
/// </summary>
public static class SeasonCalendar
{
    /// <summary>
    /// Generates the complete season schedule. Match fixtures are filled in
    /// progressively as bracket rounds advance.
    /// Now includes a transfer window between nationals and mundial.
    /// </summary>
    public static List<SeasonDay> BuildTemplate()
    {
        var days = new List<SeasonDay>();
        int day = 1;

        // Phase 1: National knockouts
        days.AddRange(TrainingBlock(ref day, 3));
        days.Add(new SeasonDay { Day = day++, Type = DayType.MatchDay });     // Quarterfinals

        days.AddRange(TrainingBlock(ref day, 2));
        days.Add(new SeasonDay { Day = day++, Type = DayType.MatchDay });     // Semifinals

        days.AddRange(TrainingBlock(ref day, 2));
        days.Add(new SeasonDay { Day = day++, Type = DayType.MatchDay });     // Finals

        // Transfer window between nationals and mundial
        days.Add(new SeasonDay { Day = day++, Type = DayType.Rest });         // Rest before window
        for (int i = 0; i < 5; i++)
            days.Add(new SeasonDay { Day = day++, Type = DayType.TransferWindow });
        days.Add(new SeasonDay { Day = day++, Type = DayType.Rest });         // Rest after window

        // Phase 2: Mundial
        days.AddRange(TrainingBlock(ref day, 3));
        days.Add(new SeasonDay { Day = day++, Type = DayType.MundialMatchDay }); // Mundial Semis

        days.AddRange(TrainingBlock(ref day, 2));
        days.Add(new SeasonDay { Day = day++, Type = DayType.MundialMatchDay }); // Mundial Final

        return days;
    }

    private static List<SeasonDay> TrainingBlock(ref int day, int count)
    {
        var block = new List<SeasonDay>();
        for (int i = 0; i < count; i++)
        {
            block.Add(new SeasonDay { Day = day++, Type = DayType.Training });
        }
        return block;
    }
}
