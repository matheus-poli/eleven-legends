using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Tests.Simulation;

/// <summary>
/// Deterministic RNG for testing — returns a fixed value.
/// </summary>
public sealed class FixedRng : IRng
{
    private readonly float _fixedFloat;
    private readonly int _fixedInt;

    public FixedRng(float fixedFloat = 0f, int fixedInt = 0)
    {
        _fixedFloat = fixedFloat;
        _fixedInt = fixedInt;
    }

    public int NextInt(int minInclusive, int maxInclusive) => Math.Clamp(_fixedInt, minInclusive, maxInclusive);
    public float NextFloat(float min, float max) => Math.Clamp(_fixedFloat, min, max);
}

public class SuccessCalculatorTests
{
    private static Player CreatePlayer(
        int passing = 70, int finishing = 70, int dribbling = 70,
        int technique = 70, int strength = 70, int anticipation = 70,
        int morale = 50, int chemistry = 50,
        Position primary = Position.CM, Position? secondary = null,
        List<string>? traits = null)
    {
        return new Player
        {
            Id = 1,
            Name = "Test Player",
            PrimaryPosition = primary,
            SecondaryPosition = secondary,
            Morale = morale,
            Chemistry = chemistry,
            Traits = traits ?? [],
            Attributes = new PlayerAttributes
            {
                Passing = passing,
                Finishing = finishing,
                Dribbling = dribbling,
                FirstTouch = 70,
                Technique = technique,
                Decisions = 70,
                Composure = 70,
                Positioning = 70,
                Anticipation = anticipation,
                OffTheBall = 70,
                Speed = 70,
                Acceleration = 70,
                Stamina = 70,
                Strength = strength,
                Agility = 70,
                Consistency = 70,
                Leadership = 70,
                Flair = 70,
                BigMatches = 70,
                Reflexes = 50,
                Handling = 50,
                GkPositioning = 50,
                Aerial = 50
            }
        };
    }

    [Fact]
    public void GetPrimaryAttribute_MapsCorrectly()
    {
        var attrs = new PlayerAttributes
        {
            Passing = 80, Finishing = 90, Dribbling = 75,
            Technique = 85, Strength = 65, Anticipation = 70
        };

        Assert.Equal(80, SuccessCalculator.GetPrimaryAttribute(attrs, ActionType.Pass));
        Assert.Equal(90, SuccessCalculator.GetPrimaryAttribute(attrs, ActionType.Shot));
        Assert.Equal(75, SuccessCalculator.GetPrimaryAttribute(attrs, ActionType.Dribble));
        Assert.Equal(85, SuccessCalculator.GetPrimaryAttribute(attrs, ActionType.Cross));
        Assert.Equal(65, SuccessCalculator.GetPrimaryAttribute(attrs, ActionType.Tackle));
        Assert.Equal(70, SuccessCalculator.GetPrimaryAttribute(attrs, ActionType.Interception));
    }

    [Theory]
    [InlineData(0, 0f)]
    [InlineData(50, 10f)]
    [InlineData(100, 20f)]
    public void GetChemistryBonus_ScalesCorrectly(int chemistry, float expectedBonus)
    {
        Assert.Equal(expectedBonus, SuccessCalculator.GetChemistryBonus(chemistry));
    }

    [Theory]
    [InlineData(0, -10f)]
    [InlineData(50, 0f)]
    [InlineData(100, 10f)]
    public void GetMoraleBonus_ScalesCorrectly(int morale, float expectedBonus)
    {
        Assert.Equal(expectedBonus, SuccessCalculator.GetMoraleBonus(morale));
    }

    [Fact]
    public void GetTraitBonus_AppliesMatchingTrait()
    {
        float bonus = SuccessCalculator.GetTraitBonus(["Finesse Shot"], ActionType.Shot);
        Assert.Equal(8f, bonus);
    }

    [Fact]
    public void GetTraitBonus_IgnoresMismatchedTrait()
    {
        float bonus = SuccessCalculator.GetTraitBonus(["Finesse Shot"], ActionType.Pass);
        Assert.Equal(0f, bonus);
    }

    [Fact]
    public void GetTraitBonus_CapsAt15()
    {
        float bonus = SuccessCalculator.GetTraitBonus(
            ["Finesse Shot", "Power Shot", "Clinical Finisher"], ActionType.Shot);
        Assert.Equal(15f, bonus);
    }

    [Fact]
    public void GetPositionPenalty_PrimaryIsZero()
    {
        float penalty = SuccessCalculator.GetPositionPenalty(Position.CM, null, Position.CM);
        Assert.Equal(0f, penalty);
    }

    [Fact]
    public void GetPositionPenalty_SecondaryIs10Percent()
    {
        float penalty = SuccessCalculator.GetPositionPenalty(Position.CM, Position.CAM, Position.CAM);
        Assert.Equal(0.10f, penalty);
    }

    [Fact]
    public void GetPositionPenalty_AdaptedIs20Percent()
    {
        // CM and CDM are in the same group (midfield)
        float penalty = SuccessCalculator.GetPositionPenalty(Position.CM, null, Position.CDM);
        Assert.Equal(0.20f, penalty);
    }

    [Fact]
    public void GetPositionPenalty_OutOfPositionIs35Percent()
    {
        // GK playing as ST
        float penalty = SuccessCalculator.GetPositionPenalty(Position.GK, null, Position.ST);
        Assert.Equal(0.35f, penalty);
    }

    [Fact]
    public void Calculate_HighAttributeSucceeds()
    {
        Player player = CreatePlayer(passing: 95, morale: 80, chemistry: 80);
        var rng = new FixedRng(fixedFloat: 5f);

        bool result = SuccessCalculator.Calculate(player, ActionType.Pass, Position.CM, rng);
        Assert.True(result);
    }

    [Fact]
    public void Calculate_LowAttributeFails()
    {
        Player player = CreatePlayer(passing: 10, morale: 10, chemistry: 10);
        var rng = new FixedRng(fixedFloat: -10f);

        bool result = SuccessCalculator.Calculate(player, ActionType.Pass, Position.CM, rng);
        Assert.False(result);
    }

    [Fact]
    public void Calculate_IsDeterministicWithSameSeed()
    {
        Player player = CreatePlayer(passing: 70);

        var rng1 = new SeededRng(42);
        var rng2 = new SeededRng(42);

        float raw1 = SuccessCalculator.CalculateRaw(player, ActionType.Pass, Position.CM, rng1);
        float raw2 = SuccessCalculator.CalculateRaw(player, ActionType.Pass, Position.CM, rng2);

        Assert.Equal(raw1, raw2);
    }

    [Fact]
    public void Calculate_PositionPenaltyReducesChance()
    {
        Player player = CreatePlayer(passing: 70);
        var rng = new FixedRng(fixedFloat: 0f);

        float rawPrimary = SuccessCalculator.CalculateRaw(player, ActionType.Pass, Position.CM, rng);
        float rawOutOfPos = SuccessCalculator.CalculateRaw(player, ActionType.Pass, Position.ST, rng);

        Assert.True(rawPrimary > rawOutOfPos);
    }
}
