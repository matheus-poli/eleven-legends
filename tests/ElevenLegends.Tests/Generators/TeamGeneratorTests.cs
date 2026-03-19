using ElevenLegends.Data.Generators;

namespace ElevenLegends.Tests.Generators;

public class TeamGeneratorTests
{
    [Fact]
    public void Generate_Creates_32_Clubs()
    {
        var clubs = TeamGenerator.Generate(42);
        Assert.Equal(32, clubs.Count);
    }

    [Fact]
    public void Generate_Creates_4_Countries_With_8_Teams_Each()
    {
        var clubs = TeamGenerator.Generate(42);
        var byCountry = clubs.GroupBy(c => c.Country).ToList();

        Assert.Equal(4, byCountry.Count);
        Assert.All(byCountry, g => Assert.Equal(8, g.Count()));
    }

    [Fact]
    public void Each_Team_Has_18_Players()
    {
        var clubs = TeamGenerator.Generate(42);
        Assert.All(clubs, c => Assert.Equal(18, c.Team.Players.Count));
    }

    [Fact]
    public void Each_Team_Has_11_Starters()
    {
        var clubs = TeamGenerator.Generate(42);
        Assert.All(clubs, c => Assert.Equal(11, c.Team.StartingLineup.Count));
    }

    [Fact]
    public void All_Player_Ids_Are_Unique()
    {
        var clubs = TeamGenerator.Generate(42);
        var allIds = clubs.SelectMany(c => c.Team.Players.Select(p => p.Id)).ToList();
        Assert.Equal(allIds.Count, allIds.Distinct().Count());
    }

    [Fact]
    public void Stronger_Teams_Have_Higher_Attributes()
    {
        var clubs = TeamGenerator.Generate(42);
        var firstCountry = clubs.Where(c => c.Country == clubs[0].Country).ToList();

        float AvgOverall(Data.Models.Club club) =>
            club.Team.Players.Average(p => p.Attributes.OutfieldOverall);

        float strongAvg = (AvgOverall(firstCountry[0]) + AvgOverall(firstCountry[1])) / 2;
        float weakAvg = (AvgOverall(firstCountry[6]) + AvgOverall(firstCountry[7])) / 2;

        Assert.True(strongAvg > weakAvg,
            $"Strong teams ({strongAvg:F1}) should have higher avg than weak ({weakAvg:F1})");
    }

    [Fact]
    public void Generation_Is_Deterministic()
    {
        var clubs1 = TeamGenerator.Generate(42);
        var clubs2 = TeamGenerator.Generate(42);

        for (int i = 0; i < clubs1.Count; i++)
        {
            Assert.Equal(clubs1[i].Name, clubs2[i].Name);
            Assert.Equal(clubs1[i].Balance, clubs2[i].Balance);
            Assert.Equal(clubs1[i].Reputation, clubs2[i].Reputation);

            for (int j = 0; j < clubs1[i].Team.Players.Count; j++)
            {
                Assert.Equal(clubs1[i].Team.Players[j].Name, clubs2[i].Team.Players[j].Name);
                Assert.Equal(clubs1[i].Team.Players[j].Attributes.Finishing,
                    clubs2[i].Team.Players[j].Attributes.Finishing);
            }
        }
    }

    [Fact]
    public void Different_Seeds_Produce_Different_Teams()
    {
        var clubs1 = TeamGenerator.Generate(42);
        var clubs2 = TeamGenerator.Generate(99);

        // Names are from fixed lists, but attributes should differ
        bool anyDifferent = clubs1.Zip(clubs2)
            .Any(pair => pair.First.Balance != pair.Second.Balance);

        Assert.True(anyDifferent);
    }

    [Fact]
    public void All_Clubs_Have_Positive_Initial_Balance()
    {
        var clubs = TeamGenerator.Generate(42);
        Assert.All(clubs, c => Assert.True(c.Balance > 0));
    }

    [Fact]
    public void All_Clubs_Have_Valid_Reputation()
    {
        var clubs = TeamGenerator.Generate(42);
        Assert.All(clubs, c =>
        {
            Assert.InRange(c.Reputation, 20, 100);
        });
    }
}
