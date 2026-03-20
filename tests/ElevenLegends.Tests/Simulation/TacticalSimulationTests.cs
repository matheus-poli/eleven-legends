using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Gacha;
using ElevenLegends.Simulation;

namespace ElevenLegends.Tests.Simulation;

public class TacticalSimulationTests
{
    /// <summary>
    /// Backward compatibility: a MatchConfig with no tactical fields produces identical results.
    /// </summary>
    [Fact]
    public void Simulate_WithoutTactics_ProducesIdenticalResult()
    {
        var config = CreateConfig(seed: 100);

        var result1 = MatchSimulator.Simulate(config);
        var result2 = MatchSimulator.Simulate(config);

        Assert.Equal(result1.ScoreHome, result2.ScoreHome);
        Assert.Equal(result1.ScoreAway, result2.ScoreAway);
        Assert.Equal(result1.MvpPlayerId, result2.MvpPlayerId);
    }

    /// <summary>
    /// Split simulation (first half + second half) produces same result as full simulation
    /// when no halftime effects are applied.
    /// </summary>
    [Fact]
    public void SplitSimulation_WithoutHalftimeEffects_MatchesFullSimulation()
    {
        var config = CreateConfig(seed: 42);

        // Full simulation
        var fullResult = MatchSimulator.Simulate(config);

        // Split simulation (no cards/subs)
        var (state, rng) = MatchSimulator.SimulateFirstHalf(config);
        var splitResult = MatchSimulator.SimulateSecondHalf(state, config, rng);

        Assert.Equal(fullResult.ScoreHome, splitResult.ScoreHome);
        Assert.Equal(fullResult.ScoreAway, splitResult.ScoreAway);
        Assert.Equal(fullResult.MvpPlayerId, splitResult.MvpPlayerId);
    }

    /// <summary>
    /// Attacking tactical style shifts possession towards the attacking team.
    /// </summary>
    [Fact]
    public void AttackingStyle_IncreasesHomePossession()
    {
        float totalAttacking = 0f;
        float totalBalanced = 0f;
        int runs = 30;

        for (int seed = 1; seed <= runs; seed++)
        {
            var attackConfig = CreateConfig(seed, homeTactics: new TacticalSetup
            {
                Formation = Formation.F433,
                Style = TacticalStyle.Attacking,
                StartingPlayerIds = Enumerable.Range(1, 11).ToList()
            });

            var balancedConfig = CreateConfig(seed);

            totalAttacking += MatchSimulator.Simulate(attackConfig).FinalState.PossessionHome;
            totalBalanced += MatchSimulator.Simulate(balancedConfig).FinalState.PossessionHome;
        }

        float avgAttacking = totalAttacking / runs;
        float avgBalanced = totalBalanced / runs;

        Assert.True(avgAttacking > avgBalanced,
            $"Expected attacking ({avgAttacking:F3}) > balanced ({avgBalanced:F3})");
    }

    /// <summary>
    /// Defensive tactical style decreases home possession.
    /// </summary>
    [Fact]
    public void DefensiveStyle_DecreasesHomePossession()
    {
        float totalDefensive = 0f;
        float totalBalanced = 0f;
        int runs = 30;

        for (int seed = 1; seed <= runs; seed++)
        {
            var defConfig = CreateConfig(seed, homeTactics: new TacticalSetup
            {
                Formation = Formation.F532,
                Style = TacticalStyle.Defensive,
                StartingPlayerIds = Enumerable.Range(1, 11).ToList()
            });

            var balancedConfig = CreateConfig(seed);

            totalDefensive += MatchSimulator.Simulate(defConfig).FinalState.PossessionHome;
            totalBalanced += MatchSimulator.Simulate(balancedConfig).FinalState.PossessionHome;
        }

        float avgDefensive = totalDefensive / runs;
        float avgBalanced = totalBalanced / runs;

        Assert.True(avgDefensive < avgBalanced,
            $"Expected defensive ({avgDefensive:F3}) < balanced ({avgBalanced:F3})");
    }

    /// <summary>
    /// Halftime card effects are applied and affect the second half.
    /// </summary>
    [Fact]
    public void HalftimeCard_TeamBuff_IncreasesBonusModifier()
    {
        var config = CreateConfig(seed: 42);
        var (state, rng) = MatchSimulator.SimulateFirstHalf(config);

        Assert.Equal(0f, state.HomeBonusModifier);

        var card = new LockerRoomCard
        {
            Name = "Team Talk",
            Description = "Boost the team",
            Effect = CardEffect.TeamBuff,
            Magnitude = 5
        };

        HalftimeProcessor.ApplyCard(state, config, card, isHomeTeam: true);

        Assert.Equal(5f, state.HomeBonusModifier);
    }

    /// <summary>
    /// Halftime card: stamina recovery restores player stamina.
    /// </summary>
    [Fact]
    public void HalftimeCard_StaminaRecovery_RestoresStamina()
    {
        var config = CreateConfig(seed: 42);
        var (state, _) = MatchSimulator.SimulateFirstHalf(config);

        float preSumStamina = state.HomeActivePlayerIds
            .Sum(id => state.PlayerStamina[id]);

        var card = new LockerRoomCard
        {
            Name = "Energy Drink",
            Description = "Recover stamina",
            Effect = CardEffect.StaminaRecovery,
            Magnitude = 20
        };

        HalftimeProcessor.ApplyCard(state, config, card, isHomeTeam: true);

        float postSumStamina = state.HomeActivePlayerIds
            .Sum(id => state.PlayerStamina[id]);

        Assert.True(postSumStamina > preSumStamina,
            $"Expected stamina increase: before={preSumStamina:F0}, after={postSumStamina:F0}");
    }

    /// <summary>
    /// Substitutions swap players in the active lineup.
    /// </summary>
    [Fact]
    public void Substitution_SwapsPlayerInActiveLineup()
    {
        var config = CreateConfigWith18Players(seed: 42);
        var (state, _) = MatchSimulator.SimulateFirstHalf(config);

        int playerOutId = state.HomeActivePlayerIds[10]; // Last starter
        int playerInId = 12; // First bench player

        var subs = new List<Substitution>
        {
            new() { PlayerOutId = playerOutId, PlayerInId = playerInId }
        };

        HalftimeProcessor.ApplySubstitutions(state, config, subs, isHomeTeam: true);

        Assert.Contains(playerInId, state.HomeActivePlayerIds);
        Assert.DoesNotContain(playerOutId, state.HomeActivePlayerIds);
        Assert.Equal(1, state.HomeSubstitutionsUsed);
    }

    /// <summary>
    /// Maximum 3 substitutions per team.
    /// </summary>
    [Fact]
    public void Substitution_MaxThreePerTeam()
    {
        var config = CreateConfigWith18Players(seed: 42);
        var (state, _) = MatchSimulator.SimulateFirstHalf(config);

        var subs = new List<Substitution>
        {
            new() { PlayerOutId = 1, PlayerInId = 12 },
            new() { PlayerOutId = 2, PlayerInId = 13 },
            new() { PlayerOutId = 3, PlayerInId = 14 },
            new() { PlayerOutId = 4, PlayerInId = 15 }, // Should be ignored
        };

        HalftimeProcessor.ApplySubstitutions(state, config, subs, isHomeTeam: true);

        Assert.Equal(3, state.HomeSubstitutionsUsed);
        Assert.Contains(4, state.HomeActivePlayerIds); // Player 4 should still be in
    }

    /// <summary>
    /// LockerRoomCardGenerator produces 3 unique cards.
    /// </summary>
    [Fact]
    public void CardGenerator_Produces3UniqueCards()
    {
        var rng = new SeededRng(42);
        var cards = LockerRoomCardGenerator.Generate(rng, scoreDiff: 0, avgStamina: 60f, avgMorale: 50f);

        Assert.Equal(3, cards.Count);
        Assert.Equal(3, cards.Select(c => c.Name).Distinct().Count());
    }

    /// <summary>
    /// When losing, card generator favors morale and stamina cards.
    /// </summary>
    [Fact]
    public void CardGenerator_WhenLosing_FavorsMoraleCards()
    {
        int moraleCountLosing = 0;
        int moraleCountWinning = 0;
        int runs = 100;

        for (int seed = 1; seed <= runs; seed++)
        {
            var rng1 = new SeededRng(seed);
            var losingCards = LockerRoomCardGenerator.Generate(rng1, scoreDiff: -2, avgStamina: 40f, avgMorale: 30f);
            moraleCountLosing += losingCards.Count(c => c.Effect is CardEffect.MoraleBoost or CardEffect.StaminaRecovery);

            var rng2 = new SeededRng(seed);
            var winningCards = LockerRoomCardGenerator.Generate(rng2, scoreDiff: 2, avgStamina: 80f, avgMorale: 80f);
            moraleCountWinning += winningCards.Count(c => c.Effect is CardEffect.MoraleBoost or CardEffect.StaminaRecovery);
        }

        Assert.True(moraleCountLosing > moraleCountWinning,
            $"Expected more morale/stamina cards when losing ({moraleCountLosing}) vs winning ({moraleCountWinning})");
    }

    /// <summary>
    /// Formation presets all have exactly 11 positions.
    /// </summary>
    [Fact]
    public void Formation_Presets_AllHave11Positions()
    {
        Assert.All(Formation.Presets, f =>
        {
            Assert.Equal(11, f.Positions.Count);
            Assert.Contains(Position.GK, f.Positions);
        });
    }

    /// <summary>
    /// Full match with tactical setup completes without errors.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(99)]
    [InlineData(500)]
    public void FullMatchWithTactics_CompletesSuccessfully(int seed)
    {
        var config = CreateConfig(seed,
            homeTactics: new TacticalSetup
            {
                Formation = Formation.F433,
                Style = TacticalStyle.Attacking,
                StartingPlayerIds = Enumerable.Range(1, 11).ToList()
            },
            awayTactics: new TacticalSetup
            {
                Formation = Formation.F532,
                Style = TacticalStyle.Defensive,
                StartingPlayerIds = Enumerable.Range(12, 11).ToList()
            });

        var result = MatchSimulator.Simulate(config);

        Assert.Equal(MatchPhase.Finished, result.FinalState.Phase);
        Assert.InRange(result.ScoreHome, 0, 12);
        Assert.InRange(result.ScoreAway, 0, 12);
    }

    #region Helpers

    private static MatchConfig CreateConfig(int seed,
        TacticalSetup? homeTactics = null, TacticalSetup? awayTactics = null)
    {
        return new MatchConfig
        {
            HomeTeam = CreateTeam(1, "Home FC", Enumerable.Range(1, 11).ToList()),
            AwayTeam = CreateTeam(2, "Away FC", Enumerable.Range(12, 11).ToList()),
            Seed = seed,
            HomeTactics = homeTactics,
            AwayTactics = awayTactics
        };
    }

    private static MatchConfig CreateConfigWith18Players(int seed)
    {
        var homePlayers = Enumerable.Range(1, 18).Select(i => MakePlayer(i, $"Home P{i}",
            i == 1 ? Position.GK : i <= 4 ? Position.CB : i <= 6 ? Position.CM : i <= 8 ? Position.LW :
            i <= 11 ? Position.ST : i == 12 ? Position.GK : i <= 14 ? Position.CB : i <= 16 ? Position.CM : Position.ST
        )).ToList();

        var awayPlayers = Enumerable.Range(20, 11).Select(i => MakePlayer(i, $"Away P{i}",
            i == 20 ? Position.GK : i <= 23 ? Position.CB : i <= 25 ? Position.CM : Position.ST
        )).ToList();

        return new MatchConfig
        {
            HomeTeam = new Team
            {
                Id = 1, Name = "Home FC",
                Players = homePlayers,
                StartingLineup = Enumerable.Range(1, 11).ToList()
            },
            AwayTeam = new Team
            {
                Id = 2, Name = "Away FC",
                Players = awayPlayers,
                StartingLineup = Enumerable.Range(20, 11).ToList()
            },
            Seed = seed
        };
    }

    private static Team CreateTeam(int id, string name, List<int> playerIds)
    {
        var players = playerIds.Select((pid, idx) =>
            MakePlayer(pid, $"{name} P{idx + 1}",
                idx == 0 ? Position.GK :
                idx <= 3 ? Position.CB :
                idx <= 5 ? Position.CM :
                idx <= 8 ? Position.LW :
                Position.ST)
        ).ToList();

        return new Team
        {
            Id = id,
            Name = name,
            Players = players,
            StartingLineup = playerIds
        };
    }

    private static Player MakePlayer(int id, string name, Position pos)
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
                Finishing = 65, Passing = 65, Dribbling = 65,
                FirstTouch = 65, Technique = 65,
                Decisions = 65, Composure = 65, Positioning = 65,
                Anticipation = 65, OffTheBall = 65,
                Speed = 65, Acceleration = 65, Stamina = 80,
                Strength = 65, Agility = 65,
                Consistency = 65, Leadership = 60, Flair = 60, BigMatches = 60,
                Reflexes = 70, Handling = 60, GkPositioning = 70, Aerial = 55
            }
        };
    }

    #endregion
}
