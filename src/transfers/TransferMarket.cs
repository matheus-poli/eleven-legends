using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Transfers;

/// <summary>
/// Handles transfer operations: buy, sell, loan in, loan out.
/// Validates squad size rules (min 14, max 22, at least 1 GK).
/// </summary>
public static class TransferMarket
{
    public const int MinSquadSize = 14;
    public const int MaxSquadSize = 22;

    /// <summary>
    /// Returns players from other clubs that are available for purchase.
    /// </summary>
    public static List<(Player Player, Club Club, decimal Price)> GetAvailablePlayers(
        IReadOnlyList<Club> clubs, int excludeClubId)
    {
        var available = new List<(Player, Club, decimal)>();

        foreach (var club in clubs)
        {
            if (club.Id == excludeClubId) continue;
            if (club.Team.Players.Count <= MinSquadSize) continue;

            var starterSet = new HashSet<int>(club.Team.StartingLineup);
            foreach (var player in club.Team.Players)
            {
                if (starterSet.Contains(player.Id)) continue;
                decimal price = PlayerValuation.Calculate(player, club.Reputation);
                available.Add((player, club, price));
            }
        }

        return available;
    }

    /// <summary>
    /// Returns players from a club that can be sold.
    /// </summary>
    public static List<(Player Player, decimal Price)> GetSellablePlayers(Club club)
    {
        if (club.Team.Players.Count <= MinSquadSize)
            return [];

        var results = new List<(Player, decimal)>();
        foreach (var player in club.Team.Players)
        {
            if (!CanRemovePlayer(club, player)) continue;
            decimal price = PlayerValuation.Calculate(player, club.Reputation);
            results.Add((player, price));
        }
        return results;
    }

    /// <summary>
    /// Returns players from other clubs available for loan.
    /// </summary>
    public static List<(Player Player, Club Club, decimal Fee)> GetLoanablePlayersIn(
        IReadOnlyList<Club> clubs, int excludeClubId)
    {
        var available = new List<(Player, Club, decimal)>();

        foreach (var club in clubs)
        {
            if (club.Id == excludeClubId) continue;
            if (club.Team.Players.Count <= MinSquadSize) continue;

            var starterSet = new HashSet<int>(club.Team.StartingLineup);
            foreach (var player in club.Team.Players)
            {
                if (starterSet.Contains(player.Id)) continue;
                decimal fee = Math.Round(PlayerValuation.Calculate(player, club.Reputation) * 0.3m / 1000m) * 1000m;
                available.Add((player, club, Math.Max(2_000m, fee)));
            }
        }

        return available;
    }

    /// <summary>
    /// Executes a player purchase. Returns true if successful.
    /// </summary>
    public static bool ExecuteBuy(Club buyer, Club seller, Player player, decimal price)
    {
        if (buyer.Team.Players.Count >= MaxSquadSize) return false;
        if (buyer.Balance < price) return false;
        if (!seller.Team.Players.Contains(player)) return false;
        if (!CanRemovePlayer(seller, player)) return false;

        buyer.Balance -= price;
        seller.Balance += price;

        RemovePlayerFromClub(seller, player);
        AddPlayerToClub(buyer, player);

        return true;
    }

    /// <summary>
    /// Executes a player sale. Returns true if successful.
    /// </summary>
    public static bool ExecuteSell(Club seller, Club buyer, Player player, decimal price)
    {
        return ExecuteBuy(buyer, seller, player, price);
    }

    /// <summary>
    /// Executes a loan-in: player moves temporarily to the host club.
    /// Returns the LoanRecord if successful.
    /// </summary>
    public static LoanRecord? ExecuteLoanIn(Club host, Club source, Player player, decimal fee)
    {
        if (host.Team.Players.Count >= MaxSquadSize) return null;
        if (host.Balance < fee) return null;
        if (!source.Team.Players.Contains(player)) return null;
        if (!CanRemovePlayer(source, player)) return null;

        host.Balance -= fee;
        source.Balance += fee;

        RemovePlayerFromClub(source, player);
        AddPlayerToClub(host, player);

        return new LoanRecord
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            OriginClubId = source.Id,
            HostClubId = host.Id
        };
    }

    /// <summary>
    /// Executes a loan-out: player is sent to another club.
    /// Returns the LoanRecord if successful.
    /// </summary>
    public static LoanRecord? ExecuteLoanOut(Club source, Club host, Player player)
    {
        if (host.Team.Players.Count >= MaxSquadSize) return null;
        if (!source.Team.Players.Contains(player)) return null;
        if (!CanRemovePlayer(source, player)) return null;

        RemovePlayerFromClub(source, player);
        AddPlayerToClub(host, player);

        return new LoanRecord
        {
            PlayerId = player.Id,
            PlayerName = player.Name,
            OriginClubId = source.Id,
            HostClubId = host.Id
        };
    }

    /// <summary>
    /// Adds a free agent / youth recruit to a club.
    /// </summary>
    public static bool AddFreeAgent(Club club, Player player, decimal fee = 0m)
    {
        if (club.Team.Players.Count >= MaxSquadSize) return false;
        if (fee > 0 && club.Balance < fee) return false;

        if (fee > 0)
            club.Balance -= fee;

        AddPlayerToClub(club, player);
        return true;
    }

    /// <summary>
    /// Checks if a player can be removed from a club without violating rules.
    /// </summary>
    public static bool CanRemovePlayer(Club club, Player player)
    {
        if (club.Team.Players.Count <= MinSquadSize) return false;

        if (player.PrimaryPosition == Position.GK)
        {
            int gkCount = club.Team.Players.Count(p => p.PrimaryPosition == Position.GK);
            if (gkCount <= 1) return false;
        }

        return true;
    }

    private static void RemovePlayerFromClub(Club club, Player player)
    {
        var newPlayers = club.Team.Players.Where(p => p.Id != player.Id).ToList();
        var newLineup = club.Team.StartingLineup.Where(id => id != player.Id).ToList();

        // If we removed a starter, fill from reserves
        if (club.Team.StartingLineup.Contains(player.Id) && newPlayers.Count >= 11)
        {
            var lineupSet = new HashSet<int>(newLineup);
            var reserve = newPlayers.FirstOrDefault(p => !lineupSet.Contains(p.Id));
            if (reserve != null)
                newLineup.Add(reserve.Id);
        }

        club.Team = club.Team with { Players = newPlayers, StartingLineup = newLineup };
    }

    private static void AddPlayerToClub(Club club, Player player)
    {
        var newPlayers = club.Team.Players.ToList();
        newPlayers.Add(player);

        var newLineup = club.Team.StartingLineup.ToList();

        if (newLineup.Count < 11)
            newLineup.Add(player.Id);

        club.Team = club.Team with { Players = newPlayers, StartingLineup = newLineup };
    }
}
