using ElevenLegends.Console;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.Manager;

namespace ElevenLegends.Tests.Integration;

public class FullSeasonTests
{
    [Fact]
    public void Single_Season_Completes_Without_Errors()
    {
        var state = ConsoleGame.RunAutomated(seed: 42, clubId: 1);

        Assert.True(state.Competition.IsSeasonComplete(),
            "Season should complete (all nationals + mundial)");
        Assert.NotNull(state.Competition.GetMundialChampion());
    }

    [Fact]
    public void Season_Is_Deterministic()
    {
        var state1 = ConsoleGame.RunAutomated(seed: 42, clubId: 1);
        var state2 = ConsoleGame.RunAutomated(seed: 42, clubId: 1);

        Assert.Equal(
            state1.Competition.GetMundialChampion(),
            state2.Competition.GetMundialChampion());

        Assert.Equal(state1.PlayerClub.Balance, state2.PlayerClub.Balance);
    }

    [Fact]
    public void Different_Seeds_May_Produce_Different_Champions()
    {
        var champions = new HashSet<int?>();
        for (int seed = 1; seed <= 20; seed++)
        {
            var state = ConsoleGame.RunAutomated(seed: seed, clubId: 1);
            champions.Add(state.Competition.GetMundialChampion());
        }

        // With 20 different seeds, we should get at least 2 different champions
        Assert.True(champions.Count >= 2,
            $"Expected variety in champions, got {champions.Count} unique across 20 seeds");
    }

    [Theory]
    [InlineData(42)]
    [InlineData(123)]
    [InlineData(999)]
    [InlineData(7777)]
    [InlineData(31415)]
    public void Season_Completes_For_Various_Seeds(int seed)
    {
        var state = ConsoleGame.RunAutomated(seed: seed, clubId: 1);
        Assert.True(state.Competition.IsSeasonComplete());
    }

    [Fact]
    public void Economy_Stays_Reasonable_Over_Season()
    {
        var state = ConsoleGame.RunAutomated(seed: 42, clubId: 1);

        foreach (var club in state.Clubs)
        {
            // No club should have absurd balance (positive or negative)
            Assert.InRange(club.Balance, -500_000m, 2_000_000m);
        }
    }

    [Fact]
    public void Hundred_Seasons_All_Complete()
    {
        for (int seed = 1; seed <= 100; seed++)
        {
            var state = ConsoleGame.RunAutomated(seed: seed, clubId: 1);
            Assert.True(state.Competition.IsSeasonComplete(),
                $"Season with seed {seed} did not complete");
            Assert.NotNull(state.Competition.GetMundialChampion());
        }
    }
}
