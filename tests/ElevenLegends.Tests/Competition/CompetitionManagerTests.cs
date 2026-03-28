using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.Manager;

namespace ElevenLegends.Tests.Competition;

public class CompetitionManagerTests
{
    private static (CompetitionManager manager, List<Club> clubs) CreateTestSetup(int seed = 42)
    {
        var clubs = TeamGenerator.Generate(seed);
        var manager = new CompetitionManager(clubs, seed);
        return (manager, clubs);
    }

    [Fact]
    public void Creates_4_National_Brackets()
    {
        var (manager, _) = CreateTestSetup();
        Assert.Equal(4, manager.NationalBrackets.Count);
    }

    [Fact]
    public void National_Round_Generates_16_Quarterfinal_Fixtures()
    {
        var (manager, _) = CreateTestSetup();
        var fixtures = manager.GenerateNationalRound(1);
        Assert.Equal(16, fixtures.Count); // 4 countries × 4 matches
    }

    [Fact]
    public void Full_National_Season_Produces_4_Champions()
    {
        var (manager, clubs) = CreateTestSetup();

        // Quarterfinals
        var qf = manager.GenerateNationalRound(1);
        manager.SimulateFixtures(qf, 1000);
        manager.AdvanceNationalRounds();

        // Semifinals
        var sf = manager.GenerateNationalRound(3);
        manager.SimulateFixtures(sf, 2000);
        manager.AdvanceNationalRounds();

        // Finals
        var finals = manager.GenerateNationalRound(5);
        manager.SimulateFixtures(finals, 3000);
        manager.AdvanceNationalRounds();

        Assert.True(manager.AreNationalsFinished());

        var champions = manager.NationalBrackets.Values
            .Select(b => b.ChampionId)
            .ToList();

        Assert.Equal(4, champions.Count);
        Assert.All(champions, c => Assert.NotNull(c));
    }

    [Fact]
    public void Full_Season_With_Mundial_Completes()
    {
        var (manager, clubs) = CreateTestSetup();

        // National rounds
        for (int round = 0; round < 3; round++)
        {
            var fixtures = manager.GenerateNationalRound(round * 2 + 1);
            manager.SimulateFixtures(fixtures, (round + 1) * 1000);
            manager.AdvanceNationalRounds();
        }

        Assert.True(manager.AreNationalsFinished());
        manager.CreateMundial();
        Assert.NotNull(manager.MundialBracket);

        // Mundial semis
        var mundialSf = manager.GenerateMundialRound(10);
        manager.SimulateFixtures(mundialSf, 5000);
        manager.AdvanceMundialRound();

        // Mundial final
        var mundialFinal = manager.GenerateMundialRound(12);
        manager.SimulateFixtures(mundialFinal, 6000);
        manager.AdvanceMundialRound();

        Assert.True(manager.IsSeasonComplete());
        Assert.NotNull(manager.GetMundialChampion());
    }

    [Fact]
    public void Season_Is_Deterministic()
    {
        int? RunSeason(int seed)
        {
            var (manager, clubs) = CreateTestSetup(seed);

            for (int round = 0; round < 3; round++)
            {
                var fixtures = manager.GenerateNationalRound(round * 2 + 1);
                manager.SimulateFixtures(fixtures, (round + 1) * 1000);
                manager.AdvanceNationalRounds();
            }

            manager.CreateMundial();

            var sf = manager.GenerateMundialRound(10);
            manager.SimulateFixtures(sf, 5000);
            manager.AdvanceMundialRound();

            var f = manager.GenerateMundialRound(12);
            manager.SimulateFixtures(f, 6000);
            manager.AdvanceMundialRound();

            return manager.GetMundialChampion();
        }

        int? champ1 = RunSeason(42);
        int? champ2 = RunSeason(42);

        Assert.Equal(champ1, champ2);
    }
}
