using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Applies halftime effects: locker room cards and substitutions.
/// </summary>
public static class HalftimeProcessor
{
    /// <summary>
    /// Applies a locker room card's effects to the match state.
    /// </summary>
    public static void ApplyCard(MatchState state, MatchConfig config, LockerRoomCard card, bool isHomeTeam)
    {
        var team = isHomeTeam ? config.HomeTeam : config.AwayTeam;
        var activeIds = isHomeTeam ? state.HomeActivePlayerIds : state.AwayActivePlayerIds;
        var playerIds = activeIds.Count > 0 ? activeIds : (IReadOnlyList<int>)team.StartingLineup;

        switch (card.Effect)
        {
            case CardEffect.MoraleBoost:
                // Bonus modifier applied to all success calculations in 2nd half
                if (isHomeTeam)
                    state.HomeBonusModifier += card.Magnitude * 0.15f;
                else
                    state.AwayBonusModifier += card.Magnitude * 0.15f;
                break;

            case CardEffect.StaminaRecovery:
                foreach (var pid in playerIds)
                {
                    if (state.PlayerStamina.TryGetValue(pid, out float stam))
                        state.PlayerStamina[pid] = Math.Min(100f, stam + card.Magnitude);
                }
                break;

            case CardEffect.TeamBuff:
                if (isHomeTeam)
                    state.HomeBonusModifier += card.Magnitude;
                else
                    state.AwayBonusModifier += card.Magnitude;
                break;

            case CardEffect.OpponentDebuff:
                // Apply negative modifier to the opponent
                if (isHomeTeam)
                    state.AwayBonusModifier -= card.Magnitude * 0.5f;
                else
                    state.HomeBonusModifier -= card.Magnitude * 0.5f;
                break;
        }
    }

    /// <summary>
    /// Processes substitutions: swaps players in active lineup, initializes stamina for subs.
    /// </summary>
    public static void ApplySubstitutions(
        MatchState state, MatchConfig config, IReadOnlyList<Substitution> subs, bool isHomeTeam)
    {
        var team = isHomeTeam ? config.HomeTeam : config.AwayTeam;
        var activeIds = isHomeTeam ? state.HomeActivePlayerIds : state.AwayActivePlayerIds;

        foreach (var sub in subs)
        {
            int subsUsed = isHomeTeam ? state.HomeSubstitutionsUsed : state.AwaySubstitutionsUsed;
            if (subsUsed >= 3) break;

            int outIdx = activeIds.IndexOf(sub.PlayerOutId);
            if (outIdx < 0) continue;

            var incoming = team.Players.FirstOrDefault(p => p.Id == sub.PlayerInId);
            if (incoming == null) continue;

            // Swap in active lineup
            activeIds[outIdx] = sub.PlayerInId;

            // Initialize stamina and rating for incoming player
            state.PlayerStamina[sub.PlayerInId] = incoming.Attributes.Stamina;
            state.PlayerRatings[sub.PlayerInId] = RatingCalculator.BaseRating;

            if (isHomeTeam)
                state.HomeSubstitutionsUsed++;
            else
                state.AwaySubstitutionsUsed++;
        }
    }
}
