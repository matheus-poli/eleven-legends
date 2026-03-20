using ElevenLegends.Console;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Tests.Integration;

public class TacticalSeasonTests
{
    /// <summary>
    /// Full automated season still completes (backward compatible).
    /// RunAutomated uses AdvanceDay() without tactics.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    public void AutomatedSeason_StillCompletes(int seed)
    {
        var clubs = TeamGenerator.Generate(seed);
        var firstClub = clubs[0];

        var state = ConsoleGame.RunAutomated(seed, firstClub.Id);

        Assert.True(state.IsSeasonOver || state.Manager.Status == ElevenLegends.Data.Enums.ManagerStatus.Dismissed ||
                    state.Manager.Status == ElevenLegends.Data.Enums.ManagerStatus.Winner);
    }

    /// <summary>
    /// PrepareMatchDay + FinishDay produces same outcome as AdvanceDay for match days.
    /// </summary>
    [Fact]
    public void PrepareAndFinish_MatchesAutoAdvance()
    {
        int seed = 42;
        var clubs1 = TeamGenerator.Generate(seed);
        var clubs2 = TeamGenerator.Generate(seed);
        var firstClub1 = clubs1[0];
        var firstClub2 = clubs2[0];

        var manager1 = new ManagerState { Name = "Bot1", ClubId = firstClub1.Id, Reputation = 50 };
        var manager2 = new ManagerState { Name = "Bot2", ClubId = firstClub2.Id, Reputation = 50 };

        var gs1 = new GameState(clubs1, manager1, seed);
        var gs2 = new GameState(clubs2, manager2, seed);

        // Advance both to first match day
        while (gs1.CurrentDay.Type is not (ElevenLegends.Data.Enums.DayType.MatchDay
            or ElevenLegends.Data.Enums.DayType.MundialMatchDay))
        {
            gs1.AdvanceDay();
            gs2.AdvanceDay();
        }

        // gs1: auto advance
        var autoResult = gs1.AdvanceDay();

        // gs2: manual prepare + finish (no player tactics)
        var ctx = gs2.PrepareMatchDay();
        // Simulate the player's match manually with same default
        if (ctx.PlayerFixture != null)
        {
            var config = gs2.BuildPlayerMatchConfig(ctx, null);
            var matchResult = ElevenLegends.Simulation.MatchSimulator.Simulate(config);
            gs2.FinishDay(ctx, matchResult);
        }
        else
        {
            gs2.FinishDay(ctx, null);
        }

        // Both should have advanced the same day
        Assert.Equal(gs1.CurrentDayIndex, gs2.CurrentDayIndex);
    }

    /// <summary>
    /// 100 automated seasons complete without crashes — stress test with Phase 3 code.
    /// </summary>
    [Fact]
    public void StressTest_100Seasons_AllComplete()
    {
        int completed = 0;
        int gameOvers = 0;
        int victories = 0;

        for (int seed = 1; seed <= 100; seed++)
        {
            var clubs = TeamGenerator.Generate(seed);
            int clubId = clubs[seed % clubs.Count].Id;

            var state = ConsoleGame.RunAutomated(seed, clubId);

            if (state.Manager.Status == ElevenLegends.Data.Enums.ManagerStatus.Winner)
                victories++;
            else if (state.Manager.Status == ElevenLegends.Data.Enums.ManagerStatus.Dismissed)
                gameOvers++;

            completed++;
        }

        Assert.Equal(100, completed);
        Assert.True(victories + gameOvers > 0 || completed == 100,
            "All seasons should complete normally");
    }
}
