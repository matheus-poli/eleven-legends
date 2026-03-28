using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Tests.Simulation;

public class TickProcessorTests
{
    private static (MatchState state, MatchConfig config) CreateMatchSetup()
    {
        var homePlayers = new List<Player>
        {
            MakePlayer(1, "GK Home", Position.GK, reflexes: 75, gkPos: 70),
            MakePlayer(2, "CB Home 1", Position.CB, strength: 75),
            MakePlayer(3, "CB Home 2", Position.CB, strength: 72),
            MakePlayer(4, "LB Home", Position.LB, speed: 78),
            MakePlayer(5, "RB Home", Position.RB, speed: 76),
            MakePlayer(6, "CDM Home", Position.CDM, anticipation: 74),
            MakePlayer(7, "CM Home", Position.CM, passing: 80),
            MakePlayer(8, "CAM Home", Position.CAM, technique: 82),
            MakePlayer(9, "LW Home", Position.LW, dribbling: 80),
            MakePlayer(10, "RW Home", Position.RW, dribbling: 78),
            MakePlayer(11, "ST Home", Position.ST, finishing: 85),
        };

        var awayPlayers = new List<Player>
        {
            MakePlayer(12, "GK Away", Position.GK, reflexes: 73, gkPos: 68),
            MakePlayer(13, "CB Away 1", Position.CB, strength: 73),
            MakePlayer(14, "CB Away 2", Position.CB, strength: 70),
            MakePlayer(15, "LB Away", Position.LB, speed: 75),
            MakePlayer(16, "RB Away", Position.RB, speed: 74),
            MakePlayer(17, "CDM Away", Position.CDM, anticipation: 72),
            MakePlayer(18, "CM Away", Position.CM, passing: 76),
            MakePlayer(19, "CAM Away", Position.CAM, technique: 78),
            MakePlayer(20, "LW Away", Position.LW, dribbling: 76),
            MakePlayer(21, "RW Away", Position.RW, dribbling: 74),
            MakePlayer(22, "ST Away", Position.ST, finishing: 80),
        };

        var home = new Team
        {
            Id = 1, Name = "FC Home",
            Players = homePlayers,
            StartingLineup = homePlayers.Select(p => p.Id).ToList()
        };
        var away = new Team
        {
            Id = 2, Name = "FC Away",
            Players = awayPlayers,
            StartingLineup = awayPlayers.Select(p => p.Id).ToList()
        };

        var config = new MatchConfig { HomeTeam = home, AwayTeam = away, Seed = 42 };
        var state = new MatchState
        {
            BallPossessionTeamId = home.Id,
            BallZone = FieldZone.MidfieldCenter
        };

        RatingCalculator.InitializeRatings(state, config);
        TickProcessor.InitializeStamina(state, config);

        return (state, config);
    }

    [Fact]
    public void ProcessTick_AdvancesTickCounter()
    {
        var (state, config) = CreateMatchSetup();
        var rng = new SeededRng(42);

        TickProcessor.ProcessTick(state, config, rng);

        Assert.Equal(1, state.CurrentTick);
        Assert.Equal(1, state.TotalTicksPlayed);
    }

    [Fact]
    public void ProcessTick_SetsPossession()
    {
        var (state, config) = CreateMatchSetup();
        var rng = new SeededRng(42);

        TickProcessor.ProcessTick(state, config, rng);

        Assert.True(
            state.BallPossessionTeamId == config.HomeTeam.Id ||
            state.BallPossessionTeamId == config.AwayTeam.Id);
    }

    [Fact]
    public void ProcessTick_GeneratesAtLeastOneEvent()
    {
        var (state, config) = CreateMatchSetup();
        var rng = new SeededRng(42);

        // Run enough ticks to have a good chance of generating events
        for (int i = 0; i < 20; i++)
            TickProcessor.ProcessTick(state, config, rng);

        Assert.NotEmpty(state.Events);
    }

    [Fact]
    public void ProcessTick_DegradeStamina()
    {
        var (state, config) = CreateMatchSetup();
        var rng = new SeededRng(42);

        float initialStamina = state.PlayerStamina[1];
        TickProcessor.ProcessTick(state, config, rng);

        Assert.True(state.PlayerStamina[1] < initialStamina);
    }

    [Fact]
    public void ProcessTick_UpdatesPossessionPercentage()
    {
        var (state, config) = CreateMatchSetup();
        var rng = new SeededRng(42);

        for (int i = 0; i < 20; i++)
            TickProcessor.ProcessTick(state, config, rng);

        // Possession should be between 0 and 1
        Assert.InRange(state.PossessionHome, 0f, 1f);
    }

    [Fact]
    public void InitializeStamina_SetsForAllPlayers()
    {
        var (state, config) = CreateMatchSetup();

        Assert.Equal(22, state.PlayerStamina.Count);
        Assert.All(state.PlayerStamina.Values, stamina => Assert.True(stamina > 0));
    }

    private static Player MakePlayer(int id, string name, Position pos,
        int finishing = 65, int passing = 65, int dribbling = 65,
        int technique = 65, int strength = 65, int anticipation = 65,
        int speed = 65, int reflexes = 50, int gkPos = 50)
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
                Decisions = 65, Composure = 65, Positioning = 65,
                Anticipation = anticipation, OffTheBall = 65,
                Speed = speed, Acceleration = 65, Stamina = 75,
                Strength = strength, Agility = 65,
                Consistency = 65, Leadership = 60, Flair = 60, BigMatches = 60,
                Reflexes = reflexes, Handling = 60, GkPositioning = gkPos, Aerial = 55
            }
        };
    }
}
