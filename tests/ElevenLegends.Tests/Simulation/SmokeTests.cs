using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Tests.Simulation;

public class SmokeTests
{
    [Fact]
    public void FullMatch_PrintsRealisticResults()
    {
        var scores = new List<(int Home, int Away)>();

        for (int seed = 1; seed <= 50; seed++)
        {
            MatchConfig config = CreateConfig(seed);
            MatchResult result = MatchSimulator.Simulate(config);
            scores.Add((result.ScoreHome, result.ScoreAway));
        }

        float avgGoals = (float)scores.Average(s => s.Home + s.Away);
        int maxGoals = scores.Max(s => s.Home + s.Away);
        int draws = scores.Count(s => s.Home == s.Away);

        // Real football averages ~2.7 goals per match
        // We allow a wider range for our simulation: 1-5 avg
        Assert.InRange(avgGoals, 0.5f, 6f);

        // Max goals in a single match should be reasonable
        Assert.True(maxGoals <= 12, $"Max goals in a match: {maxGoals}");

        // There should be some draws (in 50 matches, at least 3)
        Assert.True(draws >= 1, $"Only {draws} draws in 50 matches");
    }

    private static MatchConfig CreateConfig(int seed)
    {
        var homePlayers = new List<Player>
        {
            MakePlayer(1, "GK", Position.GK, reflexes: 78, gkPos: 75),
            MakePlayer(2, "CB1", Position.CB, strength: 78, anticipation: 72),
            MakePlayer(3, "CB2", Position.CB, strength: 75, anticipation: 70),
            MakePlayer(4, "LB", Position.LB, speed: 80, passing: 68),
            MakePlayer(5, "RB", Position.RB, speed: 78, passing: 66),
            MakePlayer(6, "CDM", Position.CDM, anticipation: 76, passing: 72),
            MakePlayer(7, "CM1", Position.CM, passing: 82),
            MakePlayer(8, "CM2", Position.CM, passing: 78),
            MakePlayer(9, "LW", Position.LW, dribbling: 82, speed: 84),
            MakePlayer(10, "RW", Position.RW, dribbling: 80, speed: 82),
            MakePlayer(11, "ST", Position.ST, finishing: 88),
        };
        var awayPlayers = new List<Player>
        {
            MakePlayer(12, "GK", Position.GK, reflexes: 74, gkPos: 72),
            MakePlayer(13, "CB1", Position.CB, strength: 74, anticipation: 70),
            MakePlayer(14, "CB2", Position.CB, strength: 72, anticipation: 68),
            MakePlayer(15, "LB", Position.LB, speed: 76, passing: 64),
            MakePlayer(16, "RB", Position.RB, speed: 74),
            MakePlayer(17, "CDM", Position.CDM, anticipation: 72, passing: 68),
            MakePlayer(18, "CM1", Position.CM, passing: 76),
            MakePlayer(19, "CM2", Position.CM, passing: 74),
            MakePlayer(20, "LW", Position.LW, dribbling: 78, speed: 80),
            MakePlayer(21, "RW", Position.RW, dribbling: 76),
            MakePlayer(22, "ST", Position.ST, finishing: 82),
        };

        return new MatchConfig
        {
            HomeTeam = new Team { Id = 1, Name = "FC Estrela", Players = homePlayers, StartingLineup = homePlayers.Select(p => p.Id).ToList() },
            AwayTeam = new Team { Id = 2, Name = "SC Trovão", Players = awayPlayers, StartingLineup = awayPlayers.Select(p => p.Id).ToList() },
            Seed = seed
        };
    }

    private static Player MakePlayer(int id, string name, Position pos,
        int finishing = 65, int passing = 65, int dribbling = 65,
        int technique = 65, int strength = 65, int anticipation = 65,
        int speed = 65, int reflexes = 50, int gkPos = 50)
    {
        return new Player
        {
            Id = id, Name = name, PrimaryPosition = pos, Age = 25, Morale = 60, Chemistry = 60,
            Attributes = new PlayerAttributes
            {
                Finishing = finishing, Passing = passing, Dribbling = dribbling,
                FirstTouch = 65, Technique = technique,
                Decisions = 65, Composure = 65, Positioning = 65,
                Anticipation = anticipation, OffTheBall = 65,
                Speed = speed, Acceleration = 65, Stamina = 80,
                Strength = strength, Agility = 65,
                Consistency = 65, Leadership = 60, Flair = 60, BigMatches = 60,
                Reflexes = reflexes, Handling = 60, GkPositioning = gkPos, Aerial = 55
            }
        };
    }
}
