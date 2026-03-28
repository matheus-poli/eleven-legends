using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Tests.Simulation;

public class RatingCalculatorTests
{
    private static Player MakePlayer(int id, string name, Position pos, int overall = 70)
    {
        return new Player
        {
            Id = id,
            Name = name,
            PrimaryPosition = pos,
            Attributes = new PlayerAttributes
            {
                Passing = overall, Finishing = overall, Dribbling = overall,
                FirstTouch = overall, Technique = overall,
                Decisions = overall, Composure = overall, Positioning = overall,
                Anticipation = overall, OffTheBall = overall,
                Speed = overall, Acceleration = overall, Stamina = overall,
                Strength = overall, Agility = overall,
                Consistency = overall, Leadership = overall, Flair = overall, BigMatches = overall,
                Reflexes = overall, Handling = overall, GkPositioning = overall, Aerial = overall
            }
        };
    }

    private static (MatchState state, MatchConfig config) SetupMatch()
    {
        var homePlayers = Enumerable.Range(1, 11)
            .Select(i => MakePlayer(i, $"Home {i}", i == 1 ? Position.GK : Position.CM))
            .ToList();
        var awayPlayers = Enumerable.Range(12, 11)
            .Select(i => MakePlayer(i, $"Away {i}", i == 12 ? Position.GK : Position.CM))
            .ToList();

        var home = new Team
        {
            Id = 1, Name = "Home",
            Players = homePlayers,
            StartingLineup = homePlayers.Select(p => p.Id).ToList()
        };
        var away = new Team
        {
            Id = 2, Name = "Away",
            Players = awayPlayers,
            StartingLineup = awayPlayers.Select(p => p.Id).ToList()
        };

        var config = new MatchConfig { HomeTeam = home, AwayTeam = away, Seed = 42 };
        var state = new MatchState();
        RatingCalculator.InitializeRatings(state, config);

        return (state, config);
    }

    [Fact]
    public void InitializeRatings_AllStartAt6()
    {
        var (state, _) = SetupMatch();

        Assert.All(state.PlayerRatings.Values, rating =>
            Assert.Equal(RatingCalculator.BaseRating, rating));
        Assert.Equal(22, state.PlayerRatings.Count);
    }

    [Fact]
    public void ApplyEvents_GoalIncreasesRating()
    {
        var (state, config) = SetupMatch();

        var events = new List<MatchEvent>
        {
            new() { Tick = 10, Type = EventType.Goal, PlayerId = 5, RatingImpact = 1.5f }
        };

        RatingCalculator.ApplyEvents(state, events, config);

        Assert.True(state.PlayerRatings[5] > RatingCalculator.BaseRating);
    }

    [Fact]
    public void ApplyEvents_FoulDecreasesRating()
    {
        var (state, config) = SetupMatch();

        var events = new List<MatchEvent>
        {
            new() { Tick = 10, Type = EventType.Foul, PlayerId = 3, RatingImpact = -0.2f }
        };

        RatingCalculator.ApplyEvents(state, events, config);

        Assert.True(state.PlayerRatings[3] < RatingCalculator.BaseRating);
    }

    [Fact]
    public void ApplyEvents_RatingClampedToMax10()
    {
        var (state, config) = SetupMatch();

        // Apply many goals to push rating above 10
        var events = Enumerable.Range(0, 10)
            .Select(i => new MatchEvent
            {
                Tick = i, Type = EventType.Goal, PlayerId = 5, RatingImpact = 1.5f
            })
            .ToList();

        RatingCalculator.ApplyEvents(state, events, config);

        Assert.Equal(RatingCalculator.MaxRating, state.PlayerRatings[5]);
    }

    [Fact]
    public void ApplyEvents_RatingClampedToMin0()
    {
        var (state, config) = SetupMatch();

        var events = Enumerable.Range(0, 50)
            .Select(i => new MatchEvent
            {
                Tick = i, Type = EventType.Foul, PlayerId = 3, RatingImpact = -0.3f
            })
            .ToList();

        RatingCalculator.ApplyEvents(state, events, config);

        Assert.Equal(RatingCalculator.MinRating, state.PlayerRatings[3]);
    }

    [Fact]
    public void GetMvpAndSvp_ReturnsCorrectPlayers()
    {
        var (state, _) = SetupMatch();

        state.PlayerRatings[7] = 9.0f;
        state.PlayerRatings[15] = 8.5f;

        var (mvp, svp) = RatingCalculator.GetMvpAndSvp(state);

        Assert.Equal(7, mvp);
        Assert.Equal(15, svp);
    }

    [Fact]
    public void PositionMultiplier_DefenderGetsMoreForTackles()
    {
        float defenderMult = RatingCalculator.GetPositionMultiplier(Position.CB, EventType.Save);
        float attackerMult = RatingCalculator.GetPositionMultiplier(Position.ST, EventType.Save);

        Assert.True(defenderMult > attackerMult);
    }
}
