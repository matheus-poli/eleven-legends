using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.Transfers;

namespace ElevenLegends.Tests.Transfers;

public class PlayerValuationTests
{
    [Fact]
    public void Calculate_HigherOverall_HigherValue()
    {
        var weak = CreatePlayer(40);
        var strong = CreatePlayer(80);

        decimal weakVal = PlayerValuation.Calculate(weak);
        decimal strongVal = PlayerValuation.Calculate(strong);

        Assert.True(strongVal > weakVal,
            $"Strong player ({strongVal:C0}) should be worth more than weak ({weakVal:C0})");
    }

    [Fact]
    public void Calculate_PeakAge_HigherThanOld()
    {
        var peak = CreatePlayer(70, age: 26);
        var old = CreatePlayer(70, age: 34);

        decimal peakVal = PlayerValuation.Calculate(peak);
        decimal oldVal = PlayerValuation.Calculate(old);

        Assert.True(peakVal > oldVal,
            $"Peak age ({peakVal:C0}) should be worth more than old ({oldVal:C0})");
    }

    [Fact]
    public void Calculate_HighRepClub_HigherValue()
    {
        var player = CreatePlayer(70);

        decimal lowRep = PlayerValuation.Calculate(player, clubReputation: 20);
        decimal highRep = PlayerValuation.Calculate(player, clubReputation: 90);

        Assert.True(highRep > lowRep,
            $"High rep club ({highRep:C0}) should markup vs low rep ({lowRep:C0})");
    }

    [Fact]
    public void Calculate_MinimumValue_Is5000()
    {
        var veryWeak = CreatePlayer(15, age: 35);
        decimal val = PlayerValuation.Calculate(veryWeak);
        Assert.True(val >= 5_000m, $"Minimum value should be 5000, got {val}");
    }

    [Fact]
    public void Calculate_ReasonableRangeForDemoEconomy()
    {
        // Average player (65 ovr, age 25) should be affordable in demo
        var avg = CreatePlayer(65, age: 25);
        decimal val = PlayerValuation.Calculate(avg);
        Assert.InRange(val, 5_000m, 100_000m);

        // Star player (85 ovr, age 26) should be more expensive than average
        var star = CreatePlayer(85, age: 26);
        decimal starVal = PlayerValuation.Calculate(star);
        Assert.True(starVal > val,
            $"Star ({starVal:C0}) should be more expensive than average ({val:C0})");
    }

    [Fact]
    public void EstimateWeeklySalary_ProportionalToOverall()
    {
        var low = CreatePlayer(40);
        var high = CreatePlayer(80);

        decimal lowSalary = PlayerValuation.EstimateWeeklySalary(low);
        decimal highSalary = PlayerValuation.EstimateWeeklySalary(high);

        Assert.True(highSalary > lowSalary);
    }

    private static Player CreatePlayer(int overall, int age = 25, Position pos = Position.CM)
    {
        return new Player
        {
            Id = 1,
            Name = "Test",
            PrimaryPosition = pos,
            Age = age,
            Morale = 50,
            Chemistry = 50,
            Attributes = new PlayerAttributes
            {
                Finishing = overall, Passing = overall, Dribbling = overall,
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
}
