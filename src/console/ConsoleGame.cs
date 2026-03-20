using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.Manager;
using ElevenLegends.Persistence;
using ElevenLegends.Simulation;

namespace ElevenLegends.Console;

/// <summary>
/// Playable console prototype for the Eleven Legends demo.
/// </summary>
public static class ConsoleGame
{
    public static void Run(int seed = 42)
    {
        PrintHeader();

        var savesDir = Path.Combine(Directory.GetCurrentDirectory(), "saves");
        var saveManager = new SaveManager(savesDir);

        GameState gameState;
        Club playerClub;
        List<Club> clubs;

        // Check for existing save
        if (saveManager.HasAutoSave())
        {
            System.Console.WriteLine("💾 Autosave found!");
            System.Console.Write("  [C]ontinue or [N]ew game? ");
            var choice = System.Console.ReadLine()?.Trim().ToUpperInvariant();

            if (choice == "C")
            {
                gameState = saveManager.LoadGame("autosave");
                clubs = gameState.Clubs.ToList();
                playerClub = gameState.PlayerClub;
                System.Console.WriteLine($"  ✅ Loaded! Day {gameState.CurrentDayIndex + 1}, managing {playerClub.Name}");
            }
            else
            {
                (gameState, playerClub, clubs) = StartNewGame(seed);
            }
        }
        else
        {
            (gameState, playerClub, clubs) = StartNewGame(seed);
        }

        var manager = gameState.Manager;

        System.Console.WriteLine($"\n⚽ Season begins! You manage {playerClub.Name} ({playerClub.Country})");
        System.Console.WriteLine($"💰 Club balance: {playerClub.Balance:C0}");
        System.Console.WriteLine($"⭐ Reputation: {manager.Reputation}/100\n");

        while (true)
        {
            var day = gameState.CurrentDay;
            PrintDaySeparator(day);

            DayResult result;

            bool isMatchDay = day.Type is DayType.MatchDay or DayType.MundialMatchDay;
            bool isTransferDay = day.Type == DayType.TransferWindow;

            if (isMatchDay)
            {
                result = RunInteractiveMatchDay(gameState, playerClub, clubs);
            }
            else if (isTransferDay)
            {
                result = RunInteractiveTransferDay(gameState, playerClub, clubs);
            }
            else
            {
                result = gameState.AdvanceDay();
            }

            if (isMatchDay)
            {
                PrintFixtures(result.Fixtures, clubs, playerClub.Id);
                PrintBracketStatus(gameState.Competition, clubs);
            }

            if (isTransferDay && result.TransferRecords.Count > 0)
            {
                System.Console.WriteLine("\n  📰 AI Transfer News:");
                foreach (var tr in result.TransferRecords)
                {
                    var fromClub = clubs.FirstOrDefault(c => c.Id == tr.FromClubId);
                    var toClub = clubs.FirstOrDefault(c => c.Id == tr.ToClubId);
                    System.Console.WriteLine(
                        $"    {tr.PlayerName}: {fromClub?.Name ?? "?"} → {toClub?.Name ?? "?"} ({tr.Fee:C0})");
                }
            }

            PrintEconomy(playerClub, manager);

            // Autosave after each day
            try { saveManager.AutoSave(gameState); }
            catch { /* Don't crash the game if save fails */ }

            if (result.Victory)
            {
                PrintVictory(playerClub);
                return;
            }

            if (result.GameOver)
            {
                PrintGameOver(playerClub);
                return;
            }

            if (result.Finished)
            {
                PrintSeasonEnd(gameState, playerClub);
                return;
            }

            WaitForInput();
        }
    }

    private static (GameState gameState, Club playerClub, List<Club> clubs) StartNewGame(int seed)
    {
        var clubs = TeamGenerator.Generate(seed);
        var playerClub = ChooseClub(clubs);

        var manager = new ManagerState
        {
            Name = "You",
            ClubId = playerClub.Id,
            Reputation = 50
        };

        var gameState = new GameState(clubs, manager, seed);
        return (gameState, playerClub, clubs);
    }

    /// <summary>
    /// Runs an interactive match day: pre-match UI → first half → halftime UI → second half.
    /// </summary>
    private static DayResult RunInteractiveMatchDay(
        GameState gameState, Club playerClub, List<Club> clubs)
    {
        var ctx = gameState.PrepareMatchDay();

        if (ctx.PlayerFixture == null)
        {
            // Player's club eliminated — just finish the day
            return gameState.FinishDay(ctx, null);
        }

        bool isHome = ctx.PlayerFixture.HomeClubId == playerClub.Id;
        var opponentClub = clubs.First(c => c.Id ==
            (isHome ? ctx.PlayerFixture.AwayClubId : ctx.PlayerFixture.HomeClubId));

        System.Console.WriteLine($"\n  ⚽ YOUR MATCH: {playerClub.Name} vs {opponentClub.Name}");

        // Pre-match: tactical decisions
        var tactics = PreMatchUI.Prompt(playerClub);
        var config = gameState.BuildPlayerMatchConfig(ctx, tactics);

        // Simulate first half
        System.Console.WriteLine("\n  ⏱️ Simulating first half...");
        var (state, rng) = MatchSimulator.SimulateFirstHalf(config);

        // Show halftime summary
        MatchEventDisplay.PrintHalftimeSummary(
            state, playerClub.Team, opponentClub.Team, isHome);

        // Halftime decisions: cards + substitutions
        var (card, subs) = HalftimeUI.Prompt(
            state, config, playerClub.Team, isHome, rng);

        // Apply halftime effects
        if (card != null)
            HalftimeProcessor.ApplyCard(state, config, card, isHome);
        if (subs.Count > 0)
            HalftimeProcessor.ApplySubstitutions(state, config, subs, isHome);

        // Simulate second half
        System.Console.WriteLine("\n  ⏱️ Simulating second half...");
        var matchResult = MatchSimulator.SimulateSecondHalf(state, config, rng);

        // Show final result
        MatchEventDisplay.PrintMatchResult(
            matchResult, playerClub.Team, opponentClub.Team, isHome);

        return gameState.FinishDay(ctx, matchResult);
    }

    /// <summary>
    /// Runs an interactive transfer window day.
    /// </summary>
    private static DayResult RunInteractiveTransferDay(
        GameState gameState, Club playerClub, List<Club> clubs)
    {
        // Count which transfer day this is (1-5)
        int transferDayNum = 1;
        for (int i = 0; i < gameState.CurrentDayIndex; i++)
        {
            if (gameState.Calendar[i].Type == DayType.TransferWindow)
                transferDayNum++;
        }

        // Player's interactive transfers
        var playerRecords = TransferWindowUI.RunTransferDay(gameState, playerClub, clubs, transferDayNum);

        // Now advance the day (AI transfers + salary/career processing)
        var result = gameState.AdvanceDay();

        // Merge player's records into the result
        var allRecords = new List<TransferRecord>(playerRecords);
        allRecords.AddRange(result.TransferRecords);
        result.TransferRecords = allRecords;

        return result;
    }

    /// <summary>
    /// Runs a full season automatically (no user input). For testing.
    /// </summary>
    public static GameState RunAutomated(int seed, int clubId)
    {
        var clubs = TeamGenerator.Generate(seed);
        var manager = new ManagerState
        {
            Name = "Bot",
            ClubId = clubId,
            Reputation = 50
        };

        var gameState = new GameState(clubs, manager, seed);

        while (true)
        {
            var result = gameState.AdvanceDay();
            if (result.Finished || result.GameOver || result.Victory)
                break;
        }

        return gameState;
    }

    private static void PrintHeader()
    {
        System.Console.WriteLine("╔══════════════════════════════════════╗");
        System.Console.WriteLine("║       ⚽ ELEVEN LEGENDS ⚽          ║");
        System.Console.WriteLine("║     Football Manager Demo           ║");
        System.Console.WriteLine("╚══════════════════════════════════════╝");
        System.Console.WriteLine();
    }

    private static Club ChooseClub(List<Club> clubs)
    {
        var countries = clubs.Select(c => c.Country).Distinct().ToList();

        System.Console.WriteLine("🌍 Choose your country:\n");
        for (int i = 0; i < countries.Count; i++)
            System.Console.WriteLine($"  {i + 1}. {countries[i]}");

        int countryIdx = ReadChoice(1, countries.Count) - 1;
        string country = countries[countryIdx];

        var countryClubs = clubs.Where(c => c.Country == country).ToList();
        System.Console.WriteLine($"\n🏟️  Choose your club in {country}:\n");
        for (int i = 0; i < countryClubs.Count; i++)
        {
            var club = countryClubs[i];
            float overall = club.Team.Players.Average(p =>
                p.PrimaryPosition == Position.GK
                    ? p.Attributes.GoalkeeperOverall
                    : p.Attributes.OutfieldOverall);
            System.Console.WriteLine(
                $"  {i + 1}. {club.Name,-25} ⭐{club.Reputation,-3} 💰{club.Balance,10:C0}  OVR: {overall:F0}");
        }

        int clubIdx = ReadChoice(1, countryClubs.Count) - 1;
        return countryClubs[clubIdx];
    }

    private static void PrintDaySeparator(SeasonDay day)
    {
        string typeEmoji = day.Type switch
        {
            DayType.Training => "🏋️",
            DayType.MatchDay => "⚽",
            DayType.MundialMatchDay => "🏆",
            DayType.TransferWindow => "🔄",
            DayType.Rest => "😴",
            _ => "📅"
        };
        System.Console.WriteLine($"\n{new string('─', 40)}");
        System.Console.WriteLine($"  {typeEmoji} Day {day.Day} — {day.Type}");
        System.Console.WriteLine(new string('─', 40));
    }

    private static void PrintFixtures(
        IReadOnlyList<MatchFixture> fixtures, List<Club> clubs, int playerClubId)
    {
        System.Console.WriteLine("\n📋 Results:\n");
        foreach (var f in fixtures)
        {
            var home = clubs.First(c => c.Id == f.HomeClubId);
            var away = clubs.First(c => c.Id == f.AwayClubId);
            string score = f.Result.HasValue
                ? $"{f.Result.Value.Home} - {f.Result.Value.Away}"
                : "? - ?";
            bool isPlayerMatch = f.HomeClubId == playerClubId || f.AwayClubId == playerClubId;
            string marker = isPlayerMatch ? " ◀ YOUR MATCH" : "";
            System.Console.WriteLine($"  {home.Name,-25} {score} {away.Name,-25}{marker}");
        }
    }

    private static void PrintBracketStatus(CompetitionManager competition, List<Club> clubs)
    {
        System.Console.WriteLine("\n🏆 Bracket Status:");
        foreach (var (country, bracket) in competition.NationalBrackets)
        {
            string status = bracket.IsFinished
                ? $"Champion: {clubs.First(c => c.Id == bracket.ChampionId!.Value).Name}"
                : $"Phase: {bracket.CurrentPhase}";
            System.Console.WriteLine($"  {country}: {status}");
        }

        if (competition.MundialBracket != null)
        {
            var mundial = competition.MundialBracket;
            string status = mundial.IsFinished
                ? $"Champion: {clubs.First(c => c.Id == mundial.ChampionId!.Value).Name}"
                : $"Phase: {mundial.CurrentPhase}";
            System.Console.WriteLine($"  🌍 Mundial: {status}");
        }
    }

    private static void PrintEconomy(Club club, ManagerState manager)
    {
        System.Console.WriteLine(
            $"\n  💰 Club: {club.Balance:C0} | 👔 Manager: {manager.PersonalBalance:C0} | ⭐ Rep: {manager.Reputation}");
    }

    private static void PrintVictory(Club club)
    {
        System.Console.WriteLine("\n╔══════════════════════════════════════╗");
        System.Console.WriteLine("║         🏆 CONGRATULATIONS! 🏆       ║");
        System.Console.WriteLine("╚══════════════════════════════════════╝");
        System.Console.WriteLine($"\n  {club.Name} won the MUNDIAL!");
        System.Console.WriteLine("  You've been called to manage the national team!");
        System.Console.WriteLine("\n  Thanks for playing Eleven Legends Demo! ⚽");
    }

    private static void PrintGameOver(Club club)
    {
        System.Console.WriteLine("\n╔══════════════════════════════════════╗");
        System.Console.WriteLine("║           💀 GAME OVER 💀            ║");
        System.Console.WriteLine("╚══════════════════════════════════════╝");
        System.Console.WriteLine($"\n  {club.Name} went bankrupt!");
        System.Console.WriteLine("  You've been dismissed as manager.");
        System.Console.WriteLine("\n  Better luck next time! ⚽");
    }

    private static void PrintSeasonEnd(GameState state, Club club)
    {
        System.Console.WriteLine("\n╔══════════════════════════════════════╗");
        System.Console.WriteLine("║         📅 SEASON COMPLETE 📅        ║");
        System.Console.WriteLine("╚══════════════════════════════════════╝");
        System.Console.WriteLine($"\n  Final balance: {club.Balance:C0}");
        System.Console.WriteLine($"  Reputation: {state.Manager.Reputation}/100");

        var mundialChamp = state.Competition.GetMundialChampion();
        if (mundialChamp.HasValue)
        {
            var champ = state.Clubs.First(c => c.Id == mundialChamp.Value);
            System.Console.WriteLine($"  Mundial Champion: {champ.Name}");
        }
    }

    private static int ReadChoice(int min, int max)
    {
        while (true)
        {
            System.Console.Write($"\n  Choose ({min}-{max}): ");
            if (int.TryParse(System.Console.ReadLine(), out int choice)
                && choice >= min && choice <= max)
                return choice;
            System.Console.WriteLine("  Invalid choice. Try again.");
        }
    }

    private static void WaitForInput()
    {
        System.Console.Write("\n  Press Enter to continue...");
        System.Console.ReadLine();
    }
}
