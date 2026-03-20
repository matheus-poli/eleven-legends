using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Selects which action the attacking/defending team will attempt in a tick,
/// and which player will execute it.
/// </summary>
public static class ActionSelector
{
    /// <summary>
    /// Chooses the action based on the current field zone and match context.
    /// </summary>
    public static ActionType SelectAction(FieldZone ballZone, bool hasPossession, IRng rng)
    {
        if (!hasPossession)
        {
            // Defending team attempts tackle or interception
            int roll = rng.NextInt(0, 100);
            return roll < 55 ? ActionType.Tackle : ActionType.Interception;
        }

        // Attacking team actions depend on field zone
        return ballZone switch
        {
            FieldZone.DefenseLeft or FieldZone.DefenseCenter or FieldZone.DefenseRight
                => SelectDefensiveZoneAction(rng),
            FieldZone.MidfieldLeft or FieldZone.MidfieldCenter or FieldZone.MidfieldRight
                => SelectMidfieldZoneAction(rng),
            FieldZone.AttackLeft or FieldZone.AttackCenter or FieldZone.AttackRight
                => SelectAttackZoneAction(ballZone, rng),
            _ => ActionType.Pass
        };
    }

    /// <summary>
    /// Selects the best player to execute the action from the team's starting lineup.
    /// Picks the player with the highest relevant attribute (with some RNG variance).
    /// </summary>
    public static Player SelectExecutor(
        Team team, ActionType action, MatchState state, IRng rng,
        IReadOnlyList<int>? activePlayerIds = null)
    {
        var activeSet = new HashSet<int>(
            activePlayerIds is { Count: > 0 } ? activePlayerIds : team.StartingLineup);
        var candidates = team.Players
            .Where(p => activeSet.Contains(p.Id))
            .Where(p => IsEligibleForAction(p, action, state))
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = team.Players.Where(p => activeSet.Contains(p.Id)).ToList();
        }

        // Weighted selection: higher attribute = higher chance of being selected
        float totalWeight = 0f;
        var weights = new float[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            int attr = SuccessCalculator.GetPrimaryAttribute(candidates[i].Attributes, action);
            float weight = attr + rng.NextFloat(0f, 20f); // some randomness in selection
            weights[i] = Math.Max(weight, 1f);
            totalWeight += weights[i];
        }

        float roll = rng.NextFloat(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return candidates[i];
        }

        return candidates[^1];
    }

    /// <summary>
    /// Advances the ball zone. On success, the ball moves forward with some probability
    /// of staying in the same zone (realistic buildup play). On failure, ball retreats.
    /// </summary>
    public static FieldZone AdvanceBallZone(FieldZone current, bool success, IRng rng)
    {
        if (success)
        {
            // Successful action doesn't always advance — sometimes it maintains position
            int advanceRoll = rng.NextInt(0, 100);

            return current switch
            {
                // From defense: 60% advance, 40% stay
                FieldZone.DefenseLeft => advanceRoll < 60
                    ? ChooseRandom(rng, FieldZone.MidfieldLeft, FieldZone.MidfieldCenter)
                    : current,
                FieldZone.DefenseCenter => advanceRoll < 60
                    ? FieldZone.MidfieldCenter
                    : current,
                FieldZone.DefenseRight => advanceRoll < 60
                    ? ChooseRandom(rng, FieldZone.MidfieldRight, FieldZone.MidfieldCenter)
                    : current,
                // From midfield: 30% advance, 70% stay (realistic buildup)
                FieldZone.MidfieldLeft => advanceRoll < 30
                    ? ChooseRandom(rng, FieldZone.AttackLeft, FieldZone.AttackCenter)
                    : current,
                FieldZone.MidfieldCenter => advanceRoll < 30
                    ? ChooseRandom(rng, FieldZone.AttackLeft, FieldZone.AttackCenter, FieldZone.AttackRight)
                    : current,
                FieldZone.MidfieldRight => advanceRoll < 30
                    ? ChooseRandom(rng, FieldZone.AttackRight, FieldZone.AttackCenter)
                    : current,
                // Already in attack zone — stay
                _ => current
            };
        }
        else
        {
            // Failed action: ball retreats
            return current switch
            {
                FieldZone.AttackLeft => ChooseRandom(rng, FieldZone.MidfieldLeft, FieldZone.MidfieldCenter),
                FieldZone.AttackCenter => FieldZone.MidfieldCenter,
                FieldZone.AttackRight => ChooseRandom(rng, FieldZone.MidfieldRight, FieldZone.MidfieldCenter),
                FieldZone.MidfieldLeft => ChooseRandom(rng, FieldZone.DefenseLeft, FieldZone.DefenseCenter),
                FieldZone.MidfieldCenter => FieldZone.DefenseCenter,
                FieldZone.MidfieldRight => ChooseRandom(rng, FieldZone.DefenseRight, FieldZone.DefenseCenter),
                // Already in defense zone — stay
                _ => current
            };
        }
    }

    private static ActionType SelectDefensiveZoneAction(IRng rng)
    {
        int roll = rng.NextInt(0, 100);
        return roll < 80 ? ActionType.Pass : ActionType.Dribble;
    }

    private static ActionType SelectMidfieldZoneAction(IRng rng)
    {
        int roll = rng.NextInt(0, 100);
        if (roll < 60) return ActionType.Pass;
        if (roll < 80) return ActionType.Dribble;
        if (roll < 95) return ActionType.Cross;
        return ActionType.Shot; // long shot — rare
    }

    private static ActionType SelectAttackZoneAction(FieldZone zone, IRng rng)
    {
        int roll = rng.NextInt(0, 100);

        // Wing zones favor crosses
        if (zone is FieldZone.AttackLeft or FieldZone.AttackRight)
        {
            if (roll < 40) return ActionType.Cross;
            if (roll < 55) return ActionType.Pass;
            if (roll < 75) return ActionType.Dribble;
            return ActionType.Shot;
        }

        // Center zone: balanced with moderate shot chance
        if (roll < 30) return ActionType.Shot;
        if (roll < 55) return ActionType.Pass;
        if (roll < 80) return ActionType.Dribble;
        return ActionType.Cross;
    }

    private static bool IsEligibleForAction(Player player, ActionType action, MatchState state)
    {
        // GK shouldn't be selected for shots (unless extreme situation)
        if (player.PrimaryPosition == Position.GK && action is ActionType.Shot or ActionType.Dribble)
            return false;

        // Defenders shouldn't shoot unless in attack zone
        if (player.PrimaryPosition is Position.CB or Position.LB or Position.RB
            && action == ActionType.Shot
            && state.BallZone is not (FieldZone.AttackLeft or FieldZone.AttackCenter or FieldZone.AttackRight))
            return false;

        return true;
    }

    private static FieldZone ChooseRandom(IRng rng, params FieldZone[] options)
    {
        int index = rng.NextInt(0, options.Length - 1);
        return options[index];
    }
}
