using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Gacha;
using ElevenLegends.Simulation;

namespace ElevenLegends.Console;

/// <summary>
/// Halftime UI: locker room cards + substitutions.
/// </summary>
public static class HalftimeUI
{
    /// <summary>
    /// Prompts the player for halftime decisions (interactive mode).
    /// Returns the chosen card and substitutions.
    /// </summary>
    public static (LockerRoomCard? Card, List<Substitution> Subs) Prompt(
        MatchState state, MatchConfig config, Team playerTeam, bool isHome, IRng rng)
    {
        // Generate cards based on context
        int scoreDiff = isHome
            ? state.ScoreHome - state.ScoreAway
            : state.ScoreAway - state.ScoreHome;

        var activeIds = isHome ? state.HomeActivePlayerIds : state.AwayActivePlayerIds;
        float avgStamina = activeIds.Count > 0
            ? activeIds.Where(id => state.PlayerStamina.ContainsKey(id))
                       .Average(id => state.PlayerStamina[id])
            : 50f;
        float avgMorale = playerTeam.Players.Average(p => (float)p.Morale);

        var cards = LockerRoomCardGenerator.Generate(rng, scoreDiff, avgStamina, avgMorale);

        // Show cards
        System.Console.WriteLine("\n  🃏 LOCKER ROOM CARDS — Choose 1:\n");
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            string effectIcon = card.Effect switch
            {
                CardEffect.MoraleBoost => "💪",
                CardEffect.StaminaRecovery => "⚡",
                CardEffect.TeamBuff => "📈",
                CardEffect.OpponentDebuff => "📉",
                _ => "🃏"
            };
            System.Console.WriteLine($"  {i + 1}. {effectIcon} {card.Name}");
            System.Console.WriteLine($"     {card.Description}");
        }

        int cardChoice = ReadChoice(1, cards.Count) - 1;
        var chosenCard = cards[cardChoice];
        System.Console.WriteLine($"\n  ✅ Selected: {chosenCard.Name}");

        // Substitutions
        var subs = new List<Substitution>();
        var benchPlayers = playerTeam.Players
            .Where(p => !activeIds.Contains(p.Id))
            .ToList();

        if (benchPlayers.Count > 0)
        {
            System.Console.WriteLine($"\n  🔄 Substitutions (up to 3, enter 0 to skip):");

            for (int subNum = 0; subNum < 3 && benchPlayers.Count > 0; subNum++)
            {
                System.Console.WriteLine($"\n  Sub {subNum + 1}/3 — Who comes OFF? (0 = done)");
                for (int i = 0; i < activeIds.Count; i++)
                {
                    var p = playerTeam.Players.First(pp => pp.Id == activeIds[i]);
                    float stam = state.PlayerStamina.TryGetValue(p.Id, out float s) ? s : 0f;
                    float rating = state.PlayerRatings.TryGetValue(p.Id, out float r) ? r : 6f;
                    System.Console.WriteLine(
                        $"    {i + 1,2}. {p.Name,-20} {p.PrimaryPosition,-4} ⚡{stam:F0} 📊{rating:F1}");
                }

                int outChoice = ReadChoice(0, activeIds.Count);
                if (outChoice == 0) break;

                int playerOutId = activeIds[outChoice - 1];

                System.Console.WriteLine("\n  Who comes ON?");
                for (int i = 0; i < benchPlayers.Count; i++)
                {
                    var p = benchPlayers[i];
                    float ovr = p.PrimaryPosition == Position.GK
                        ? p.Attributes.GoalkeeperOverall
                        : p.Attributes.OutfieldOverall;
                    System.Console.WriteLine(
                        $"    {i + 1}. {p.Name,-20} {p.PrimaryPosition,-4} OVR:{ovr:F0}");
                }

                int inChoice = ReadChoice(1, benchPlayers.Count) - 1;
                var incoming = benchPlayers[inChoice];

                subs.Add(new Substitution
                {
                    PlayerOutId = playerOutId,
                    PlayerInId = incoming.Id
                });

                benchPlayers.RemoveAt(inChoice);
                var outPlayer = playerTeam.Players.First(p => p.Id == playerOutId);
                System.Console.WriteLine($"  ✅ {outPlayer.Name} ↔ {incoming.Name}");
            }
        }

        return (chosenCard, subs);
    }

    /// <summary>
    /// Auto-selects halftime decisions (for automated/AI matches).
    /// Picks the first card and no substitutions.
    /// </summary>
    public static (LockerRoomCard? Card, List<Substitution> Subs) AutoDecide(
        MatchState state, Team team, bool isHome, IRng rng)
    {
        int scoreDiff = isHome
            ? state.ScoreHome - state.ScoreAway
            : state.ScoreAway - state.ScoreHome;

        var activeIds = isHome ? state.HomeActivePlayerIds : state.AwayActivePlayerIds;
        float avgStamina = activeIds.Count > 0
            ? activeIds.Where(id => state.PlayerStamina.ContainsKey(id))
                       .Average(id => state.PlayerStamina[id])
            : 50f;
        float avgMorale = team.Players.Average(p => (float)p.Morale);

        var cards = LockerRoomCardGenerator.Generate(rng, scoreDiff, avgStamina, avgMorale);

        // AI picks first card, no subs (simple for demo)
        return (cards.Count > 0 ? cards[0] : null, []);
    }

    private static int ReadChoice(int min, int max)
    {
        while (true)
        {
            System.Console.Write($"\n  Choose ({min}-{max}): ");
            if (int.TryParse(System.Console.ReadLine(), out int choice)
                && choice >= min && choice <= max)
                return choice;
            System.Console.WriteLine("  Invalid choice.");
        }
    }
}
