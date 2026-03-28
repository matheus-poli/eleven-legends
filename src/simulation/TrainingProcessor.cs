using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Simulation;

/// <summary>
/// Processes training day choices and generates player events.
/// Inspired by Uma Musume / Inazuma Eleven training mechanics.
/// </summary>
public static class TrainingProcessor
{
    private static readonly TrainingChoice[] AllChoices =
    [
        new() { Name = "Intense Drills", Description = "Push the squad hard. Big gains but risk of morale loss.", Type = TrainingType.IntenseDrills },
        new() { Name = "Tactical Session", Description = "Work on team play. Builds chemistry between players.", Type = TrainingType.TacticalSession },
        new() { Name = "Light Training", Description = "Easy session. Small morale boost, no risks.", Type = TrainingType.LightTraining },
        new() { Name = "Rest Day", Description = "Let the squad recover. Stressed players recover morale.", Type = TrainingType.RestDay },
        new() { Name = "Youth Showcase", Description = "Give reserves the spotlight. Potential breakthroughs.", Type = TrainingType.YouthFocus },
    ];

    /// <summary>
    /// Generates 3 random training choices for the manager to pick from.
    /// </summary>
    public static IReadOnlyList<TrainingChoice> GenerateChoices(IRng rng)
    {
        var shuffled = AllChoices.OrderBy(_ => rng.NextInt(0, 1000)).ToList();
        return shuffled.Take(3).ToList();
    }

    /// <summary>
    /// Processes the chosen training and returns events.
    /// Modifies the team's players in-place (morale, chemistry).
    /// </summary>
    public static TrainingResult ProcessTraining(
        TrainingChoice choice, Club club, IRng rng)
    {
        var events = new List<TrainingPlayerEvent>();
        Team team = club.Team;
        var starterSet = new HashSet<int>(team.StartingLineup);

        switch (choice.Type)
        {
            case TrainingType.IntenseDrills:
                events.AddRange(ProcessIntenseDrills(team, starterSet, rng));
                break;
            case TrainingType.TacticalSession:
                events.AddRange(ProcessTacticalSession(team, starterSet, rng));
                break;
            case TrainingType.LightTraining:
                events.AddRange(ProcessLightTraining(team, rng));
                break;
            case TrainingType.RestDay:
                events.AddRange(ProcessRestDay(team, rng));
                break;
            case TrainingType.YouthFocus:
                events.AddRange(ProcessYouthFocus(team, starterSet, rng));
                break;
        }

        // Apply morale/chemistry changes
        var updatedPlayers = team.Players.Select(p =>
        {
            int moraleDelta = events.Where(e => e.PlayerId == p.Id).Sum(e => e.MoraleDelta);
            int chemDelta = events.Where(e => e.PlayerId == p.Id).Sum(e => e.ChemistryDelta);

            if (moraleDelta == 0 && chemDelta == 0) return p;

            return p with
            {
                Morale = Math.Clamp(p.Morale + moraleDelta, 0, 100),
                Chemistry = Math.Clamp(p.Chemistry + chemDelta, 0, 100),
            };
        }).ToList();

        club.Team = team with { Players = updatedPlayers };

        return new TrainingResult { Choice = choice, Events = events };
    }

    private static List<TrainingPlayerEvent> ProcessIntenseDrills(
        Team team, HashSet<int> starters, IRng rng)
    {
        var events = new List<TrainingPlayerEvent>();

        foreach (Player p in team.Players)
        {
            bool isStarter = starters.Contains(p.Id);
            int roll = rng.NextInt(0, 100);

            if (isStarter)
            {
                if (roll < 60) // 60% positive
                {
                    events.Add(new TrainingPlayerEvent
                    {
                        PlayerId = p.Id, PlayerName = p.Name,
                        Description = $"{p.Name} had an excellent training session!",
                        MoraleDelta = rng.NextInt(3, 6), IsPositive = true,
                    });
                }
                else if (roll < 85) // 25% neutral
                {
                    events.Add(new TrainingPlayerEvent
                    {
                        PlayerId = p.Id, PlayerName = p.Name,
                        Description = $"{p.Name} trained solidly.",
                        MoraleDelta = 1, IsPositive = true,
                    });
                }
                else // 15% negative — overtraining
                {
                    events.Add(new TrainingPlayerEvent
                    {
                        PlayerId = p.Id, PlayerName = p.Name,
                        Description = $"{p.Name} is exhausted from intense training.",
                        MoraleDelta = rng.NextInt(-5, -2), IsPositive = false,
                    });
                }
            }
            else
            {
                // Reserves also train but with less drama
                if (roll < 40)
                {
                    events.Add(new TrainingPlayerEvent
                    {
                        PlayerId = p.Id, PlayerName = p.Name,
                        Description = $"{p.Name} showed determination in training.",
                        MoraleDelta = rng.NextInt(1, 3), IsPositive = true,
                    });
                }
            }
        }

        // Random team event
        if (rng.NextInt(0, 100) < 30)
        {
            Player random = team.Players[rng.NextInt(0, team.Players.Count - 1)];
            events.Add(new TrainingPlayerEvent
            {
                PlayerId = random.Id, PlayerName = random.Name,
                Description = $"{random.Name} had a breakthrough moment in finishing drills!",
                MoraleDelta = 5, IsPositive = true,
            });
        }

        return events;
    }

    private static List<TrainingPlayerEvent> ProcessTacticalSession(
        Team team, HashSet<int> starters, IRng rng)
    {
        var events = new List<TrainingPlayerEvent>();

        // Chemistry boost for starters
        foreach (Player p in team.Players.Where(pl => starters.Contains(pl.Id)))
        {
            int chemGain = rng.NextInt(2, 5);
            events.Add(new TrainingPlayerEvent
            {
                PlayerId = p.Id, PlayerName = p.Name,
                Description = $"{p.Name} improved team understanding.",
                MoraleDelta = 1, ChemistryDelta = chemGain, IsPositive = true,
            });
        }

        // Random pair bonding event
        if (team.Players.Count >= 2 && rng.NextInt(0, 100) < 50)
        {
            Player p1 = team.Players[rng.NextInt(0, team.Players.Count - 1)];
            Player p2 = team.Players[rng.NextInt(0, team.Players.Count - 1)];
            if (p1.Id != p2.Id)
            {
                events.Add(new TrainingPlayerEvent
                {
                    PlayerId = p1.Id, PlayerName = p1.Name,
                    Description = $"{p1.Name} and {p2.Name} developed a great connection!",
                    ChemistryDelta = 4, MoraleDelta = 2, IsPositive = true,
                });
                events.Add(new TrainingPlayerEvent
                {
                    PlayerId = p2.Id, PlayerName = p2.Name,
                    Description = $"{p2.Name} built rapport with {p1.Name}.",
                    ChemistryDelta = 4, MoraleDelta = 2, IsPositive = true,
                });
            }
        }

        return events;
    }

    private static List<TrainingPlayerEvent> ProcessLightTraining(Team team, IRng rng)
    {
        var events = new List<TrainingPlayerEvent>();

        foreach (Player p in team.Players)
        {
            if (rng.NextInt(0, 100) < 70) // 70% get small boost
            {
                events.Add(new TrainingPlayerEvent
                {
                    PlayerId = p.Id, PlayerName = p.Name,
                    Description = $"{p.Name} enjoyed a relaxed session.",
                    MoraleDelta = rng.NextInt(1, 3), IsPositive = true,
                });
            }
        }

        return events;
    }

    private static List<TrainingPlayerEvent> ProcessRestDay(Team team, IRng rng)
    {
        var events = new List<TrainingPlayerEvent>();

        foreach (Player p in team.Players)
        {
            if (p.Morale < 50) // Low morale players benefit most
            {
                int recovery = rng.NextInt(5, 10);
                events.Add(new TrainingPlayerEvent
                {
                    PlayerId = p.Id, PlayerName = p.Name,
                    Description = $"{p.Name} feels refreshed after rest.",
                    MoraleDelta = recovery, IsPositive = true,
                });
            }
            else if (rng.NextInt(0, 100) < 40)
            {
                events.Add(new TrainingPlayerEvent
                {
                    PlayerId = p.Id, PlayerName = p.Name,
                    Description = $"{p.Name} recharged mentally.",
                    MoraleDelta = rng.NextInt(1, 3), IsPositive = true,
                });
            }
        }

        // Random off-day event
        if (rng.NextInt(0, 100) < 20)
        {
            Player random = team.Players[rng.NextInt(0, team.Players.Count - 1)];
            events.Add(new TrainingPlayerEvent
            {
                PlayerId = random.Id, PlayerName = random.Name,
                Description = $"The press praised {random.Name}'s recent form!",
                MoraleDelta = 3, IsPositive = true,
            });
        }

        return events;
    }

    private static List<TrainingPlayerEvent> ProcessYouthFocus(
        Team team, HashSet<int> starters, IRng rng)
    {
        var events = new List<TrainingPlayerEvent>();
        var reserves = team.Players.Where(p => !starters.Contains(p.Id)).ToList();

        foreach (Player p in reserves)
        {
            int roll = rng.NextInt(0, 100);

            if (roll < 40) // 40% breakthrough
            {
                events.Add(new TrainingPlayerEvent
                {
                    PlayerId = p.Id, PlayerName = p.Name,
                    Description = $"{p.Name} impressed everyone in training!",
                    MoraleDelta = rng.NextInt(5, 8), IsPositive = true,
                });
            }
            else if (roll < 70)
            {
                events.Add(new TrainingPlayerEvent
                {
                    PlayerId = p.Id, PlayerName = p.Name,
                    Description = $"{p.Name} worked hard to prove themselves.",
                    MoraleDelta = rng.NextInt(2, 4), IsPositive = true,
                });
            }
            else if (roll < 85)
            {
                events.Add(new TrainingPlayerEvent
                {
                    PlayerId = p.Id, PlayerName = p.Name,
                    Description = $"{p.Name} is frustrated about lack of game time.",
                    MoraleDelta = rng.NextInt(-4, -1), IsPositive = false,
                });
            }
        }

        // Starters get a small morale hit (bored by easy session)
        foreach (Player p in team.Players.Where(pl => starters.Contains(pl.Id)))
        {
            if (rng.NextInt(0, 100) < 30)
            {
                events.Add(new TrainingPlayerEvent
                {
                    PlayerId = p.Id, PlayerName = p.Name,
                    Description = $"{p.Name} found the youth session unchallenging.",
                    MoraleDelta = -1, IsPositive = false,
                });
            }
        }

        return events;
    }
}
