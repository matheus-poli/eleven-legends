using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Transfers;

/// <summary>
/// Calculates market value for players based on overall rating, age, and club reputation.
/// </summary>
public static class PlayerValuation
{
    /// <summary>
    /// Calculates a player's market value in the transfer market.
    /// </summary>
    public static decimal Calculate(Player player, int clubReputation = 50)
    {
        float overall = player.PrimaryPosition == Position.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        // Exponential base value from overall (65 ovr ≈ 50k, 75 ≈ 150k, 85 ≈ 500k)
        decimal baseValue = (decimal)Math.Pow(overall / 10.0, 3.5) * 15m;

        // Age factor: peak 25-28, young premium, old discount
        decimal ageFactor = GetAgeFactor(player.Age);

        // Club reputation adds 0-30% markup
        decimal repBonus = 1m + clubReputation / 500m;

        decimal value = baseValue * ageFactor * repBonus;

        // Minimum value, rounded to nearest 1000
        return Math.Max(5_000m, Math.Round(value / 1000m) * 1000m);
    }

    /// <summary>
    /// Gets a salary estimate for a player (weekly cost).
    /// </summary>
    public static decimal EstimateWeeklySalary(Player player)
    {
        float overall = player.PrimaryPosition == Position.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        return (decimal)overall * 10m;
    }

    private static decimal GetAgeFactor(int age) => age switch
    {
        <= 17 => 0.6m,
        18 => 0.8m,
        19 => 0.9m,
        20 => 1.0m,
        21 => 1.05m,
        22 => 1.1m,
        23 => 1.15m,
        24 => 1.2m,
        >= 25 and <= 28 => 1.2m,
        29 => 1.1m,
        30 => 0.95m,
        31 => 0.8m,
        32 => 0.65m,
        33 => 0.5m,
        34 => 0.35m,
        _ => 0.25m
    };
}
