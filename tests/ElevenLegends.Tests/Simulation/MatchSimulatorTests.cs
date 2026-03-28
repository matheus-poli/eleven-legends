using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Tests.Simulation;

public class MatchSimulatorTests
{
    private static MatchConfig CreateRealisticConfig(int seed = 42)
    {
        var homePlayers = new List<Player>
        {
            MakePlayer(1, "GK Home", Position.GK, finishing: 20, reflexes: 78, gkPos: 75),
            MakePlayer(2, "CB Home 1", Position.CB, strength: 78, anticipation: 72),
            MakePlayer(3, "CB Home 2", Position.CB, strength: 75, anticipation: 70),
            MakePlayer(4, "LB Home", Position.LB, speed: 80, passing: 68),
            MakePlayer(5, "RB Home", Position.RB, speed: 78, passing: 66),
            MakePlayer(6, "CDM Home", Position.CDM, anticipation: 76, passing: 72),
            MakePlayer(7, "CM Home 1", Position.CM, passing: 82, decisions: 78),
            MakePlayer(8, "CM Home 2", Position.CM, passing: 78, technique: 76),
            MakePlayer(9, "LW Home", Position.LW, dribbling: 82, speed: 84),
            MakePlayer(10, "RW Home", Position.RW, dribbling: 80, speed: 82),
            MakePlayer(11, "ST Home", Position.ST, finishing: 88, composure: 80),
        };

        var awayPlayers = new List<Player>
        {
            MakePlayer(12, "GK Away", Position.GK, finishing: 18, reflexes: 74, gkPos: 72),
            MakePlayer(13, "CB Away 1", Position.CB, strength: 74, anticipation: 70),
            MakePlayer(14, "CB Away 2", Position.CB, strength: 72, anticipation: 68),
            MakePlayer(15, "LB Away", Position.LB, speed: 76, passing: 64),
            MakePlayer(16, "RB Away", Position.RB, speed: 74, passing: 62),
            MakePlayer(17, "CDM Away", Position.CDM, anticipation: 72, passing: 68),
            MakePlayer(18, "CM Away 1", Position.CM, passing: 76, decisions: 74),
            MakePlayer(19, "CM Away 2", Position.CM, passing: 74, technique: 72),
            MakePlayer(20, "LW Away", Position.LW, dribbling: 78, speed: 80),
            MakePlayer(21, "RW Away", Position.RW, dribbling: 76, speed: 78),
            MakePlayer(22, "ST Away", Position.ST, finishing: 82, composure: 76),
        };

        return new MatchConfig
        {
            HomeTeam = new Team
            {
                Id = 1, Name = "FC Home",
                Players = homePlayers,
                StartingLineup = homePlayers.Select(p => p.Id).ToList()
            },
            AwayTeam = new Team
            {
                Id = 2, Name = "FC Away",
                Players = awayPlayers,
                StartingLineup = awayPlayers.Select(p => p.Id).ToList()
            },
            Seed = seed
        };
    }

    [Fact]
    public void Simulate_ProducesCompleteResult()
    {
        MatchConfig config = CreateRealisticConfig();
        MatchResult result = MatchSimulator.Simulate(config);

        Assert.NotNull(result);
        Assert.NotNull(result.FinalState);
        Assert.NotNull(result.Events);
        Assert.Equal(MatchPhase.Finished, result.FinalState.Phase);
        Assert.Equal(MatchSimulator.TotalTicks, result.FinalState.CurrentTick);
    }

    [Fact]
    public void Simulate_ScoreIsReasonable()
    {
        // Run several matches to verify scores are reasonable
        for (int seed = 1; seed <= 20; seed++)
        {
            MatchConfig config = CreateRealisticConfig(seed);
            MatchResult result = MatchSimulator.Simulate(config);

            // Scores should be 0–10 (rarely more)
            Assert.InRange(result.ScoreHome, 0, 12);
            Assert.InRange(result.ScoreAway, 0, 12);
        }
    }

    [Fact]
    public void Simulate_GeneratesMultipleEvents()
    {
        MatchConfig config = CreateRealisticConfig();
        MatchResult result = MatchSimulator.Simulate(config);

        // A 90-tick match should generate many events
        Assert.True(result.Events.Count > 20,
            $"Expected > 20 events, got {result.Events.Count}");
    }

    [Fact]
    public void Simulate_RatingsAreWithinBounds()
    {
        MatchConfig config = CreateRealisticConfig();
        MatchResult result = MatchSimulator.Simulate(config);

        Assert.All(result.FinalState.PlayerRatings.Values, rating =>
        {
            Assert.InRange(rating, RatingCalculator.MinRating, RatingCalculator.MaxRating);
        });
    }

    [Fact]
    public void Simulate_MvpAndSvpAreValid()
    {
        MatchConfig config = CreateRealisticConfig();
        MatchResult result = MatchSimulator.Simulate(config);

        Assert.True(result.MvpPlayerId > 0);
        Assert.True(result.SvpPlayerId > 0);
        Assert.NotEqual(result.MvpPlayerId, result.SvpPlayerId);

        // MVP should have highest or equal-highest rating
        float mvpRating = result.FinalState.PlayerRatings[result.MvpPlayerId];
        float svpRating = result.FinalState.PlayerRatings[result.SvpPlayerId];
        Assert.True(mvpRating >= svpRating);
    }

    [Fact]
    public void Simulate_PossessionIsReasonable()
    {
        MatchConfig config = CreateRealisticConfig();
        MatchResult result = MatchSimulator.Simulate(config);

        // Possession should be somewhere reasonable (not 100% one-sided)
        Assert.InRange(result.FinalState.PossessionHome, 0.15f, 0.85f);
    }

    [Fact]
    public void Simulate_StaminaDegrades()
    {
        MatchConfig config = CreateRealisticConfig();
        MatchResult result = MatchSimulator.Simulate(config);

        // After 90 ticks, stamina should have degraded significantly
        Assert.All(result.FinalState.PlayerStamina.Values, stamina =>
        {
            Assert.True(stamina < 75, $"Expected stamina < 75 after full match, got {stamina}");
        });
    }

    [Fact]
    public void Simulate_GoalEventsMatchScore()
    {
        MatchConfig config = CreateRealisticConfig();
        MatchResult result = MatchSimulator.Simulate(config);

        int goalEvents = result.Events.Count(e => e.Type == EventType.Goal);
        int totalScore = result.ScoreHome + result.ScoreAway;

        Assert.Equal(totalScore, goalEvents);
    }

    private static Player MakePlayer(int id, string name, Position pos,
        int finishing = 65, int passing = 65, int dribbling = 65,
        int technique = 65, int strength = 65, int anticipation = 65,
        int speed = 65, int reflexes = 50, int gkPos = 50,
        int decisions = 65, int composure = 65)
    {
        return new Player
        {
            Id = id,
            Name = name,
            PrimaryPosition = pos,
            Age = 25,
            Morale = 60,
            Chemistry = 60,
            Attributes = new PlayerAttributes
            {
                Finishing = finishing, Passing = passing, Dribbling = dribbling,
                FirstTouch = 65, Technique = technique,
                Decisions = decisions, Composure = composure, Positioning = 65,
                Anticipation = anticipation, OffTheBall = 65,
                Speed = speed, Acceleration = 65, Stamina = 80,
                Strength = strength, Agility = 65,
                Consistency = 65, Leadership = 60, Flair = 60, BigMatches = 60,
                Reflexes = reflexes, Handling = 60, GkPositioning = gkPos, Aerial = 55
            }
        };
    }
}
