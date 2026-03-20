using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.Transfers;

namespace ElevenLegends.Tests.Transfers;

public class YouthAcademyAndScoutTests
{
    [Fact]
    public void YouthAcademy_Generates3Prospects()
    {
        var rng = new SeededRng(42);
        var prospects = YouthAcademy.GenerateProspects(rng, "Brasilândia", nextPlayerId: 1000);

        Assert.Equal(3, prospects.Count);
    }

    [Fact]
    public void YouthAcademy_ProspectsAreYoung()
    {
        var rng = new SeededRng(42);
        var prospects = YouthAcademy.GenerateProspects(rng, "Brasilândia", nextPlayerId: 1000);

        Assert.All(prospects, p =>
        {
            Assert.InRange(p.Prospect.Age, 16, 19);
        });
    }

    [Fact]
    public void YouthAcademy_ProspectsHaveUniquIds()
    {
        var rng = new SeededRng(42);
        var prospects = YouthAcademy.GenerateProspects(rng, "Brasilândia", nextPlayerId: 1000);

        var ids = prospects.Select(p => p.Prospect.Id).ToList();
        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public void YouthAcademy_FeeInReasonableRange()
    {
        var rng = new SeededRng(42);
        var prospects = YouthAcademy.GenerateProspects(rng, "Brasilândia", nextPlayerId: 1000);

        Assert.All(prospects, p =>
        {
            Assert.InRange(p.Fee, 5_000m, 15_000m);
        });
    }

    [Fact]
    public void YouthAcademy_DifferentCountriesProduceDifferentNames()
    {
        var rng1 = new SeededRng(42);
        var rng2 = new SeededRng(42);

        var brProspects = YouthAcademy.GenerateProspects(rng1, "Brasilândia", 1000);
        var engProspects = YouthAcademy.GenerateProspects(rng2, "Angleterre", 2000);

        // Different country pools should produce different name patterns
        Assert.NotEqual(brProspects[0].Prospect.Name, engProspects[0].Prospect.Name);
    }

    [Fact]
    public void YouthAcademy_GetMaxPlayerId_ReturnsCorrectMax()
    {
        var clubs = new List<Club>
        {
            CreateClubWithPlayerIds([1, 5, 10]),
            CreateClubWithPlayerIds([3, 7, 20])
        };

        int maxId = YouthAcademy.GetMaxPlayerId(clubs);
        Assert.Equal(20, maxId);
    }

    [Fact]
    public void ScoutingSystem_Has7Regions()
    {
        var regions = ScoutingSystem.GetRegions();
        Assert.Equal(7, regions.Count);
    }

    [Fact]
    public void ScoutingSystem_Scout_Returns3To5Players()
    {
        var regions = ScoutingSystem.GetRegions();
        var rng = new SeededRng(42);

        foreach (var region in regions)
        {
            var players = ScoutingSystem.Scout(rng, region, nextPlayerId: 500);
            Assert.InRange(players.Count, 3, 5);
        }
    }

    [Fact]
    public void ScoutingSystem_Scout_PlayersHaveValidAttributes()
    {
        var region = ScoutingSystem.GetRegions()[0]; // Brasilândia
        var rng = new SeededRng(42);

        var players = ScoutingSystem.Scout(rng, region, nextPlayerId: 500);

        Assert.All(players, player =>
        {
            Assert.InRange(player.Age, 19, 33);
            Assert.True(player.Attributes.OutfieldOverall > 0 || player.Attributes.GoalkeeperOverall > 0);
            Assert.False(string.IsNullOrEmpty(player.Name));
        });
    }

    [Fact]
    public void ScoutingSystem_Scout_UniqueIds()
    {
        var region = ScoutingSystem.GetRegions()[4]; // África
        var rng = new SeededRng(42);

        var players = ScoutingSystem.Scout(rng, region, nextPlayerId: 500);
        var ids = players.Select(p => p.Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void ScoutingSystem_InternationalRegionsExist()
    {
        var regions = ScoutingSystem.GetRegions();
        var names = regions.Select(r => r.Name).ToList();

        Assert.Contains("África", names);
        Assert.Contains("Ásia", names);
        Assert.Contains("Américas", names);
    }

    private static Club CreateClubWithPlayerIds(int[] ids)
    {
        var players = ids.Select(id => new Player
        {
            Id = id,
            Name = $"P{id}",
            PrimaryPosition = Position.CM,
            Age = 25,
            Morale = 50,
            Chemistry = 50,
            Attributes = new PlayerAttributes
            {
                Finishing = 50, Passing = 50, Dribbling = 50,
                FirstTouch = 50, Technique = 50,
                Decisions = 50, Composure = 50, Positioning = 50,
                Anticipation = 50, OffTheBall = 50,
                Speed = 50, Acceleration = 50, Stamina = 50,
                Strength = 50, Agility = 50,
                Consistency = 50, Leadership = 50, Flair = 50, BigMatches = 50,
                Reflexes = 50, Handling = 50, GkPositioning = 50, Aerial = 50
            }
        }).ToList();

        return new Club
        {
            Id = ids[0] * 100,
            Name = $"Club {ids[0]}",
            Country = "Brasilândia",
            Balance = 100_000m,
            Reputation = 50,
            Team = new Team
            {
                Id = ids[0] * 100,
                Name = $"Club {ids[0]}",
                Players = players,
                StartingLineup = players.Select(p => p.Id).ToList()
            }
        };
    }
}
