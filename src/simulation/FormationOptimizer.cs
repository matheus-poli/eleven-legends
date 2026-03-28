using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Greedy optimizer that assigns the best-fit player to each formation slot
/// based on position-specific overall ratings.
/// </summary>
public static class FormationOptimizer
{
    /// <summary>
    /// Returns 11 player IDs in formation-position order, maximizing
    /// each slot's OverallForPosition. Greedy: picks best available per slot.
    /// </summary>
    public static IReadOnlyList<int> OptimalLineup(
        IReadOnlyList<Player> squad, Formation formation)
    {
        var available = new HashSet<int>(squad.Select(p => p.Id));
        var playerMap = squad.ToDictionary(p => p.Id);
        int[] result = new int[formation.Positions.Count];

        // Sort slots by scarcity: GK first (fewest candidates), then others
        int[] slotOrder = Enumerable.Range(0, formation.Positions.Count)
            .OrderBy(i => formation.Positions[i] == Position.GK ? 0 : 1)
            .ThenBy(i => CountCandidates(squad, formation.Positions[i]))
            .ToArray();

        foreach (int slotIdx in slotOrder)
        {
            Position slotPos = formation.Positions[slotIdx];
            int bestId = -1;
            float bestOvr = float.MinValue;

            foreach (int pid in available)
            {
                Player p = playerMap[pid];
                float ovr = p.Attributes.OverallForPosition(slotPos);

                // Prefer players whose primary/secondary matches the slot
                if (p.PrimaryPosition == slotPos)
                    ovr += 5f;
                else if (p.SecondaryPosition == slotPos)
                    ovr += 2f;

                if (ovr > bestOvr)
                {
                    bestOvr = ovr;
                    bestId = pid;
                }
            }

            result[slotIdx] = bestId;
            available.Remove(bestId);
        }

        return result;
    }

    /// <summary>
    /// Calculates the average position-specific overall of an optimal lineup.
    /// </summary>
    public static float AverageOverall(IReadOnlyList<Player> squad, Formation formation)
    {
        IReadOnlyList<int> lineup = OptimalLineup(squad, formation);
        var playerMap = squad.ToDictionary(p => p.Id);

        float total = 0f;
        for (int i = 0; i < lineup.Count; i++)
        {
            if (playerMap.TryGetValue(lineup[i], out Player? player))
            {
                total += player.Attributes.OverallForPosition(formation.Positions[i]);
            }
        }

        return total / lineup.Count;
    }

    private static int CountCandidates(IReadOnlyList<Player> squad, Position pos)
    {
        return squad.Count(p => p.PrimaryPosition == pos || p.SecondaryPosition == pos);
    }
}
