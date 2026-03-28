using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.Economy;

namespace ElevenLegends.Tests.Economy;

public class EconomyProcessorTests
{
    private static Club CreateTestClub(decimal balance = 200_000m, int reputation = 70)
    {
        var clubs = TeamGenerator.Generate(42);
        var club = clubs[0];
        club.Balance = balance;
        club.Reputation = reputation;
        return club;
    }

    [Fact]
    public void MatchRevenue_Scales_With_Reputation()
    {
        var club = CreateTestClub(reputation: 80);
        decimal revenue = EconomyProcessor.CalculateMatchRevenue(club);
        Assert.Equal(80 * 200m, revenue);
    }

    [Fact]
    public void WeeklySalary_Scales_With_Squad_Quality()
    {
        var club = CreateTestClub();
        decimal salary = EconomyProcessor.CalculateWeeklySalary(club);
        Assert.True(salary > 0, "Salary should be positive");
    }

    [Fact]
    public void PhasePrizes_Increase_Per_Round()
    {
        decimal qfPrize = EconomyProcessor.GetPhasePrize(CompetitionPhase.Quarterfinals);
        decimal sfPrize = EconomyProcessor.GetPhasePrize(CompetitionPhase.Semifinals);
        decimal finalPrize = EconomyProcessor.GetPhasePrize(CompetitionPhase.Final);
        decimal mundialFinalPrize = EconomyProcessor.GetPhasePrize(CompetitionPhase.MundialFinal);

        Assert.True(sfPrize > qfPrize);
        Assert.True(finalPrize > sfPrize);
        Assert.True(mundialFinalPrize > finalPrize);
    }

    [Fact]
    public void ProcessMatchDay_Increases_Balance()
    {
        var club = CreateTestClub(balance: 100_000m);
        decimal before = club.Balance;

        EconomyProcessor.ProcessMatchDay(club, CompetitionPhase.Quarterfinals, won: true);

        Assert.True(club.Balance > before);
    }

    [Fact]
    public void ProcessWeeklySalary_Decreases_Balance()
    {
        var club = CreateTestClub(balance: 100_000m);
        decimal before = club.Balance;

        EconomyProcessor.ProcessWeeklySalary(club);

        Assert.True(club.Balance < before);
    }

    [Fact]
    public void IsBankrupt_When_Negative_Balance()
    {
        var club = CreateTestClub(balance: -1m);
        Assert.True(EconomyProcessor.IsBankrupt(club));
    }

    [Fact]
    public void IsNotBankrupt_When_Positive_Balance()
    {
        var club = CreateTestClub(balance: 100_000m);
        Assert.False(EconomyProcessor.IsBankrupt(club));
    }
}
