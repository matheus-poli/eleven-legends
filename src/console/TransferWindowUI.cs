using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.Transfers;

namespace ElevenLegends.Console;

/// <summary>
/// Console UI for the transfer window. Handles all player transfer interactions.
/// </summary>
public static class TransferWindowUI
{
    /// <summary>
    /// Runs one interactive transfer window day.
    /// Returns the list of transfers made by the player.
    /// </summary>
    public static List<TransferRecord> RunTransferDay(
        GameState gameState, Club playerClub, IReadOnlyList<Club> clubs, int dayNumber)
    {
        var records = new List<TransferRecord>();

        System.Console.WriteLine("\n╔══════════════════════════════════════╗");
        System.Console.WriteLine($"║    🔄 TRANSFER WINDOW — Day {dayNumber}/5    ║");
        System.Console.WriteLine("╚══════════════════════════════════════╝");

        PrintSquadSummary(playerClub);

        while (true)
        {
            System.Console.WriteLine("\n  📋 Transfer Actions:");
            System.Console.WriteLine("  1. 🛒 Buy Player");
            System.Console.WriteLine("  2. 💰 Sell Player");
            System.Console.WriteLine("  3. 📥 Loan In (Alugar)");
            System.Console.WriteLine("  4. 📤 Loan Out (Emprestar)");
            System.Console.WriteLine("  5. 🌱 Youth Academy");
            System.Console.WriteLine("  6. 🔍 Scout Region");
            System.Console.WriteLine("  7. 📊 View Squad");
            System.Console.WriteLine("  8. ⏭️  End Day");

            int choice = ReadChoice(1, 8);

            switch (choice)
            {
                case 1:
                    BuyPlayer(gameState, playerClub, clubs, records);
                    break;
                case 2:
                    SellPlayer(gameState, playerClub, clubs, records);
                    break;
                case 3:
                    LoanIn(gameState, playerClub, clubs, records);
                    break;
                case 4:
                    LoanOut(gameState, playerClub, clubs, records);
                    break;
                case 5:
                    YouthRecruit(gameState, playerClub, records);
                    break;
                case 6:
                    ScoutRegion(gameState, playerClub, records);
                    break;
                case 7:
                    PrintFullSquad(playerClub);
                    break;
                case 8:
                    return records;
            }
        }
    }

    private static void BuyPlayer(
        GameState gameState, Club playerClub, IReadOnlyList<Club> clubs, List<TransferRecord> records)
    {
        var available = TransferMarket.GetAvailablePlayers(clubs, playerClub.Id);
        if (available.Count == 0)
        {
            System.Console.WriteLine("\n  ❌ No players available for purchase.");
            return;
        }

        if (playerClub.Team.Players.Count >= TransferMarket.MaxSquadSize)
        {
            System.Console.WriteLine($"\n  ❌ Squad full ({TransferMarket.MaxSquadSize} players max).");
            return;
        }

        // Filter by position
        var posChoice = ChoosePositionFilter();
        var filtered = posChoice == null
            ? available
            : available.Where(a => a.Player.PrimaryPosition == posChoice.Value).ToList();

        if (filtered.Count == 0)
        {
            System.Console.WriteLine("\n  ❌ No players found for that position.");
            return;
        }

        // Sort by price and show top 10
        var sorted = filtered.OrderByDescending(a => GetOverall(a.Player)).Take(10).ToList();

        System.Console.WriteLine($"\n  🛒 Available Players (Budget: {playerClub.Balance:C0}):\n");
        for (int i = 0; i < sorted.Count; i++)
        {
            var (player, club, price) = sorted[i];
            float ovr = GetOverall(player);
            string affordable = playerClub.Balance >= price ? "✅" : "❌";
            System.Console.WriteLine(
                $"  {affordable} {i + 1,2}. {player.Name,-20} {player.PrimaryPosition,-4} Age:{player.Age,2} OVR:{ovr:F0} — {club.Name} — {price:C0}");
        }

        System.Console.WriteLine($"   0. Cancel");
        int pick = ReadChoice(0, sorted.Count);
        if (pick == 0) return;

        var chosen = sorted[pick - 1];
        if (TransferMarket.ExecuteBuy(playerClub, chosen.Club, chosen.Player, chosen.Price))
        {
            System.Console.WriteLine($"\n  ✅ Signed {chosen.Player.Name} from {chosen.Club.Name} for {chosen.Price:C0}!");
            records.Add(new TransferRecord
            {
                Type = TransferType.Buy,
                PlayerId = chosen.Player.Id,
                PlayerName = chosen.Player.Name,
                FromClubId = chosen.Club.Id,
                ToClubId = playerClub.Id,
                Fee = chosen.Price,
                Day = gameState.CurrentDay.Day
            });
            gameState.RecordTransfer(records[^1]);
        }
        else
        {
            System.Console.WriteLine("\n  ❌ Transfer failed (insufficient funds or squad limit).");
        }
    }

    private static void SellPlayer(
        GameState gameState, Club playerClub, IReadOnlyList<Club> clubs, List<TransferRecord> records)
    {
        var sellable = TransferMarket.GetSellablePlayers(playerClub);
        if (sellable.Count == 0)
        {
            System.Console.WriteLine($"\n  ❌ Cannot sell — squad at minimum ({TransferMarket.MinSquadSize}).");
            return;
        }

        System.Console.WriteLine("\n  💰 Players you can sell:\n");
        for (int i = 0; i < sellable.Count; i++)
        {
            var (player, price) = sellable[i];
            float ovr = GetOverall(player);
            bool isStarter = playerClub.Team.StartingLineup.Contains(player.Id);
            string tag = isStarter ? "★" : " ";
            System.Console.WriteLine(
                $"  {tag} {i + 1,2}. {player.Name,-20} {player.PrimaryPosition,-4} Age:{player.Age,2} OVR:{ovr:F0} — {price:C0}");
        }

        System.Console.WriteLine($"   0. Cancel");
        int pick = ReadChoice(0, sellable.Count);
        if (pick == 0) return;

        var chosen = sellable[pick - 1];

        // Find a buyer AI club
        var buyer = clubs.FirstOrDefault(c =>
            c.Id != playerClub.Id &&
            c.Team.Players.Count < TransferMarket.MaxSquadSize &&
            c.Balance >= chosen.Price);

        if (buyer == null)
        {
            System.Console.WriteLine("\n  ❌ No clubs interested at this price.");
            return;
        }

        if (TransferMarket.ExecuteSell(playerClub, buyer, chosen.Player, chosen.Price))
        {
            System.Console.WriteLine($"\n  ✅ Sold {chosen.Player.Name} to {buyer.Name} for {chosen.Price:C0}!");
            var record = new TransferRecord
            {
                Type = TransferType.Sell,
                PlayerId = chosen.Player.Id,
                PlayerName = chosen.Player.Name,
                FromClubId = playerClub.Id,
                ToClubId = buyer.Id,
                Fee = chosen.Price,
                Day = gameState.CurrentDay.Day
            };
            records.Add(record);
            gameState.RecordTransfer(record);
        }
    }

    private static void LoanIn(
        GameState gameState, Club playerClub, IReadOnlyList<Club> clubs, List<TransferRecord> records)
    {
        if (playerClub.Team.Players.Count >= TransferMarket.MaxSquadSize)
        {
            System.Console.WriteLine($"\n  ❌ Squad full ({TransferMarket.MaxSquadSize} max).");
            return;
        }

        var available = TransferMarket.GetLoanablePlayersIn(clubs, playerClub.Id);
        if (available.Count == 0)
        {
            System.Console.WriteLine("\n  ❌ No players available for loan.");
            return;
        }

        var sorted = available.OrderByDescending(a => GetOverall(a.Player)).Take(10).ToList();

        System.Console.WriteLine($"\n  📥 Loan Market (Budget: {playerClub.Balance:C0}):\n");
        for (int i = 0; i < sorted.Count; i++)
        {
            var (player, club, fee) = sorted[i];
            float ovr = GetOverall(player);
            string affordable = playerClub.Balance >= fee ? "✅" : "❌";
            System.Console.WriteLine(
                $"  {affordable} {i + 1,2}. {player.Name,-20} {player.PrimaryPosition,-4} Age:{player.Age,2} OVR:{ovr:F0} — {club.Name} — Loan fee: {fee:C0}");
        }

        System.Console.WriteLine($"   0. Cancel");
        int pick = ReadChoice(0, sorted.Count);
        if (pick == 0) return;

        var chosen = sorted[pick - 1];
        var loan = TransferMarket.ExecuteLoanIn(playerClub, chosen.Club, chosen.Player, chosen.Fee);
        if (loan != null)
        {
            System.Console.WriteLine($"\n  ✅ Loaned {chosen.Player.Name} from {chosen.Club.Name} (fee: {chosen.Fee:C0})!");
            gameState.RecordLoan(loan);
            var record = new TransferRecord
            {
                Type = TransferType.LoanIn,
                PlayerId = chosen.Player.Id,
                PlayerName = chosen.Player.Name,
                FromClubId = chosen.Club.Id,
                ToClubId = playerClub.Id,
                Fee = chosen.Fee,
                Day = gameState.CurrentDay.Day
            };
            records.Add(record);
            gameState.RecordTransfer(record);
        }
        else
        {
            System.Console.WriteLine("\n  ❌ Loan failed.");
        }
    }

    private static void LoanOut(
        GameState gameState, Club playerClub, IReadOnlyList<Club> clubs, List<TransferRecord> records)
    {
        var sellable = TransferMarket.GetSellablePlayers(playerClub);
        if (sellable.Count == 0)
        {
            System.Console.WriteLine($"\n  ❌ Cannot loan out — squad at minimum.");
            return;
        }

        System.Console.WriteLine("\n  📤 Select player to loan out:\n");
        for (int i = 0; i < sellable.Count; i++)
        {
            var (player, _) = sellable[i];
            float ovr = GetOverall(player);
            System.Console.WriteLine(
                $"  {i + 1,2}. {player.Name,-20} {player.PrimaryPosition,-4} Age:{player.Age,2} OVR:{ovr:F0}");
        }

        System.Console.WriteLine($"   0. Cancel");
        int pick = ReadChoice(0, sellable.Count);
        if (pick == 0) return;

        var chosen = sellable[pick - 1];

        // Find a host club
        var host = clubs.FirstOrDefault(c =>
            c.Id != playerClub.Id &&
            c.Team.Players.Count < TransferMarket.MaxSquadSize);

        if (host == null)
        {
            System.Console.WriteLine("\n  ❌ No clubs want to take this player on loan.");
            return;
        }

        var loan = TransferMarket.ExecuteLoanOut(playerClub, host, chosen.Player);
        if (loan != null)
        {
            System.Console.WriteLine($"\n  ✅ Loaned {chosen.Player.Name} to {host.Name}!");
            gameState.RecordLoan(loan);
            var record = new TransferRecord
            {
                Type = TransferType.LoanOut,
                PlayerId = chosen.Player.Id,
                PlayerName = chosen.Player.Name,
                FromClubId = playerClub.Id,
                ToClubId = host.Id,
                Fee = 0m,
                Day = gameState.CurrentDay.Day
            };
            records.Add(record);
            gameState.RecordTransfer(record);
        }
    }

    private static void YouthRecruit(
        GameState gameState, Club playerClub, List<TransferRecord> records)
    {
        if (playerClub.Team.Players.Count >= TransferMarket.MaxSquadSize)
        {
            System.Console.WriteLine($"\n  ❌ Squad full ({TransferMarket.MaxSquadSize} max).");
            return;
        }

        int nextId = gameState.GetNextPlayerId(3);
        var rng = new SeededRng(gameState.CurrentDay.Day * 7 + playerClub.Id);
        var prospects = YouthAcademy.GenerateProspects(rng, playerClub.Country, nextId);

        System.Console.WriteLine("\n  🌱 YOUTH ACADEMY — Choose 1 prospect:\n");
        for (int i = 0; i < prospects.Count; i++)
        {
            var (prospect, fee) = prospects[i];
            float ovr = GetOverall(prospect);
            string affordable = playerClub.Balance >= fee ? "✅" : "❌";
            System.Console.WriteLine(
                $"  {affordable} {i + 1}. {prospect.Name,-20} {prospect.PrimaryPosition,-4} Age:{prospect.Age} OVR:{ovr:F0} — Fee: {fee:C0}");
        }

        System.Console.WriteLine($"   0. Pass (don't recruit)");
        int pick = ReadChoice(0, prospects.Count);
        if (pick == 0) return;

        var chosen = prospects[pick - 1];
        if (TransferMarket.AddFreeAgent(playerClub, chosen.Prospect, chosen.Fee))
        {
            System.Console.WriteLine($"\n  ✅ Recruited {chosen.Prospect.Name} from youth academy for {chosen.Fee:C0}!");
            var record = new TransferRecord
            {
                Type = TransferType.YouthRecruit,
                PlayerId = chosen.Prospect.Id,
                PlayerName = chosen.Prospect.Name,
                ToClubId = playerClub.Id,
                Fee = chosen.Fee,
                Day = gameState.CurrentDay.Day
            };
            records.Add(record);
            gameState.RecordTransfer(record);
        }
        else
        {
            System.Console.WriteLine("\n  ❌ Recruitment failed (insufficient funds or squad full).");
        }
    }

    private static void ScoutRegion(
        GameState gameState, Club playerClub, List<TransferRecord> records)
    {
        if (playerClub.Team.Players.Count >= TransferMarket.MaxSquadSize)
        {
            System.Console.WriteLine($"\n  ❌ Squad full ({TransferMarket.MaxSquadSize} max).");
            return;
        }

        var regions = ScoutingSystem.GetRegions();

        System.Console.WriteLine($"\n  🔍 SCOUT REGIONS (Budget: {playerClub.Balance:C0}):\n");
        for (int i = 0; i < regions.Count; i++)
        {
            string affordable = playerClub.Balance >= regions[i].Cost ? "✅" : "❌";
            System.Console.WriteLine($"  {affordable} {i + 1}. {regions[i].Name,-15} — Cost: {regions[i].Cost:C0}");
        }

        System.Console.WriteLine($"   0. Cancel");
        int regionChoice = ReadChoice(0, regions.Count);
        if (regionChoice == 0) return;

        var region = regions[regionChoice - 1];
        if (playerClub.Balance < region.Cost)
        {
            System.Console.WriteLine("\n  ❌ Not enough funds.");
            return;
        }

        playerClub.Balance -= region.Cost;
        int nextId = gameState.GetNextPlayerId(5);
        var rng = new SeededRng(gameState.CurrentDay.Day * 13 + regionChoice);
        var freeAgents = ScoutingSystem.Scout(rng, region, nextId);

        System.Console.WriteLine($"\n  Scouting report from {region.Name} — {freeAgents.Count} players found:\n");
        for (int i = 0; i < freeAgents.Count; i++)
        {
            (Player player, decimal signFee) = freeAgents[i];
            float ovr = GetOverall(player);
            string feeText = signFee > 0 ? $"Fee:{signFee / 1000:F0}K" : "Free";
            System.Console.WriteLine(
                $"  {i + 1}. {player.Name,-20} {player.PrimaryPosition,-4} Age:{player.Age,2} OVR:{ovr:F0} {feeText}");
        }

        System.Console.WriteLine($"   0. Don't recruit anyone");
        int pick = ReadChoice(0, freeAgents.Count);
        if (pick == 0) return;

        (Player chosen, decimal chosenFee) = freeAgents[pick - 1];
        if (TransferMarket.AddFreeAgent(playerClub, chosen, chosenFee))
        {
            System.Console.WriteLine($"\n  Recruited {chosen.Name}!");
            var record = new TransferRecord
            {
                Type = TransferType.ScoutRecruit,
                PlayerId = chosen.Id,
                PlayerName = chosen.Name,
                ToClubId = playerClub.Id,
                Fee = chosenFee,
                Day = gameState.CurrentDay.Day
            };
            records.Add(record);
            gameState.RecordTransfer(record);
        }
        else
        {
            System.Console.WriteLine("\n  ❌ Recruitment failed (squad full).");
        }
    }

    private static void PrintSquadSummary(Club club)
    {
        System.Console.WriteLine($"\n  📊 {club.Name} — {club.Team.Players.Count} players — 💰 {club.Balance:C0}");

        var positions = club.Team.Players
            .GroupBy(p => GetPositionGroup(p.PrimaryPosition))
            .OrderBy(g => g.Key);

        foreach (var group in positions)
        {
            System.Console.WriteLine($"    {group.Key}: {group.Count()} players");
        }
    }

    private static void PrintFullSquad(Club club)
    {
        System.Console.WriteLine($"\n  📋 Full Squad — {club.Name} ({club.Team.Players.Count} players):\n");

        var starterSet = new HashSet<int>(club.Team.StartingLineup);

        foreach (var player in club.Team.Players.OrderBy(p => GetPositionOrder(p.PrimaryPosition)))
        {
            float ovr = GetOverall(player);
            decimal value = PlayerValuation.Calculate(player, club.Reputation);
            string tag = starterSet.Contains(player.Id) ? "★" : " ";
            System.Console.WriteLine(
                $"  {tag} {player.Name,-20} {player.PrimaryPosition,-4} Age:{player.Age,2} OVR:{ovr:F0} Value:{value:C0}");
        }
    }

    private static Position? ChoosePositionFilter()
    {
        System.Console.WriteLine("\n  Filter by position:");
        System.Console.WriteLine("  1. All positions");
        System.Console.WriteLine("  2. Goalkeepers");
        System.Console.WriteLine("  3. Defenders");
        System.Console.WriteLine("  4. Midfielders");
        System.Console.WriteLine("  5. Attackers");

        int choice = ReadChoice(1, 5);
        return choice switch
        {
            2 => Position.GK,
            3 => Position.CB,
            4 => Position.CM,
            5 => Position.ST,
            _ => null
        };
    }

    private static float GetOverall(Player player) =>
        player.PrimaryPosition == Position.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

    private static string GetPositionGroup(Position pos) => pos switch
    {
        Position.GK => "GK",
        Position.CB or Position.LB or Position.RB or Position.LWB or Position.RWB => "DEF",
        Position.CDM or Position.CM or Position.CAM or Position.LM or Position.RM => "MID",
        _ => "ATK"
    };

    private static int GetPositionOrder(Position pos) => pos switch
    {
        Position.GK => 0,
        Position.CB or Position.LB or Position.RB => 1,
        Position.CDM or Position.CM or Position.CAM or Position.LM or Position.RM => 2,
        _ => 3
    };

    private static int ReadChoice(int min, int max)
    {
        while (true)
        {
            System.Console.Write($"\n  Choose ({min}-{max}): ");
            if (int.TryParse(System.Console.ReadLine(), out int choice)
                && choice >= min && choice <= max)
                return choice;
            System.Console.WriteLine("  Invalid choice.");
        }
    }
}
