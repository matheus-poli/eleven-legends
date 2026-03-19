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
    /// </summary>
    public static List<SeasonDay> BuildTemplate()
    {
        var days = new List<SeasonDay>();
        int day = 1;

        // Phase 1: National knockouts
        // Training block before quarterfinals
        days.AddRange(TrainingBlock(ref day, 3));
        days.Add(new SeasonDay { Day = day++, Type = DayType.MatchDay });     // Quarterfinals

        days.AddRange(TrainingBlock(ref day, 2));
        days.Add(new SeasonDay { Day = day++, Type = DayType.MatchDay });     // Semifinals

        days.AddRange(TrainingBlock(ref day, 2));
        days.Add(new SeasonDay { Day = day++, Type = DayType.MatchDay });     // Finals

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
