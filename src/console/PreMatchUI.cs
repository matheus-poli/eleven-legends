using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Console;

/// <summary>
/// Pre-match tactical menu: formation, lineup, style.
/// </summary>
public static class PreMatchUI
{
    /// <summary>
    /// Prompts the player for tactical setup. Returns null for "Auto" (use defaults).
    /// </summary>
    public static TacticalSetup? Prompt(Club club)
    {
        System.Console.WriteLine("\n╔══════════════════════════════════════╗");
        System.Console.WriteLine("║        📋 PRE-MATCH TACTICS         ║");
        System.Console.WriteLine("╚══════════════════════════════════════╝");

        System.Console.WriteLine($"\n  {club.Name} — Squad ({club.Team.Players.Count} players):\n");

        // Show squad
        for (int i = 0; i < club.Team.Players.Count; i++)
        {
            var p = club.Team.Players[i];
            bool isStarter = club.Team.StartingLineup.Contains(p.Id);
            float ovr = p.PrimaryPosition == Position.GK
                ? p.Attributes.GoalkeeperOverall
                : p.Attributes.OutfieldOverall;
            string tag = isStarter ? "★" : " ";
            System.Console.WriteLine(
                $"  {tag} {i + 1,2}. {p.Name,-20} {p.PrimaryPosition,-4} OVR:{ovr:F0}  Morale:{p.Morale}");
        }

        System.Console.WriteLine("\n  Choose an option:");
        System.Console.WriteLine("  1. Auto (default formation & lineup)");
        System.Console.WriteLine("  2. Choose formation & style");

        int choice = ReadChoice(1, 2);
        if (choice == 1)
            return null;

        // Choose formation
        System.Console.WriteLine("\n  🏟️ Choose Formation:\n");
        for (int i = 0; i < Formation.Presets.Count; i++)
        {
            var f = Formation.Presets[i];
            System.Console.WriteLine($"  {i + 1}. {f.Name}");
        }
        int formIdx = ReadChoice(1, Formation.Presets.Count) - 1;
        var formation = Formation.Presets[formIdx];

        // Choose style
        System.Console.WriteLine("\n  🎯 Choose Tactical Style:\n");
        System.Console.WriteLine("  1. ⚔️  Attacking (+possession, +shots)");
        System.Console.WriteLine("  2. ⚖️  Balanced");
        System.Console.WriteLine("  3. 🛡️ Defensive (-possession, +defense)");
        int styleChoice = ReadChoice(1, 3);
        var style = styleChoice switch
        {
            1 => TacticalStyle.Attacking,
            3 => TacticalStyle.Defensive,
            _ => TacticalStyle.Balanced
        };

        // Auto-assign best players to formation positions
        var lineup = AutoAssignLineup(club.Team, formation);

        System.Console.WriteLine($"\n  ✅ Formation: {formation.Name} | Style: {style}");
        System.Console.WriteLine("  Starting XI:");
        for (int i = 0; i < lineup.Count; i++)
        {
            var player = club.Team.Players.First(p => p.Id == lineup[i]);
            System.Console.WriteLine($"    {formation.Positions[i],-4} → {player.Name}");
        }

        return new TacticalSetup
        {
            Formation = formation,
            Style = style,
            StartingPlayerIds = lineup
        };
    }

    /// <summary>
    /// Auto-assigns the best available players to each formation position.
    /// </summary>
    public static List<int> AutoAssignLineup(Team team, Formation formation)
    {
        var assigned = new HashSet<int>();
        var lineup = new List<int>(11);

        foreach (var pos in formation.Positions)
        {
            // Find best unassigned player for this position
            var best = team.Players
                .Where(p => !assigned.Contains(p.Id))
                .OrderByDescending(p => PositionFit(p, pos))
                .ThenByDescending(p => p.PrimaryPosition == Position.GK
                    ? p.Attributes.GoalkeeperOverall
                    : p.Attributes.OutfieldOverall)
                .FirstOrDefault();

            if (best != null)
            {
                lineup.Add(best.Id);
                assigned.Add(best.Id);
            }
        }

        return lineup;
    }

    private static int PositionFit(Player player, Position targetPos)
    {
        if (player.PrimaryPosition == targetPos) return 100;
        if (player.SecondaryPosition == targetPos) return 80;
        if (SameZone(player.PrimaryPosition, targetPos)) return 50;
        return 0;
    }

    private static bool SameZone(Position a, Position b)
    {
        return GetZone(a) == GetZone(b);
    }

    private static int GetZone(Position pos) => pos switch
    {
        Position.GK => 0,
        Position.CB or Position.LB or Position.RB or Position.LWB or Position.RWB => 1,
        Position.CDM or Position.CM or Position.CAM or Position.LM or Position.RM => 2,
        Position.LW or Position.RW or Position.CF or Position.ST => 3,
        _ => 2
    };

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
