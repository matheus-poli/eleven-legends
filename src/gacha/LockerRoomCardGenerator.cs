using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Gacha;

/// <summary>
/// Generates contextual locker room cards at halftime.
/// Cards are recurrent (not consumable) — drawn fresh each halftime.
/// </summary>
public static class LockerRoomCardGenerator
{
    private static readonly LockerRoomCard[] CardPool =
    [
        new() { Name = "Discurso Inspirador", Description = "Moral do time +15", Effect = CardEffect.MoraleBoost, Magnitude = 15 },
        new() { Name = "Pep Talk Individual", Description = "Moral do jogador mais fraco +25", Effect = CardEffect.MoraleBoost, Magnitude = 25 },
        new() { Name = "Garra e Raça", Description = "Moral do time +10, Stamina +10", Effect = CardEffect.MoraleBoost, Magnitude = 10 },
        new() { Name = "Hidratação Especial", Description = "Stamina do time +20", Effect = CardEffect.StaminaRecovery, Magnitude = 20 },
        new() { Name = "Recuperação Física", Description = "Stamina dos 3 mais cansados +30", Effect = CardEffect.StaminaRecovery, Magnitude = 30 },
        new() { Name = "Segundo Fôlego", Description = "Stamina do time +15", Effect = CardEffect.StaminaRecovery, Magnitude = 15 },
        new() { Name = "Ajuste Tático", Description = "Buff de atributos +5 no 2° tempo", Effect = CardEffect.TeamBuff, Magnitude = 5 },
        new() { Name = "Análise de Vídeo", Description = "Buff de decisões +8 no 2° tempo", Effect = CardEffect.TeamBuff, Magnitude = 8 },
        new() { Name = "Pressão Psicológica", Description = "Moral do adversário -10", Effect = CardEffect.OpponentDebuff, Magnitude = 10 },
        new() { Name = "Provocação Tática", Description = "Moral do adversário -15", Effect = CardEffect.OpponentDebuff, Magnitude = 15 },
    ];

    /// <summary>
    /// Generates 3 contextual cards based on match state at halftime.
    /// Cards are weighted by context: losing → more recovery, winning → more buffs.
    /// </summary>
    public static List<LockerRoomCard> Generate(IRng rng, int scoreDiff, float avgStamina, float avgMorale)
    {
        var weighted = new List<(LockerRoomCard card, int weight)>();

        foreach (var card in CardPool)
        {
            int weight = 10; // base weight

            // Losing: favor recovery and morale
            if (scoreDiff < 0)
            {
                if (card.Effect is CardEffect.MoraleBoost or CardEffect.StaminaRecovery)
                    weight += 15;
            }
            // Winning: favor buffs and debuffs
            else if (scoreDiff > 0)
            {
                if (card.Effect is CardEffect.TeamBuff or CardEffect.OpponentDebuff)
                    weight += 15;
            }

            // Low stamina: favor recovery
            if (avgStamina < 50f)
            {
                if (card.Effect == CardEffect.StaminaRecovery)
                    weight += 10;
            }

            // Low morale: favor morale boost
            if (avgMorale < 40f)
            {
                if (card.Effect == CardEffect.MoraleBoost)
                    weight += 10;
            }

            weighted.Add((card, weight));
        }

        // Weighted random selection of 3 unique cards
        var selected = new List<LockerRoomCard>(3);
        var available = new List<(LockerRoomCard card, int weight)>(weighted);

        for (int i = 0; i < 3 && available.Count > 0; i++)
        {
            int totalWeight = available.Sum(w => w.weight);
            int roll = rng.NextInt(0, totalWeight - 1);
            int cumulative = 0;

            for (int j = 0; j < available.Count; j++)
            {
                cumulative += available[j].weight;
                if (roll < cumulative)
                {
                    selected.Add(available[j].card);
                    available.RemoveAt(j);
                    break;
                }
            }
        }

        return selected;
    }
}
