using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.Transfers;

namespace ElevenLegends.Tests.Transfers;

public class TransferMarketTests
{
    [Fact]
    public void ExecuteBuy_TransfersPlayerAndUpdateBalances()
    {
        var (buyer, seller) = CreateTwoClubs();
        var player = seller.Team.Players.Last(); // A reserve

        decimal price = 50_000m;
        decimal buyerBefore = buyer.Balance;
        decimal sellerBefore = seller.Balance;

        bool success = TransferMarket.ExecuteBuy(buyer, seller, player, price);

        Assert.True(success);
        Assert.Contains(player, buyer.Team.Players);
        Assert.DoesNotContain(player, seller.Team.Players);
        Assert.Equal(buyerBefore - price, buyer.Balance);
        Assert.Equal(sellerBefore + price, seller.Balance);
    }

    [Fact]
    public void ExecuteBuy_FailsWhenBuyerCantAfford()
    {
        var (buyer, seller) = CreateTwoClubs();
        buyer.Balance = 100m;
        var player = seller.Team.Players.Last();

        bool success = TransferMarket.ExecuteBuy(buyer, seller, player, 50_000m);

        Assert.False(success);
    }

    [Fact]
    public void ExecuteBuy_FailsWhenBuyerSquadFull()
    {
        var (buyer, seller) = CreateTwoClubs();

        // Fill buyer's squad to max
        var extraPlayers = Enumerable.Range(200, TransferMarket.MaxSquadSize - buyer.Team.Players.Count)
            .Select(id => MakePlayer(id, $"Extra {id}", Position.CM))
            .ToList();

        var allPlayers = buyer.Team.Players.ToList();
        allPlayers.AddRange(extraPlayers);
        buyer.Team = buyer.Team with { Players = allPlayers };

        var player = seller.Team.Players.Last();
        bool success = TransferMarket.ExecuteBuy(buyer, seller, player, 1_000m);

        Assert.False(success);
    }

    [Fact]
    public void ExecuteBuy_FailsWhenSellerAtMinSquad()
    {
        var (buyer, seller) = CreateTwoClubs();

        // Trim seller to minimum
        var trimmed = seller.Team.Players.Take(TransferMarket.MinSquadSize).ToList();
        seller.Team = seller.Team with
        {
            Players = trimmed,
            StartingLineup = trimmed.Take(11).Select(p => p.Id).ToList()
        };

        var player = trimmed.Last();
        bool success = TransferMarket.ExecuteBuy(buyer, seller, player, 1_000m);

        Assert.False(success);
    }

    [Fact]
    public void CanRemovePlayer_ProtectsLastGK()
    {
        var (club, _) = CreateTwoClubs();

        // Only 1 GK in the squad
        var gk = club.Team.Players.First(p => p.PrimaryPosition == Position.GK);
        int gkCount = club.Team.Players.Count(p => p.PrimaryPosition == Position.GK);

        if (gkCount <= 1)
            Assert.False(TransferMarket.CanRemovePlayer(club, gk));
    }

    [Fact]
    public void ExecuteLoanIn_TransfersPlayerAndCreatesLoanRecord()
    {
        var (host, source) = CreateTwoClubs();
        var player = source.Team.Players.Last();
        decimal fee = 10_000m;

        var loan = TransferMarket.ExecuteLoanIn(host, source, player, fee);

        Assert.NotNull(loan);
        Assert.Equal(player.Id, loan.PlayerId);
        Assert.Equal(source.Id, loan.OriginClubId);
        Assert.Equal(host.Id, loan.HostClubId);
        Assert.Contains(player, host.Team.Players);
        Assert.DoesNotContain(player, source.Team.Players);
    }

    [Fact]
    public void ExecuteLoanOut_TransfersPlayerNoFee()
    {
        var (source, host) = CreateTwoClubs();
        var player = source.Team.Players.Last();
        decimal sourceBefore = source.Balance;

        var loan = TransferMarket.ExecuteLoanOut(source, host, player);

        Assert.NotNull(loan);
        Assert.Equal(sourceBefore, source.Balance); // No fee deducted
        Assert.DoesNotContain(player, source.Team.Players);
        Assert.Contains(player, host.Team.Players);
    }

    [Fact]
    public void AddFreeAgent_AddsPlayerToClub()
    {
        var (club, _) = CreateTwoClubs();
        var freeAgent = MakePlayer(999, "Free Agent", Position.ST);

        bool success = TransferMarket.AddFreeAgent(club, freeAgent, 5_000m);

        Assert.True(success);
        Assert.Contains(freeAgent, club.Team.Players);
    }

    [Fact]
    public void GetAvailablePlayers_ExcludesStarters()
    {
        var (_, seller) = CreateTwoClubs();
        var clubs = new List<Club> { seller };

        var available = TransferMarket.GetAvailablePlayers(clubs, excludeClubId: -1);

        var starterSet = new HashSet<int>(seller.Team.StartingLineup);
        Assert.All(available, a => Assert.DoesNotContain(a.Player.Id, starterSet));
    }

    [Fact]
    public void GetSellablePlayers_RespectsMinSquadSize()
    {
        var (club, _) = CreateTwoClubs();

        // Trim to exactly MinSquadSize
        var trimmed = club.Team.Players.Take(TransferMarket.MinSquadSize).ToList();
        club.Team = club.Team with
        {
            Players = trimmed,
            StartingLineup = trimmed.Take(11).Select(p => p.Id).ToList()
        };

        var sellable = TransferMarket.GetSellablePlayers(club);
        Assert.Empty(sellable);
    }

    #region Helpers

    private static (Club A, Club B) CreateTwoClubs()
    {
        var playersA = CreateSquad(1, 18);
        var playersB = CreateSquad(100, 18);

        var clubA = new Club
        {
            Id = 1, Name = "Club A", Country = "Brasilândia",
            Balance = 300_000m, Reputation = 70,
            Team = new Team
            {
                Id = 1, Name = "Club A",
                Players = playersA,
                StartingLineup = playersA.Take(11).Select(p => p.Id).ToList()
            }
        };

        var clubB = new Club
        {
            Id = 2, Name = "Club B", Country = "Hispânia",
            Balance = 200_000m, Reputation = 60,
            Team = new Team
            {
                Id = 2, Name = "Club B",
                Players = playersB,
                StartingLineup = playersB.Take(11).Select(p => p.Id).ToList()
            }
        };

        return (clubA, clubB);
    }

    private static List<Player> CreateSquad(int startId, int count)
    {
        var positions = new[]
        {
            Position.GK, Position.CB, Position.CB, Position.LB, Position.RB,
            Position.CDM, Position.CM, Position.CM,
            Position.LW, Position.RW, Position.ST,
            // Reserves
            Position.GK, Position.CB, Position.CM, Position.CAM,
            Position.RW, Position.ST, Position.CF
        };

        return Enumerable.Range(0, Math.Min(count, positions.Length))
            .Select(i => MakePlayer(startId + i, $"Player {startId + i}", positions[i]))
            .ToList();
    }

    private static Player MakePlayer(int id, string name, Position pos, int overall = 65)
    {
        return new Player
        {
            Id = id, Name = name,
            PrimaryPosition = pos,
            Age = 25, Morale = 60, Chemistry = 60,
            Attributes = new PlayerAttributes
            {
                Finishing = overall, Passing = overall, Dribbling = overall,
                FirstTouch = overall, Technique = overall,
                Decisions = overall, Composure = overall, Positioning = overall,
                Anticipation = overall, OffTheBall = overall,
                Speed = overall, Acceleration = overall, Stamina = 80,
                Strength = overall, Agility = overall,
                Consistency = overall, Leadership = 60, Flair = 60, BigMatches = 60,
                Reflexes = 70, Handling = 60, GkPositioning = 70, Aerial = 55
            }
        };
    }

    #endregion
}
