using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Result of resolving an action in a tick.
/// </summary>
public sealed record ActionResult
{
    public required ActionType Action { get; init; }
    public required Player Executor { get; init; }
    public required bool Success { get; init; }

    /// <summary>The defending player involved (for tackles/interceptions).</summary>
    public Player? Opponent { get; init; }

    /// <summary>Whether this action resulted in a goal.</summary>
    public bool IsGoal { get; init; }

    /// <summary>Whether this was a shot on target (but not necessarily a goal).</summary>
    public bool IsShotOnTarget { get; init; }

    /// <summary>Whether this action resulted in a foul.</summary>
    public bool IsFoul { get; init; }
}

/// <summary>
/// Resolves the full outcome of an action, including secondary effects
/// (goals, fouls, shots on target).
/// </summary>
public static class ActionResolver
{
    /// <summary>
    /// Resolves a tick action for the attacking team.
    /// </summary>
    public static ActionResult ResolveAttack(
        Player executor, ActionType action, Position assignedPosition,
        Team defendingTeam, MatchState state, IRng rng,
        float bonusModifier = 0f, IReadOnlyList<int>? defenseActiveIds = null)
    {
        bool success = SuccessCalculator.Calculate(executor, action, assignedPosition, rng, bonusModifier);

        if (action == ActionType.Shot)
            return ResolveShotAction(executor, assignedPosition, success, defendingTeam, state, rng, defenseActiveIds);

        return new ActionResult
        {
            Action = action,
            Executor = executor,
            Success = success
        };
    }

    /// <summary>
    /// Resolves a tick action for the defending team (tackle/interception).
    /// </summary>
    public static ActionResult ResolveDefense(
        Player executor, ActionType action, Position assignedPosition,
        Player attacker, IRng rng)
    {
        bool success = SuccessCalculator.Calculate(executor, action, assignedPosition, rng);
        bool isFoul = !success && action == ActionType.Tackle && rng.NextInt(0, 100) < 30;

        return new ActionResult
        {
            Action = action,
            Executor = executor,
            Success = success,
            Opponent = attacker,
            IsFoul = isFoul
        };
    }

    private static ActionResult ResolveShotAction(
        Player executor, Position assignedPosition, bool shotSuccess,
        Team defendingTeam, MatchState state, IRng rng,
        IReadOnlyList<int>? defenseActiveIds = null)
    {
        if (!shotSuccess)
        {
            return new ActionResult
            {
                Action = ActionType.Shot,
                Executor = executor,
                Success = false,
                IsShotOnTarget = false
            };
        }

        // Shot was on target — does the goalkeeper save it?
        bool isSaved = TryGoalkeeperSave(defendingTeam, state, rng, defenseActiveIds);

        return new ActionResult
        {
            Action = ActionType.Shot,
            Executor = executor,
            Success = !isSaved,
            IsShotOnTarget = true,
            IsGoal = !isSaved
        };
    }

    private static bool TryGoalkeeperSave(
        Team defendingTeam, MatchState state, IRng rng,
        IReadOnlyList<int>? activeIds = null)
    {
        var playerIds = activeIds ?? defendingTeam.StartingLineup;
        var activeSet = new HashSet<int>(playerIds);
        Player? goalkeeper = defendingTeam.Players
            .FirstOrDefault(p => activeSet.Contains(p.Id) && p.PrimaryPosition == Position.GK);

        if (goalkeeper == null)
            return false; // no GK = always concedes

        float saveChance = (goalkeeper.Attributes.Reflexes + goalkeeper.Attributes.GkPositioning) / 2f;
        float staminaFactor = state.PlayerStamina.TryGetValue(goalkeeper.Id, out float stam)
            ? stam / 100f
            : 1f;
        saveChance *= staminaFactor;

        float rngValue = rng.NextFloat(-10f, 10f);
        float threshold = 40f; // GK needs to beat this to save — favors saves for realism

        return (saveChance + rngValue) >= threshold;
    }
}
