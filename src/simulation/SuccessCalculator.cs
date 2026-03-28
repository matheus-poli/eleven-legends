using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Calculates success chance for a given action.
/// Formula: success_chance = attribute + chemistry_bonus + morale_bonus + trait_bonus + rng_value
/// </summary>
public static class SuccessCalculator
{
    /// <summary>
    /// Calculates the total success chance and compares against a threshold.
    /// </summary>
    /// <param name="player">The player performing the action.</param>
    /// <param name="action">The type of action being attempted.</param>
    /// <param name="assignedPosition">The position the player is currently playing.</param>
    /// <param name="rng">Injected RNG for determinism.</param>
    /// <returns>True if the action succeeds, false otherwise.</returns>
    public static bool Calculate(
        Player player, ActionType action, Position assignedPosition, IRng rng,
        float bonusModifier = 0f)
    {
        float successChance = CalculateRaw(player, action, assignedPosition, rng) + bonusModifier;
        float threshold = GetThreshold(action);
        return successChance >= threshold;
    }

    /// <summary>
    /// Returns the raw success value (before threshold comparison).
    /// Useful for testing and debugging.
    /// </summary>
    public static float CalculateRaw(Player player, ActionType action, Position assignedPosition, IRng rng)
    {
        int attribute = GetPrimaryAttribute(player.Attributes, action);
        float chemistryBonus = GetChemistryBonus(player.Chemistry);
        float moraleBonus = GetMoraleBonus(player.Morale);
        float traitBonus = GetTraitBonus(player.Traits, action);
        float rngValue = rng.NextFloat(-15f, 15f);
        float positionPenalty = GetPositionPenalty(player.PrimaryPosition, player.SecondaryPosition, assignedPosition);

        return (attribute * (1f - positionPenalty)) + chemistryBonus + moraleBonus + traitBonus + rngValue;
    }

    /// <summary>
    /// Maps an action to its primary attribute.
    /// </summary>
    public static int GetPrimaryAttribute(PlayerAttributes attributes, ActionType action)
    {
        return action switch
        {
            ActionType.Pass => attributes.Passing,
            ActionType.Dribble => attributes.Dribbling,
            ActionType.Shot => attributes.Finishing,
            ActionType.Cross => attributes.Technique,
            ActionType.Tackle => attributes.Strength,
            ActionType.Interception => attributes.Anticipation,
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };
    }

    /// <summary>
    /// Chemistry bonus: 0–20 mapped from chemistry 0–100.
    /// </summary>
    public static float GetChemistryBonus(int chemistry)
    {
        return chemistry / 100f * 20f;
    }

    /// <summary>
    /// Morale bonus: -10 to +10 mapped from morale 0–100 (50 = neutral).
    /// </summary>
    public static float GetMoraleBonus(int morale)
    {
        return (morale - 50f) / 50f * 10f;
    }

    /// <summary>
    /// Trait bonus: checks if any of the player's traits match the action context.
    /// Returns 0–15.
    /// </summary>
    public static float GetTraitBonus(IReadOnlyList<string> traits, ActionType action)
    {
        float bonus = 0f;

        foreach (string trait in traits)
        {
            bonus += GetSingleTraitBonus(trait, action);
        }

        return Math.Min(bonus, 15f);
    }

    /// <summary>
    /// Position penalty based on how far the assigned position is from the player's natural positions.
    /// Primary: 0%, Secondary: 10%, Out of position: 35%.
    /// </summary>
    public static float GetPositionPenalty(Position primary, Position? secondary, Position assigned)
    {
        if (assigned == primary) return 0f;
        if (secondary.HasValue && assigned == secondary.Value) return 0.10f;
        if (ArePositionsRelated(primary, assigned)) return 0.20f;
        return 0.35f;
    }

    /// <summary>
    /// Thresholds vary by action — shots are hardest, passes are easiest.
    /// </summary>
    public static float GetThreshold(ActionType action)
    {
        return action switch
        {
            ActionType.Pass => 40f,
            ActionType.Dribble => 55f,
            ActionType.Shot => 85f,
            ActionType.Cross => 50f,
            ActionType.Tackle => 45f,
            ActionType.Interception => 45f,
            _ => 50f
        };
    }

    private static float GetSingleTraitBonus(string trait, ActionType action)
    {
        return (trait, action) switch
        {
            ("Finesse Shot", ActionType.Shot) => 8f,
            ("Power Shot", ActionType.Shot) => 10f,
            ("Close Control", ActionType.Dribble) => 8f,
            ("Through Pass", ActionType.Pass) => 8f,
            ("Interceptor", ActionType.Interception) => 10f,
            ("Aerial Dominance", ActionType.Tackle) => 5f,
            ("Clinical Finisher", ActionType.Shot) => 12f,
            ("Playmaker", ActionType.Pass) => 7f,
            ("Hard Tackler", ActionType.Tackle) => 8f,
            ("Whipped Crosses", ActionType.Cross) => 8f,
            _ => 0f
        };
    }

    /// <summary>
    /// Determines if two positions are in the same general area (adapted position = -20%).
    /// </summary>
    private static bool ArePositionsRelated(Position a, Position b)
    {
        return GetPositionGroup(a) == GetPositionGroup(b);
    }

    private static int GetPositionGroup(Position position)
    {
        return position switch
        {
            Position.GK => 0,
            Position.CB or Position.LB or Position.RB or Position.LWB or Position.RWB => 1,
            Position.CDM or Position.CM or Position.CAM or Position.LM or Position.RM => 2,
            Position.LW or Position.RW or Position.CF or Position.ST => 3,
            _ => -1
        };
    }
}
