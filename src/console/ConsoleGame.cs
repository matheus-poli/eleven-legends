using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.Manager;

namespace ElevenLegends.Console;

/// <summary>
/// Playable console prototype for the Eleven Legends demo.
/// </summary>
public static class ConsoleGame
{
    public static void Run(int seed = 42)
    {
        PrintHeader();

        var clubs = TeamGenerator.Generate(seed);
        var playerClub = ChooseClub(clubs);

        var manager = new ManagerState
        {
            Name = "You",
            ClubId = playerClub.Id,
            Reputation = 50
        };

        var gameState = new GameState(clubs, manager, seed);

        System.Console.WriteLine($"\n⚽ Season begins! You manage {playerClub.Name} ({playerClub.Country})");
        System.Console.WriteLine($"💰 Club balance: {playerClub.Balance:C0}");
        System.Console.WriteLine($"⭐ Reputation: {manager.Reputation}/100\n");

        while (true)
        {
            var day = gameState.CurrentDay;
            PrintDaySeparator(day);

            var result = gameState.AdvanceDay();

            if (day.Type is DayType.MatchDay or DayType.MundialMatchDay)
            {
                PrintFixtures(result.Fixtures, clubs, playerClub.Id);
                PrintBracketStatus(gameState.Competition, clubs);
            }

            PrintEconomy(playerClub, manager);

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
