using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Economy;

/// <summary>
/// Processes daily economy: match revenue, weekly salary, phase prizes, bankruptcy.
/// </summary>
public static class EconomyProcessor
{
    /// <summary>Base match-day revenue per reputation point.</summary>
    private const decimal RevenuePerReputation = 200m;

    /// <summary>Prize money per competition phase.</summary>
    private static readonly Dictionary<CompetitionPhase, decimal> PhasePrizes = new()
    {
        [CompetitionPhase.Quarterfinals] = 10_000m,
        [CompetitionPhase.Semifinals] = 25_000m,
        [CompetitionPhase.Final] = 50_000m,
        [CompetitionPhase.MundialSemifinals] = 100_000m,
        [CompetitionPhase.MundialFinal] = 200_000m
    };

    /// <summary>
    /// Processes match-day revenue for a club (gate receipts based on reputation).
    /// </summary>
    public static decimal CalculateMatchRevenue(Club club) =>
        club.Reputation * RevenuePerReputation;

    /// <summary>
    /// Calculates the weekly salary bill based on squad average overall.
    /// </summary>
    public static decimal CalculateWeeklySalary(Club club)
    {
        var players = club.Team.Players;
        if (players.Count == 0) return 0;

        float avgOverall = players.Average(p =>
            p.PrimaryPosition == Position.GK
                ? p.Attributes.GoalkeeperOverall
                : p.Attributes.OutfieldOverall);

        // Salary scales with squad quality: better teams pay more
        return (decimal)avgOverall * players.Count * 10m;
    }

    /// <summary>
    /// Returns the prize for advancing past a competition phase.
    /// </summary>
    public static decimal GetPhasePrize(CompetitionPhase phase) =>
        PhasePrizes.TryGetValue(phase, out var prize) ? prize : 0m;

    /// <summary>
    /// Applies match-day economics to a club.
    /// </summary>
    public static void ProcessMatchDay(Club club, CompetitionPhase phase, bool won)
    {
        club.Balance += CalculateMatchRevenue(club);
        if (won)
            club.Balance += GetPhasePrize(phase);
    }

    /// <summary>
    /// Deducts weekly salary. Call once per week (every 7 days).
    /// </summary>
    public static void ProcessWeeklySalary(Club club)
    {
        club.Balance -= CalculateWeeklySalary(club);
    }

    /// <summary>
    /// Returns true if the club is bankrupt (negative balance).
    /// </summary>
    public static bool IsBankrupt(Club club) => club.Balance < 0;
}
