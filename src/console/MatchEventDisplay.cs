using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Console;

/// <summary>
/// Formats and displays match events, ratings, and summaries for the console.
/// </summary>
public static class MatchEventDisplay
{
    /// <summary>
    /// Formats key events from a list of match events.
    /// </summary>
    public static List<string> FormatKeyEvents(IReadOnlyList<MatchEvent> events)
    {
        var lines = new List<string>();
        foreach (var evt in events)
        {
            string emoji = evt.Type switch
            {
                EventType.Goal => "⚽",
                EventType.Assist => "👟",
                EventType.Save => "🧤",
                EventType.FreeKick => "🏳️",
                EventType.Foul => "⚠️",
                EventType.YellowCard => "🟨",
                EventType.RedCard => "🟥",
                EventType.ShotOnTarget => "🎯",
                _ => "📋"
            };

            if (evt.Type is EventType.Goal or EventType.Assist or EventType.Save
                or EventType.YellowCard or EventType.RedCard)
            {
                lines.Add($"  {emoji} {evt.Tick}' — {evt.Description}");
            }
        }
        return lines;
    }

    /// <summary>
    /// Prints a half-time summary.
    /// </summary>
    public static void PrintHalftimeSummary(
        MatchState state, Team playerTeam, Team opponentTeam, bool isHome)
    {
        int playerScore = isHome ? state.ScoreHome : state.ScoreAway;
        int oppScore = isHome ? state.ScoreAway : state.ScoreHome;

        System.Console.WriteLine("\n╔══════════════════════════════════════╗");
        System.Console.WriteLine("║          ⏰ HALF-TIME ⏰             ║");
        System.Console.WriteLine("╚══════════════════════════════════════╝");
        System.Console.WriteLine($"\n  {playerTeam.Name} {playerScore} × {oppScore} {opponentTeam.Name}");
        System.Console.WriteLine($"  Posse: {(isHome ? state.PossessionHome : 1f - state.PossessionHome) * 100:F0}%");

        // Key events
        var events = FormatKeyEvents(state.Events);
        if (events.Count > 0)
        {
            System.Console.WriteLine("\n  📋 Events:");
            foreach (var line in events)
                System.Console.WriteLine(line);
        }

        // Player team ratings
        System.Console.WriteLine($"\n  📊 Player Ratings ({playerTeam.Name}):");
        var teamPlayerIds = new HashSet<int>(playerTeam.StartingLineup);
        var ratings = state.PlayerRatings
            .Where(r => teamPlayerIds.Contains(r.Key))
            .OrderByDescending(r => r.Value)
            .ToList();

        foreach (var (pid, rating) in ratings)
        {
            var player = playerTeam.Players.First(p => p.Id == pid);
            string bar = rating >= 7f ? "★" : rating >= 6f ? "●" : "○";
            float stamina = state.PlayerStamina.TryGetValue(pid, out float s) ? s : 0f;
            System.Console.WriteLine(
                $"    {bar} {player.Name,-20} {player.PrimaryPosition,-4} {rating:F1}  ⚡{stamina:F0}");
        }
    }

    /// <summary>
    /// Prints the final match result with ratings.
    /// </summary>
    public static void PrintMatchResult(
        MatchResult result, Team playerTeam, Team opponentTeam, bool isHome)
    {
        var state = result.FinalState;
        int playerScore = isHome ? state.ScoreHome : state.ScoreAway;
        int oppScore = isHome ? state.ScoreAway : state.ScoreHome;

        string outcome = playerScore > oppScore ? "VICTORY! 🎉" :
                         playerScore < oppScore ? "DEFEAT 😞" : "DRAW";

        System.Console.WriteLine($"\n  🏁 FULL TIME: {playerTeam.Name} {playerScore} × {oppScore} {opponentTeam.Name}");
        System.Console.WriteLine($"  Result: {outcome}");
        System.Console.WriteLine($"  Possession: {(isHome ? state.PossessionHome : 1f - state.PossessionHome) * 100:F0}%");

        // Key events
        var events = FormatKeyEvents(state.Events);
        if (events.Count > 0)
        {
            System.Console.WriteLine("\n  📋 Match Events:");
            foreach (var line in events)
                System.Console.WriteLine(line);
        }

        // Ratings
        System.Console.WriteLine($"\n  📊 Final Ratings ({playerTeam.Name}):");
        var teamPlayerIds = new HashSet<int>(playerTeam.StartingLineup);
        var ratings = state.PlayerRatings
            .Where(r => teamPlayerIds.Contains(r.Key) ||
                        playerTeam.Players.Any(p => p.Id == r.Key))
            .OrderByDescending(r => r.Value)
            .Take(11)
            .ToList();

        foreach (var (pid, rating) in ratings)
        {
            var player = playerTeam.Players.FirstOrDefault(p => p.Id == pid);
            if (player == null) continue;
            string bar = rating >= 7.5f ? "★★" : rating >= 7f ? "★" : rating >= 6f ? "●" : "○";
            System.Console.WriteLine($"    {bar} {player.Name,-20} {player.PrimaryPosition,-4} {rating:F1}");
        }

        // MVP / SVP
        var mvp = FindPlayer(result.MvpPlayerId, playerTeam, opponentTeam);
        var svp = FindPlayer(result.SvpPlayerId, playerTeam, opponentTeam);
        if (mvp != null)
            System.Console.WriteLine($"\n  🏅 MVP: {mvp.Name} ({state.PlayerRatings[mvp.Id]:F1})");
        if (svp != null)
            System.Console.WriteLine($"  🥈 SVP: {svp.Name} ({state.PlayerRatings[svp.Id]:F1})");
    }

    private static Player? FindPlayer(int id, Team a, Team b) =>
        a.Players.FirstOrDefault(p => p.Id == id)
        ?? b.Players.FirstOrDefault(p => p.Id == id);
}
