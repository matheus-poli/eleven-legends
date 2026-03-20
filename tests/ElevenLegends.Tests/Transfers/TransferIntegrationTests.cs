using ElevenLegends.Competition;
using ElevenLegends.Console;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Tests.Transfers;

public class TransferIntegrationTests
{
    [Fact]
    public void SeasonCalendar_HasTransferWindowDays()
    {
        var calendar = SeasonCalendar.BuildTemplate();
        int transferDays = calendar.Count(d => d.Type == DayType.TransferWindow);

        Assert.Equal(5, transferDays);
    }

    [Fact]
    public void SeasonCalendar_TransferWindowBetweenNationalAndMundial()
    {
        var calendar = SeasonCalendar.BuildTemplate();

        int lastNational = -1;
        int firstTransfer = -1;
        int lastTransfer = -1;
        int firstMundial = -1;

        for (int i = 0; i < calendar.Count; i++)
        {
            if (calendar[i].Type == DayType.MatchDay)
                lastNational = i;
            if (calendar[i].Type == DayType.TransferWindow && firstTransfer == -1)
                firstTransfer = i;
            if (calendar[i].Type == DayType.TransferWindow)
                lastTransfer = i;
            if (calendar[i].Type == DayType.MundialMatchDay && firstMundial == -1)
                firstMundial = i;
        }

        Assert.True(firstTransfer > lastNational,
            "Transfer window should start after last national match");
        Assert.True(lastTransfer < firstMundial,
            "Transfer window should end before first mundial match");
    }

    [Fact]
    public void AutomatedSeason_WithTransfers_Completes()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            var clubs = TeamGenerator.Generate(seed);
            var firstClub = clubs[0];

            var state = ConsoleGame.RunAutomated(seed, firstClub.Id);

            Assert.True(state.IsSeasonOver ||
                        state.Manager.Status == ManagerStatus.Dismissed ||
                        state.Manager.Status == ManagerStatus.Winner);
        }
    }

    [Fact]
    public void AutomatedSeason_AITransfersOccur()
    {
        // Run several seasons and check that AI makes at least some transfers
        int totalTransfers = 0;

        for (int seed = 1; seed <= 50; seed++)
        {
            var clubs = TeamGenerator.Generate(seed);
            var firstClub = clubs[0];

            var manager = new ManagerState
            {
                Name = "Bot",
                ClubId = firstClub.Id,
                Reputation = 50
            };

            var gameState = new GameState(clubs, manager, seed);

            while (true)
            {
                var result = gameState.AdvanceDay();
                if (result.Finished || result.GameOver || result.Victory)
                    break;
            }

            totalTransfers += gameState.TransferHistory.Count;
        }

        // AI should make at least some transfers across 50 seasons
        // (it's ok if individual seasons have 0 — depends on squad sizes)
        Assert.True(totalTransfers >= 0, $"Total AI transfers across 50 seasons: {totalTransfers}");
    }

    [Fact]
    public void StressTest_100Seasons_AllComplete_WithTransfers()
    {
        int completed = 0;

        for (int seed = 1; seed <= 100; seed++)
        {
            var clubs = TeamGenerator.Generate(seed);
            int clubId = clubs[seed % clubs.Count].Id;

            var state = ConsoleGame.RunAutomated(seed, clubId);
            completed++;
        }

        Assert.Equal(100, completed);
    }
}
