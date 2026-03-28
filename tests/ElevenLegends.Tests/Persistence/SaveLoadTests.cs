using ElevenLegends.Console;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;

namespace ElevenLegends.Tests.Persistence;

public class SaveLoadTests : IDisposable
{
    private readonly string _testDir;

    public SaveLoadTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"eleven-legends-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void RoundTrip_FreshGame_AllStatePreserved()
    {
        var filePath = Path.Combine(_testDir, "test1.db");
        var clubs = TeamGenerator.Generate(42);
        var manager = new ManagerState
        {
            Name = "Test Manager",
            ClubId = clubs[0].Id,
            Reputation = 65
        };

        var original = new GameState(clubs, manager, 42);

        // Save
        GameSaver.Save(original, filePath);

        // Load
        var loaded = GameLoader.Load(filePath);

        // Verify core state
        Assert.Equal(original.CurrentDayIndex, loaded.CurrentDayIndex);
        Assert.Equal(original.Manager.Name, loaded.Manager.Name);
        Assert.Equal(original.Manager.ClubId, loaded.Manager.ClubId);
        Assert.Equal(original.Manager.Reputation, loaded.Manager.Reputation);
        Assert.Equal(original.Manager.Status, loaded.Manager.Status);

        // Verify clubs
        Assert.Equal(original.Clubs.Count, loaded.Clubs.Count);
        for (int i = 0; i < original.Clubs.Count; i++)
        {
            Assert.Equal(original.Clubs[i].Id, loaded.Clubs[i].Id);
            Assert.Equal(original.Clubs[i].Name, loaded.Clubs[i].Name);
            Assert.Equal(original.Clubs[i].Country, loaded.Clubs[i].Country);
            Assert.Equal(original.Clubs[i].Balance, loaded.Clubs[i].Balance);
            Assert.Equal(original.Clubs[i].Reputation, loaded.Clubs[i].Reputation);
        }
    }

    [Fact]
    public void RoundTrip_AllPlayersPreserved()
    {
        var filePath = Path.Combine(_testDir, "test2.db");
        var clubs = TeamGenerator.Generate(42);
        var manager = new ManagerState { Name = "Bot", ClubId = clubs[0].Id, Reputation = 50 };
        var original = new GameState(clubs, manager, 42);

        GameSaver.Save(original, filePath);
        var loaded = GameLoader.Load(filePath);

        foreach (var origClub in original.Clubs)
        {
            var loadedClub = loaded.Clubs.First(c => c.Id == origClub.Id);

            Assert.Equal(origClub.Team.Players.Count, loadedClub.Team.Players.Count);

            foreach (var origPlayer in origClub.Team.Players)
            {
                var loadedPlayer = loadedClub.Team.Players.First(p => p.Id == origPlayer.Id);

                Assert.Equal(origPlayer.Name, loadedPlayer.Name);
                Assert.Equal(origPlayer.PrimaryPosition, loadedPlayer.PrimaryPosition);
                Assert.Equal(origPlayer.Age, loadedPlayer.Age);
                Assert.Equal(origPlayer.Morale, loadedPlayer.Morale);
                Assert.Equal(origPlayer.Chemistry, loadedPlayer.Chemistry);

                // Verify attributes
                Assert.Equal(origPlayer.Attributes.Finishing, loadedPlayer.Attributes.Finishing);
                Assert.Equal(origPlayer.Attributes.Passing, loadedPlayer.Attributes.Passing);
                Assert.Equal(origPlayer.Attributes.Speed, loadedPlayer.Attributes.Speed);
                Assert.Equal(origPlayer.Attributes.Stamina, loadedPlayer.Attributes.Stamina);
                Assert.Equal(origPlayer.Attributes.Reflexes, loadedPlayer.Attributes.Reflexes);
            }

            // Verify starting lineup
            Assert.Equal(origClub.Team.StartingLineup.Count, loadedClub.Team.StartingLineup.Count);
            for (int i = 0; i < origClub.Team.StartingLineup.Count; i++)
                Assert.Equal(origClub.Team.StartingLineup[i], loadedClub.Team.StartingLineup[i]);
        }
    }

    [Fact]
    public void RoundTrip_AfterSeveralDays_StatePreserved()
    {
        var filePath = Path.Combine(_testDir, "test3.db");
        var clubs = TeamGenerator.Generate(42);
        var manager = new ManagerState { Name = "Bot", ClubId = clubs[0].Id, Reputation = 50 };
        var original = new GameState(clubs, manager, 42);

        // Advance several days
        for (int i = 0; i < 10; i++)
        {
            var result = original.AdvanceDay();
            if (result.Finished || result.GameOver) break;
        }

        GameSaver.Save(original, filePath);
        var loaded = GameLoader.Load(filePath);

        Assert.Equal(original.CurrentDayIndex, loaded.CurrentDayIndex);
        Assert.Equal(original.Manager.Reputation, loaded.Manager.Reputation);
        Assert.Equal(original.Manager.PersonalBalance, loaded.Manager.PersonalBalance);

        // Verify club balances changed and were preserved
        for (int i = 0; i < original.Clubs.Count; i++)
        {
            Assert.Equal(original.Clubs[i].Balance, loaded.Clubs[i].Balance);
        }
    }

    [Fact]
    public void RoundTrip_CompleteSeason_BracketsPreserved()
    {
        var filePath = Path.Combine(_testDir, "test4.db");

        // Run until season is over
        var state = ConsoleGame.RunAutomated(42, 1);

        GameSaver.Save(state, filePath);
        var loaded = GameLoader.Load(filePath);

        // Verify season completion flags
        Assert.Equal(state.CurrentDayIndex, loaded.CurrentDayIndex);

        // Verify competition brackets
        Assert.Equal(
            state.Competition.NationalBrackets.Count,
            loaded.Competition.NationalBrackets.Count);

        foreach (var kvp in state.Competition.NationalBrackets)
        {
            Assert.True(loaded.Competition.NationalBrackets.ContainsKey(kvp.Key));
            var origBracket = kvp.Value;
            var loadBracket = loaded.Competition.NationalBrackets[kvp.Key];

            Assert.Equal(origBracket.CurrentPhase, loadBracket.CurrentPhase);
            Assert.Equal(origBracket.ChampionId, loadBracket.ChampionId);
            Assert.Equal(origBracket.Fixtures.Count, loadBracket.Fixtures.Count);
        }

        // Verify mundial
        if (state.Competition.MundialBracket != null)
        {
            Assert.NotNull(loaded.Competition.MundialBracket);
            Assert.Equal(
                state.Competition.MundialBracket.ChampionId,
                loaded.Competition.MundialBracket.ChampionId);
        }
    }

    [Fact]
    public void RoundTrip_TransferHistory_Preserved()
    {
        var filePath = Path.Combine(_testDir, "test5.db");
        var clubs = TeamGenerator.Generate(42);
        var manager = new ManagerState { Name = "Bot", ClubId = clubs[0].Id, Reputation = 50 };
        var original = new GameState(clubs, manager, 42);

        // Run through entire season (includes transfer window)
        while (true)
        {
            var result = original.AdvanceDay();
            if (result.Finished || result.GameOver || result.Victory) break;
        }

        GameSaver.Save(original, filePath);
        var loaded = GameLoader.Load(filePath);

        Assert.Equal(original.TransferHistory.Count, loaded.TransferHistory.Count);
        for (int i = 0; i < original.TransferHistory.Count; i++)
        {
            Assert.Equal(original.TransferHistory[i].Type, loaded.TransferHistory[i].Type);
            Assert.Equal(original.TransferHistory[i].PlayerId, loaded.TransferHistory[i].PlayerId);
            Assert.Equal(original.TransferHistory[i].PlayerName, loaded.TransferHistory[i].PlayerName);
            Assert.Equal(original.TransferHistory[i].Fee, loaded.TransferHistory[i].Fee);
        }
    }

    [Fact]
    public void SaveManager_AutoSave_Works()
    {
        var sm = new SaveManager(_testDir);
        var clubs = TeamGenerator.Generate(42);
        var manager = new ManagerState { Name = "Bot", ClubId = clubs[0].Id, Reputation = 50 };
        var gs = new GameState(clubs, manager, 42);

        Assert.False(sm.HasAutoSave());

        sm.AutoSave(gs);

        Assert.True(sm.HasAutoSave());
        var loaded = sm.LoadGame("autosave");
        Assert.Equal(gs.Manager.Name, loaded.Manager.Name);
    }

    [Fact]
    public void SaveManager_MultipleSaveSlots()
    {
        var sm = new SaveManager(_testDir);
        var clubs = TeamGenerator.Generate(42);
        var manager = new ManagerState { Name = "Bot", ClubId = clubs[0].Id, Reputation = 50 };
        var gs = new GameState(clubs, manager, 42);

        sm.SaveGame(gs, "slot1");
        sm.SaveGame(gs, "slot2");
        sm.SaveGame(gs, "slot3");

        var saves = sm.ListSaves();
        Assert.Equal(3, saves.Count);
        Assert.Contains(saves, s => s.SlotName == "slot1");
        Assert.Contains(saves, s => s.SlotName == "slot2");
        Assert.Contains(saves, s => s.SlotName == "slot3");
    }

    [Fact]
    public void SaveManager_DeleteSave()
    {
        var sm = new SaveManager(_testDir);
        var clubs = TeamGenerator.Generate(42);
        var manager = new ManagerState { Name = "Bot", ClubId = clubs[0].Id, Reputation = 50 };
        var gs = new GameState(clubs, manager, 42);

        sm.SaveGame(gs, "to-delete");
        Assert.True(sm.SlotExists("to-delete"));

        sm.DeleteSave("to-delete");
        Assert.False(sm.SlotExists("to-delete"));
    }

    [Fact]
    public void Load_NonExistentFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            GameLoader.Load(Path.Combine(_testDir, "nonexistent.db")));
    }

    [Fact]
    public void StressTest_SaveLoadAcross50Seasons()
    {
        int successCount = 0;

        for (int seed = 1; seed <= 50; seed++)
        {
            var filePath = Path.Combine(_testDir, $"stress_{seed}.db");
            var clubs = TeamGenerator.Generate(seed);
            var manager = new ManagerState
            {
                Name = "Stress Bot",
                ClubId = clubs[seed % clubs.Count].Id,
                Reputation = 50
            };

            var gs = new GameState(clubs, manager, seed);

            // Advance to mid-season
            for (int d = 0; d < 15; d++)
            {
                var result = gs.AdvanceDay();
                if (result.Finished || result.GameOver) break;
            }

            // Save
            GameSaver.Save(gs, filePath);

            // Load and verify key data
            var loaded = GameLoader.Load(filePath);
            Assert.Equal(gs.CurrentDayIndex, loaded.CurrentDayIndex);
            Assert.Equal(gs.Clubs.Count, loaded.Clubs.Count);
            Assert.Equal(gs.Manager.Name, loaded.Manager.Name);

            // Clean up to save disk
            File.Delete(filePath);
            successCount++;
        }

        Assert.Equal(50, successCount);
    }
}
