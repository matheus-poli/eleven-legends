using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Processes a single tick of a match. Each tick = 1 minute of game time.
/// Follows the simulation loop: possession → action → executor → success → result → event → rating → stamina.
/// </summary>
public static class TickProcessor
{
    private const float StaminaDrainPerTick = 0.8f;

    /// <summary>
    /// Processes one tick and updates the MatchState accordingly.
    /// </summary>
    public static void ProcessTick(MatchState state, MatchConfig config, IRng rng)
    {
        state.CurrentTick++;
        state.TotalTicksPlayed++;

        // 1. Resolve possession
        int possessionTeamId = PossessionResolver.Resolve(state, config, rng);
        state.BallPossessionTeamId = possessionTeamId;

        if (possessionTeamId == config.HomeTeam.Id)
            state.HomePossessionTicks++;

        // Update possession percentage
        if (state.TotalTicksPlayed > 0)
            state.PossessionHome = (float)state.HomePossessionTicks / state.TotalTicksPlayed;

        Team attackingTeam = possessionTeamId == config.HomeTeam.Id ? config.HomeTeam : config.AwayTeam;
        Team defendingTeam = possessionTeamId == config.HomeTeam.Id ? config.AwayTeam : config.HomeTeam;

        // 2. Choose action for the attacking team
        ActionType attackAction = ActionSelector.SelectAction(state.BallZone, hasPossession: true, rng);

        // 3. Select executor
        Player executor = ActionSelector.SelectExecutor(attackingTeam, attackAction, state, rng);

        // 4-5. Calculate success and apply result
        ActionResult attackResult = ActionResolver.ResolveAttack(
            executor, attackAction, executor.PrimaryPosition,
            defendingTeam, state, rng);

        // Track the last successful passer for potential assists
        Player? assistProvider = null;
        if (attackResult.IsGoal && attackAction == ActionType.Shot)
        {
            // Look for recent successful pass/cross in the last few events
            assistProvider = FindAssistProvider(state, attackingTeam, executor.Id);
        }

        // 6. Generate events
        List<MatchEvent> events = EventGenerator.Generate(attackResult, state.CurrentTick, assistProvider);

        // Apply goal to score
        if (attackResult.IsGoal)
        {
            if (possessionTeamId == config.HomeTeam.Id)
                state.ScoreHome++;
            else
                state.ScoreAway++;

            // Kickoff: conceding team gets possession from midfield
            state.BallZone = FieldZone.MidfieldCenter;
            state.BallPossessionTeamId = possessionTeamId == config.HomeTeam.Id
                ? config.AwayTeam.Id : config.HomeTeam.Id;
        }
        else if (attackResult.IsShotOnTarget)
        {
            // Saved shot: defending team gets possession from defense
            state.BallZone = FieldZone.DefenseCenter;
            state.BallPossessionTeamId = defendingTeam.Id;
        }
        else if (!attackResult.Success)
        {
            // Failed action: ball retreats, possible turnover
            state.BallZone = ActionSelector.AdvanceBallZone(state.BallZone, false, rng);
        }
        else
        {
            // Successful non-shot action: advance ball zone
            state.BallZone = ActionSelector.AdvanceBallZone(state.BallZone, true, rng);
        }

        // Also process defensive reaction if the attack failed and no goal/foul
        if (!attackResult.Success && !attackResult.IsGoal && !attackResult.IsFoul)
        {
            ActionType defenseAction = ActionSelector.SelectAction(state.BallZone, hasPossession: false, rng);
            Player defender = ActionSelector.SelectExecutor(defendingTeam, defenseAction, state, rng);
            ActionResult defenseResult = ActionResolver.ResolveDefense(
                defender, defenseAction, defender.PrimaryPosition, executor, rng);

            List<MatchEvent> defenseEvents = EventGenerator.Generate(defenseResult, state.CurrentTick);
            events.AddRange(defenseEvents);

            // Foul from tackle gives attacking team a free kick
            if (defenseResult.IsFoul)
            {
                events.Add(new MatchEvent
                {
                    Tick = state.CurrentTick,
                    Type = EventType.FreeKick,
                    PlayerId = executor.Id,
                    Description = $"Free kick for {attackingTeam.Name}",
                    RatingImpact = 0f
                });
            }
        }

        // 7. Update ratings
        state.Events.AddRange(events);
        RatingCalculator.ApplyEvents(state, events, config);

        // 8. Degrade stamina for all starting players
        DegradeStamina(state, config);
    }

    /// <summary>
    /// Initializes stamina for all starting players based on their Stamina attribute.
    /// </summary>
    public static void InitializeStamina(MatchState state, MatchConfig config)
    {
        foreach (int playerId in config.HomeTeam.StartingLineup)
        {
            Player? player = config.HomeTeam.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
                state.PlayerStamina[playerId] = player.Attributes.Stamina;
        }
        foreach (int playerId in config.AwayTeam.StartingLineup)
        {
            Player? player = config.AwayTeam.Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
                state.PlayerStamina[playerId] = player.Attributes.Stamina;
        }
    }

    private static void DegradeStamina(MatchState state, MatchConfig config)
    {
        var allStarting = config.HomeTeam.StartingLineup
            .Concat(config.AwayTeam.StartingLineup);

        foreach (int playerId in allStarting)
        {
            if (state.PlayerStamina.ContainsKey(playerId))
            {
                state.PlayerStamina[playerId] = Math.Max(0f,
                    state.PlayerStamina[playerId] - StaminaDrainPerTick);
            }
        }
    }

    private static Player? FindAssistProvider(MatchState state, Team attackingTeam, int scorerId)
    {
        // Look at recent events for a successful pass/cross by a teammate
        var recentEvents = state.Events
            .Where(e => e.Tick >= state.CurrentTick - 3)
            .Where(e => e.PlayerId != scorerId)
            .Where(e => e.RatingImpact > 0)
            .LastOrDefault();

        if (recentEvents == null) return null;

        var startingSet = new HashSet<int>(attackingTeam.StartingLineup);
        return attackingTeam.Players
            .FirstOrDefault(p => p.Id == recentEvents.PlayerId && startingSet.Contains(p.Id));
    }
}
