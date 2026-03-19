using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Tests.Simulation;

/// <summary>
/// Tests that the simulation is fully deterministic when given the same seed.
/// This is critical for replays, testing, and debugging.
/// </summary>
public class DeterminismTests
{
    private static MatchConfig CreateConfig(int seed)
    {
        var homePlayers = Enumerable.Range(1, 11).Select(i =>
            new Player
            {
                Id = i,
                Name = $"Home {i}",
                PrimaryPosition = i == 1 ? Position.GK : (i <= 5 ? Position.CB : (i <= 8 ? Position.CM : Position.ST)),
                Age = 25,
                Morale = 60,
                Chemistry = 55,
                Attributes = new PlayerAttributes
                {
                    Finishing = 60 + i, Passing = 65 + i, Dribbling = 60 + i,
                    FirstTouch = 65, Technique = 65,
                    Decisions = 65, Composure = 65, Positioning = 65,
                    Anticipation = 65, OffTheBall = 65,
                    Speed = 65, Acceleration = 65, Stamina = 78,
                    Strength = 65, Agility = 65,
                    Consistency = 65, Leadership = 60, Flair = 60, BigMatches = 60,
                    Reflexes = i == 1 ? 75 : 40, Handling = i == 1 ? 70 : 30,
                    GkPositioning = i == 1 ? 72 : 30, Aerial = 55
                }
            }).ToList();

        var awayPlayers = Enumerable.Range(12, 11).Select(i =>
            new Player
            {
                Id = i,
                Name = $"Away {i}",
                PrimaryPosition = i == 12 ? Position.GK : (i <= 16 ? Position.CB : (i <= 19 ? Position.CM : Position.ST)),
                Age = 25,
                Morale = 60,
                Chemistry = 55,
                Attributes = new PlayerAttributes
                {
                    Finishing = 58 + (i - 11), Passing = 63 + (i - 11), Dribbling = 58 + (i - 11),
                    FirstTouch = 63, Technique = 63,
                    Decisions = 63, Composure = 63, Positioning = 63,
                    Anticipation = 63, OffTheBall = 63,
                    Speed = 63, Acceleration = 63, Stamina = 76,
                    Strength = 63, Agility = 63,
                    Consistency = 63, Leadership = 58, Flair = 58, BigMatches = 58,
                    Reflexes = i == 12 ? 73 : 38, Handling = i == 12 ? 68 : 28,
                    GkPositioning = i == 12 ? 70 : 28, Aerial = 53
                }
            }).ToList();

        return new MatchConfig
        {
            HomeTeam = new Team
            {
                Id = 1, Name = "FC Deterministic Home",
                Players = homePlayers,
                StartingLineup = homePlayers.Select(p => p.Id).ToList()
            },
            AwayTeam = new Team
            {
                Id = 2, Name = "FC Deterministic Away",
                Players = awayPlayers,
                StartingLineup = awayPlayers.Select(p => p.Id).ToList()
            },
            Seed = seed
        };
    }

    [Fact]
    public void SameSeed_ProducesSameScore()
    {
        MatchConfig config1 = CreateConfig(12345);
        MatchConfig config2 = CreateConfig(12345);

        MatchResult result1 = MatchSimulator.Simulate(config1);
        MatchResult result2 = MatchSimulator.Simulate(config2);

        Assert.Equal(result1.ScoreHome, result2.ScoreHome);
        Assert.Equal(result1.ScoreAway, result2.ScoreAway);
    }

    [Fact]
    public void SameSeed_ProducesSameEvents()
    {
        MatchConfig config1 = CreateConfig(99999);
        MatchConfig config2 = CreateConfig(99999);

        MatchResult result1 = MatchSimulator.Simulate(config1);
        MatchResult result2 = MatchSimulator.Simulate(config2);

        Assert.Equal(result1.Events.Count, result2.Events.Count);
        for (int i = 0; i < result1.Events.Count; i++)
        {
            Assert.Equal(result1.Events[i].Tick, result2.Events[i].Tick);
            Assert.Equal(result1.Events[i].Type, result2.Events[i].Type);
            Assert.Equal(result1.Events[i].PlayerId, result2.Events[i].PlayerId);
        }
    }

    [Fact]
    public void SameSeed_ProducesSameRatings()
    {
        MatchConfig config1 = CreateConfig(77777);
        MatchConfig config2 = CreateConfig(77777);

        MatchResult result1 = MatchSimulator.Simulate(config1);
        MatchResult result2 = MatchSimulator.Simulate(config2);

        foreach (var kvp in result1.FinalState.PlayerRatings)
        {
            Assert.Equal(kvp.Value, result2.FinalState.PlayerRatings[kvp.Key]);
        }
    }

    [Fact]
    public void SameSeed_ProducesSameMvp()
    {
        MatchConfig config1 = CreateConfig(55555);
        MatchConfig config2 = CreateConfig(55555);

        MatchResult result1 = MatchSimulator.Simulate(config1);
        MatchResult result2 = MatchSimulator.Simulate(config2);

        Assert.Equal(result1.MvpPlayerId, result2.MvpPlayerId);
        Assert.Equal(result1.SvpPlayerId, result2.SvpPlayerId);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentResults()
    {
        MatchConfig config1 = CreateConfig(111);
        MatchConfig config2 = CreateConfig(222);

        MatchResult result1 = MatchSimulator.Simulate(config1);
        MatchResult result2 = MatchSimulator.Simulate(config2);

        // With different seeds, at least something should differ
        // (scores, events, or ratings — extremely unlikely to be identical)
        bool scoresIdentical = result1.ScoreHome == result2.ScoreHome && result1.ScoreAway == result2.ScoreAway;
        bool eventsCountIdentical = result1.Events.Count == result2.Events.Count;

        // At least one of these should differ
        Assert.False(scoresIdentical && eventsCountIdentical,
            "Different seeds produced identical results — extremely unlikely");
    }
}
