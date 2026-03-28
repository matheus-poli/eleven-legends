using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Transfers;

/// <summary>
/// Simple AI logic for automated club transfer decisions during the window.
/// </summary>
public static class AITransferAgent
{
    /// <summary>
    /// Processes one transfer window day for all AI clubs.
    /// Each AI club may buy or sell one player per day.
    /// </summary>
    public static List<TransferRecord> ProcessDay(
        IReadOnlyList<Club> clubs, int playerClubId, IRng rng, ref int nextPlayerId)
    {
        var records = new List<TransferRecord>();

        foreach (var club in clubs)
        {
            if (club.Id == playerClubId) continue;

            // Try to buy if squad is small
            if (club.Team.Players.Count < 16 && club.Balance > 20_000m)
            {
                var available = TransferMarket.GetAvailablePlayers(clubs, club.Id);
                if (available.Count > 0)
                {
                    // Pick a random affordable player
                    var affordable = available
                        .Where(a => a.Price <= club.Balance * 0.3m)
                        .ToList();

                    if (affordable.Count > 0)
                    {
                        var pick = affordable[rng.NextInt(0, affordable.Count - 1)];
                        if (TransferMarket.ExecuteBuy(club, pick.Club, pick.Player, pick.Price))
                        {
                            records.Add(new TransferRecord
                            {
                                Type = Data.Enums.TransferType.Buy,
                                PlayerId = pick.Player.Id,
                                PlayerName = pick.Player.Name,
                                FromClubId = pick.Club.Id,
                                ToClubId = club.Id,
                                Fee = pick.Price
                            });
                        }
                    }
                }
            }

            // Try to sell if squad is large and player is below average
            if (club.Team.Players.Count > 20)
            {
                var sellable = TransferMarket.GetSellablePlayers(club);
                if (sellable.Count > 0)
                {
                    float avgOverall = club.Team.Players.Average(p =>
                        p.PrimaryPosition == Data.Enums.Position.GK
                            ? p.Attributes.GoalkeeperOverall
                            : p.Attributes.OutfieldOverall);

                    var weakest = sellable
                        .Where(s =>
                        {
                            float ovr = s.Player.PrimaryPosition == Data.Enums.Position.GK
                                ? s.Player.Attributes.GoalkeeperOverall
                                : s.Player.Attributes.OutfieldOverall;
                            return ovr < avgOverall;
                        })
                        .OrderBy(s => s.Price)
                        .FirstOrDefault();

                    if (weakest.Player != null)
                    {
                        // Find a buyer (any AI club with budget and space)
                        var buyer = clubs.FirstOrDefault(c =>
                            c.Id != club.Id && c.Id != playerClubId &&
                            c.Team.Players.Count < TransferMarket.MaxSquadSize &&
                            c.Balance >= weakest.Price);

                        if (buyer != null)
                        {
                            if (TransferMarket.ExecuteSell(club, buyer, weakest.Player, weakest.Price))
                            {
                                records.Add(new TransferRecord
                                {
                                    Type = Data.Enums.TransferType.Sell,
                                    PlayerId = weakest.Player.Id,
                                    PlayerName = weakest.Player.Name,
                                    FromClubId = club.Id,
                                    ToClubId = buyer.Id,
                                    Fee = weakest.Price
                                });
                            }
                        }
                    }
                }
            }
        }

        return records;
    }
}
