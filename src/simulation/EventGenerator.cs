using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Generates match events from action results.
/// </summary>
public static class EventGenerator
{
    /// <summary>
    /// Generates zero or more events from an action result.
    /// </summary>
    public static List<MatchEvent> Generate(ActionResult result, int tick, Player? assistProvider = null)
    {
        var events = new List<MatchEvent>();

        if (result.IsGoal)
        {
            events.Add(new MatchEvent
            {
                Tick = tick,
                Type = EventType.Goal,
                PlayerId = result.Executor.Id,
                SecondaryPlayerId = assistProvider?.Id,
                Description = $"GOAL! {result.Executor.Name} scores!",
                RatingImpact = 1.5f
            });

            if (assistProvider != null)
            {
                events.Add(new MatchEvent
                {
                    Tick = tick,
                    Type = EventType.Assist,
                    PlayerId = assistProvider.Id,
                    Description = $"Assist by {assistProvider.Name}",
                    RatingImpact = 1.0f
                });
            }
        }
        else if (result.IsShotOnTarget)
        {
            events.Add(new MatchEvent
            {
                Tick = tick,
                Type = EventType.ShotOnTarget,
                PlayerId = result.Executor.Id,
                Description = $"Shot on target by {result.Executor.Name} — saved!",
                RatingImpact = 0.1f
            });

            // GK save event would be handled separately
        }
        else if (result.Action == ActionType.Shot && !result.Success)
        {
            events.Add(new MatchEvent
            {
                Tick = tick,
                Type = EventType.Shot,
                PlayerId = result.Executor.Id,
                Description = $"Shot off target by {result.Executor.Name}",
                RatingImpact = -0.1f
            });
        }

        if (result.IsFoul)
        {
            events.Add(new MatchEvent
            {
                Tick = tick,
                Type = EventType.Foul,
                PlayerId = result.Executor.Id,
                SecondaryPlayerId = result.Opponent?.Id,
                Description = $"Foul by {result.Executor.Name}",
                RatingImpact = -0.2f
            });
        }

        // Successful defensive actions
        if (result.Success && result.Action == ActionType.Tackle && !result.IsFoul)
        {
            events.Add(new MatchEvent
            {
                Tick = tick,
                Type = EventType.Save, // reusing as "defensive action" for now
                PlayerId = result.Executor.Id,
                Description = $"Great tackle by {result.Executor.Name}",
                RatingImpact = 0.3f
            });
        }

        if (result.Success && result.Action == ActionType.Interception)
        {
            events.Add(new MatchEvent
            {
                Tick = tick,
                Type = EventType.Save,
                PlayerId = result.Executor.Id,
                Description = $"Interception by {result.Executor.Name}",
                RatingImpact = 0.3f
            });
        }

        // Successful dribble
        if (result.Success && result.Action == ActionType.Dribble)
        {
            events.Add(new MatchEvent
            {
                Tick = tick,
                Type = EventType.Shot, // generic positive action event
                PlayerId = result.Executor.Id,
                Description = $"Successful dribble by {result.Executor.Name}",
                RatingImpact = 0.2f
            });
        }

        // Failed pass
        if (!result.Success && result.Action == ActionType.Pass)
        {
            events.Add(new MatchEvent
            {
                Tick = tick,
                Type = EventType.Shot, // generic event
                PlayerId = result.Executor.Id,
                Description = $"Bad pass by {result.Executor.Name}",
                RatingImpact = -0.2f
            });
        }

        // Turnover on failed dribble
        if (!result.Success && result.Action == ActionType.Dribble)
        {
            events.Add(new MatchEvent
            {
                Tick = tick,
                Type = EventType.Shot,
                PlayerId = result.Executor.Id,
                Description = $"Lost possession: {result.Executor.Name}",
                RatingImpact = -0.3f
            });
        }

        return events;
    }
}
