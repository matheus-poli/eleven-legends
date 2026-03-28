using Godot;
using ElevenLegends.Data.Enums;
using PlayerPosition = ElevenLegends.Data.Enums.Position;
using ElevenLegends.Data.Models;
using ElevenLegends.Transfers;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Transfer Window — buy, sell, youth academy, scouting with filters and player details.
/// </summary>
public partial class TransferWindow : Control
{
    private GameState _gameState = null!;
    private Club _playerClub = null!;
    private string _activeTab = "buy";
    private bool _youthGenerated;

    // Buy/Sell filters
    private string _filterPosition = "All";
    private string _filterCountry = "All";
    private int _filterMinOvr;
    private string _sortBy = "OVR";

    public override void _Ready()
    {
        _gameState = SceneManager.Instance.CurrentGameState!;
        _playerClub = _gameState.PlayerClub;
        BuildUI();
    }

    private void BuildUI()
    {
        foreach (Node child in GetChildren())
            child.QueueFree();

        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = UITheme.PaddingLarge;
        root.OffsetRight = -UITheme.PaddingLarge;
        root.OffsetTop = UITheme.PaddingSmall;
        root.OffsetBottom = -UITheme.PaddingSmall;
        root.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        AddChild(root);

        // ─── Header ──────────────────────────────────────────────
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", UITheme.Padding);
        root.AddChild(headerRow);

        headerRow.AddChild(UITheme.CreateLabel("Transfer Window",
            UITheme.FontSizeHeading, UITheme.Orange));

        headerRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        int squadCount = _playerClub.Team.Players.Count;
        Color squadColor = squadCount >= TransferMarket.MaxSquadSize ? UITheme.Red : UITheme.Blue;
        headerRow.AddChild(UITheme.CreateLabel(
            $"Budget: {FormatMoney(_playerClub.Balance)}  |  Squad: {squadCount}/{TransferMarket.MaxSquadSize}",
            UITheme.FontSizeSmall, squadColor));

        // ─── Tab bar ─────────────────────────────────────────────
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 6);
        root.AddChild(tabs);

        (string id, string label, Color color)[] tabDefs =
        [
            ("buy", "Buy", UITheme.Green),
            ("sell", "Sell", UITheme.Red),
            ("youth", "Youth", UITheme.Blue),
            ("scout", "Scout", UITheme.Purple),
        ];

        foreach ((string id, string label, Color color) in tabDefs)
        {
            bool active = _activeTab == id;
            var btn = UITheme.CreateFlatButton(label,
                active ? color : UITheme.Border,
                active ? UITheme.TextLight : UITheme.TextPrimary);
            btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            btn.CustomMinimumSize = new Vector2(0, 36);
            string captured = id;
            btn.Pressed += () => { _activeTab = captured; BuildUI(); };
            tabs.AddChild(btn);
        }

        // ─── Filter bar (for buy/sell tabs) ──────────────────────
        if (_activeTab is "buy" or "sell")
        {
            // Position filter row
            var posRow = new HBoxContainer();
            posRow.AddThemeConstantOverride("separation", 3);
            root.AddChild(posRow);

            string[] posOptions = ["All", "GK", "CB", "LB", "RB", "CDM", "CM", "CAM", "LM", "RM", "LW", "RW", "ST", "CF"];
            foreach (string posOpt in posOptions)
            {
                bool active = _filterPosition == posOpt;
                var posBtn = UITheme.CreateFlatButton(posOpt,
                    active ? UITheme.Blue : UITheme.Border,
                    active ? UITheme.TextLight : UITheme.TextSecondary);
                posBtn.CustomMinimumSize = new Vector2(0, 26);
                posBtn.AddThemeFontSizeOverride("font_size", 11);
                string cap = posOpt;
                posBtn.Pressed += () => { _filterPosition = cap; BuildUI(); };
                posRow.AddChild(posBtn);
            }

            // Country filter row
            var countryRow = new HBoxContainer();
            countryRow.AddThemeConstantOverride("separation", 4);
            root.AddChild(countryRow);

            string[] countries = ["All", "Brasilândia", "Hispânia", "Angleterre", "Itália Nova"];
            foreach (string country in countries)
            {
                string display = country == "All" ? "All Countries" : country;
                bool active = _filterCountry == country;
                var cBtn = UITheme.CreateFlatButton(display,
                    active ? UITheme.Orange : UITheme.Border,
                    active ? UITheme.TextLight : UITheme.TextSecondary);
                cBtn.CustomMinimumSize = new Vector2(0, 26);
                cBtn.AddThemeFontSizeOverride("font_size", 11);
                string cap = country;
                cBtn.Pressed += () => { _filterCountry = cap; BuildUI(); };
                countryRow.AddChild(cBtn);
            }
        }

        // ─── Content area ────────────────────────────────────────
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        root.AddChild(scroll);

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        content.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(content);

        switch (_activeTab)
        {
            case "buy": BuildBuyTab(content); break;
            case "sell": BuildSellTab(content); break;
            case "youth": BuildYouthTab(content); break;
            case "scout": BuildScoutTab(content); break;
        }

        // ─── Bottom actions ──────────────────────────────────────
        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", UITheme.Padding);
        root.AddChild(bottomRow);

        var backBtn = UITheme.CreateButton("Back to Hub", UITheme.Green);
        backBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        backBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        bottomRow.AddChild(backBtn);

        var squadBtn = UITheme.CreateButton("Squad", UITheme.BlueDark);
        squadBtn.Pressed += () =>
        {
            Squad.ReturnScene = "res://scenes/TransferWindow.tscn";
            SceneManager.Instance.ChangeScene("res://scenes/Squad.tscn");
        };
        bottomRow.AddChild(squadBtn);

        Anim.StaggerChildren(content, stagger: 0.02f, useScale: false);
    }

    // ─── Buy tab ──────────────────────────────────────────────────────

    private void BuildBuyTab(VBoxContainer content)
    {
        var available = TransferMarket.GetAvailablePlayers(
            _gameState.Clubs.ToList(), excludeClubId: _playerClub.Id);

        var filtered = FilterPlayers(available.Select(a =>
            (a.Player, a.Club, a.Price)).ToList());

        if (filtered.Count == 0)
        {
            content.AddChild(UITheme.CreateLabel("No players match filters.",
                UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center));
            return;
        }

        content.AddChild(UITheme.CreateLabel($"{filtered.Count} players available",
            UITheme.FontSizeCaption, UITheme.TextSecondary));

        bool squadFull = _playerClub.Team.Players.Count >= TransferMarket.MaxSquadSize;
        foreach ((Player player, Club club, decimal price) in filtered.Take(50))
        {
            content.AddChild(CreatePlayerCard(player, club, price, "buy", squadFull));
        }
    }

    // ─── Sell tab ─────────────────────────────────────────────────────

    private void BuildSellTab(VBoxContainer content)
    {
        var sellable = TransferMarket.GetSellablePlayers(_playerClub);

        if (sellable.Count == 0)
        {
            content.AddChild(UITheme.CreateLabel("No players can be sold (minimum squad).",
                UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center));
            return;
        }

        var filtered = FilterPlayers(sellable.Select(s =>
            (s.Player, _playerClub, s.Price)).ToList());

        foreach ((Player player, Club club, decimal price) in filtered)
        {
            content.AddChild(CreatePlayerCard(player, club, price, "sell", false));
        }
    }

    // ─── Youth tab ────────────────────────────────────────────────────

    private void BuildYouthTab(VBoxContainer content)
    {
        content.AddChild(UITheme.CreateLabel("Youth Academy",
            UITheme.FontSizeHeading, UITheme.Blue));

        if (_youthGenerated)
        {
            content.AddChild(UITheme.CreateLabel(
                "Already generated prospects this transfer window. Come back next season.",
                UITheme.FontSizeSmall, UITheme.TextSecondary));
            return;
        }

        decimal generationCost = 10_000m;
        content.AddChild(UITheme.CreateLabel(
            $"Generate 3 youth prospects. Cost: {FormatMoney(generationCost)}. Limit: 1 per transfer window.",
            UITheme.FontSizeSmall, UITheme.TextSecondary));

        bool canAfford = _playerClub.Balance >= generationCost;
        bool squadFull = _playerClub.Team.Players.Count >= TransferMarket.MaxSquadSize;

        var generateBtn = UITheme.CreateButton(
            canAfford ? "Generate Prospects" : "Not enough budget",
            canAfford ? UITheme.Green : UITheme.Border,
            canAfford ? UITheme.TextLight : UITheme.TextSecondary);
        generateBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        generateBtn.Disabled = !canAfford;

        generateBtn.Pressed += () =>
        {
            if (!canAfford || _youthGenerated) return;

            _playerClub.Balance -= generationCost;
            _youthGenerated = true;

            int nextId = _gameState.GetNextPlayerId(3);
            var rng = new Simulation.SeededRng(nextId);
            var prospects = YouthAcademy.GenerateProspects(rng, _playerClub.Country, nextId);

            foreach (Node child in content.GetChildren())
                child.QueueFree();

            content.AddChild(UITheme.CreateLabel("Choose a prospect to recruit:",
                UITheme.FontSizeHeading, UITheme.Blue));

            foreach (var p in prospects)
            {
                content.AddChild(CreateProspectCard(p, squadFull));
            }
        };
        content.AddChild(generateBtn);
    }

    // ─── Scout tab ────────────────────────────────────────────────────

    private void BuildScoutTab(VBoxContainer content)
    {
        content.AddChild(UITheme.CreateLabel("Scouting Regions",
            UITheme.FontSizeHeading, UITheme.Purple));
        content.AddChild(UITheme.CreateLabel(
            "Higher-tier leagues cost more to scout but yield better players.",
            UITheme.FontSizeCaption, UITheme.TextSecondary));

        var regions = ScoutingSystem.GetRegions();
        bool squadFull = _playerClub.Team.Players.Count >= TransferMarket.MaxSquadSize;

        foreach (var region in regions)
        {
            var card = HoverCard.Create(UITheme.Purple);
            card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            card.DisableTilt = true;
            content.AddChild(card);

            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", UITheme.Padding);
            card.AddChild(hbox);

            var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            info.AddChild(UITheme.CreateLabel(region.Name,
                UITheme.FontSizeBody, UITheme.TextDark));
            info.AddChild(UITheme.CreateLabel($"Scout cost: {FormatMoney(region.Cost)}",
                UITheme.FontSizeSmall, UITheme.TextSecondary));
            hbox.AddChild(info);

            bool canAfford = _playerClub.Balance >= region.Cost;
            var scoutBtn = UITheme.CreateFlatButton(
                canAfford ? "Scout" : "No budget",
                canAfford ? UITheme.Purple : UITheme.Border,
                canAfford ? UITheme.TextLight : UITheme.TextSecondary);
            scoutBtn.Disabled = !canAfford;

            var capturedRegion = region;
            scoutBtn.Pressed += () =>
            {
                if (_playerClub.Balance < capturedRegion.Cost) return;

                _playerClub.Balance -= capturedRegion.Cost;
                int nextId = _gameState.GetNextPlayerId(5);
                var rng = new Simulation.SeededRng(nextId);
                var scoutResults = ScoutingSystem.Scout(rng, capturedRegion, nextId);

                foreach (Node child in content.GetChildren())
                    child.QueueFree();

                content.AddChild(UITheme.CreateLabel(
                    $"Scouted {scoutResults.Count} players from {capturedRegion.Name}:",
                    UITheme.FontSizeHeading, UITheme.Purple));

                foreach ((Player player, decimal signFee) in scoutResults)
                {
                    content.AddChild(CreateScoutedPlayerCard(player, signFee, squadFull));
                }
            };
            hbox.AddChild(scoutBtn);
        }
    }

    // ─── Filter logic ─────────────────────────────────────────────────

    private List<(Player Player, Club Club, decimal Price)> FilterPlayers(
        List<(Player Player, Club Club, decimal Price)> players)
    {
        IEnumerable<(Player Player, Club Club, decimal Price)> result = players;

        // Position filter (exact match)
        if (_filterPosition != "All")
        {
            if (System.Enum.TryParse<PlayerPosition>(_filterPosition, out PlayerPosition filterPos))
            {
                result = result.Where(p => p.Player.PrimaryPosition == filterPos);
            }
        }

        // Country filter
        if (_filterCountry != "All")
        {
            result = result.Where(p => p.Club.Country == _filterCountry);
        }

        // Sort
        result = _sortBy switch
        {
            "Price" => result.OrderBy(p => p.Price),
            "Age" => result.OrderBy(p => p.Player.Age),
            _ => result.OrderByDescending(p => p.Player.PrimaryPosition == PlayerPosition.GK
                ? p.Player.Attributes.GoalkeeperOverall
                : p.Player.Attributes.OutfieldOverall),
        };

        return result.ToList();
    }

    // ─── Card factories ───────────────────────────────────────────────

    private HoverCard CreatePlayerCard(Player player, Club club, decimal price,
        string action, bool squadFull)
    {
        var card = HoverCard.Create();
        card.DisableTilt = true;
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        card.AddChild(hbox);

        float ovr = player.PrimaryPosition == PlayerPosition.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        // OVR badge
        hbox.AddChild(UITheme.CreateBadge($"{ovr:F0}",
            UITheme.RatingColor(ovr), UITheme.TextDark,
            UITheme.FontSizeSmall, new Vector2(38, 32)));

        // Info block
        var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        info.AddThemeConstantOverride("separation", 1);
        info.AddChild(UITheme.CreateLabel(player.Name, UITheme.FontSizeSmall, UITheme.TextDark));

        var detailText = $"{player.PrimaryPosition} | Age {player.Age} | {club.Name}";
        info.AddChild(UITheme.CreateLabel(detailText, 11, UITheme.TextSecondary));

        // Key stats row
        var statsRow = new HBoxContainer();
        statsRow.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        PlayerAttributes attrs = player.Attributes;

        if (player.PrimaryPosition == PlayerPosition.GK)
        {
            AddStatChip(statsRow, "REF", attrs.Reflexes);
            AddStatChip(statsRow, "HAN", attrs.Handling);
            AddStatChip(statsRow, "POS", attrs.GkPositioning);
        }
        else
        {
            AddStatChip(statsRow, "PAC", (attrs.Speed + attrs.Acceleration) / 2);
            AddStatChip(statsRow, "SHO", attrs.Finishing);
            AddStatChip(statsRow, "PAS", attrs.Passing);
            AddStatChip(statsRow, "DRI", attrs.Dribbling);
            AddStatChip(statsRow, "DEF", (attrs.Positioning + attrs.Anticipation) / 2);
            AddStatChip(statsRow, "PHY", (attrs.Strength + attrs.Stamina) / 2);
        }
        info.AddChild(statsRow);
        hbox.AddChild(info);

        // Price
        hbox.AddChild(UITheme.CreateLabel(FormatMoney(price),
            UITheme.FontSizeSmall, UITheme.Green));

        // Action button
        if (action == "buy")
        {
            bool canBuy = !squadFull && _playerClub.Balance >= price;
            var buyBtn = UITheme.CreateFlatButton("Buy",
                canBuy ? UITheme.Green : UITheme.Border,
                canBuy ? UITheme.TextLight : UITheme.TextSecondary);
            buyBtn.Disabled = !canBuy;
            buyBtn.Pressed += () =>
            {
                if (TransferMarket.ExecuteBuy(_playerClub, club, player, price))
                {
                    _gameState.RecordTransfer(new TransferRecord
                    {
                        Type = TransferType.Buy,
                        PlayerId = player.Id,
                        PlayerName = player.Name,
                        FromClubId = club.Id,
                        ToClubId = _playerClub.Id,
                        Fee = price,
                        Day = _gameState.CurrentDay.Day,
                    });
                    BuildUI();
                }
            };
            hbox.AddChild(buyBtn);
        }
        else
        {
            var sellBtn = UITheme.CreateFlatButton("Sell", UITheme.Red);
            sellBtn.Pressed += () =>
            {
                Club? buyer = _gameState.Clubs.FirstOrDefault(c =>
                    c.Id != _playerClub.Id && c.Balance >= price &&
                    c.Team.Players.Count < TransferMarket.MaxSquadSize);

                if (buyer != null && TransferMarket.ExecuteBuy(buyer, _playerClub, player, price))
                {
                    _gameState.RecordTransfer(new TransferRecord
                    {
                        Type = TransferType.Sell,
                        PlayerId = player.Id,
                        PlayerName = player.Name,
                        FromClubId = _playerClub.Id,
                        ToClubId = buyer.Id,
                        Fee = price,
                        Day = _gameState.CurrentDay.Day,
                    });
                    BuildUI();
                }
            };
            hbox.AddChild(sellBtn);
        }

        return card;
    }

    private static void AddStatChip(HBoxContainer row, string label, int value)
    {
        var lbl = UITheme.CreateLabel($"{label} {value}",
            10, UITheme.StatColor(value));
        row.AddChild(lbl);
    }

    private HoverCard CreateProspectCard((Player Prospect, decimal Fee) prospect, bool squadFull)
    {
        var card = HoverCard.Create(UITheme.Blue);
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        card.DisableTilt = true;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.Padding);
        card.AddChild(hbox);

        Player p = prospect.Prospect;
        float ovr = p.PrimaryPosition == PlayerPosition.GK
            ? p.Attributes.GoalkeeperOverall
            : p.Attributes.OutfieldOverall;

        hbox.AddChild(UITheme.CreateBadge($"{ovr:F0}",
            UITheme.StatColor((int)ovr), UITheme.TextLight,
            UITheme.FontSizeBody, new Vector2(44, 40)));

        var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        info.AddChild(UITheme.CreateLabel(p.Name, UITheme.FontSizeBody, UITheme.TextDark));
        info.AddChild(UITheme.CreateLabel(
            $"{p.PrimaryPosition} | Age {p.Age} | Fee: {FormatMoney(prospect.Fee)}",
            UITheme.FontSizeCaption, UITheme.TextSecondary));
        hbox.AddChild(info);

        bool canRecruit = !squadFull && _playerClub.Balance >= prospect.Fee;
        var recruitBtn = UITheme.CreateFlatButton("Recruit",
            canRecruit ? UITheme.Green : UITheme.Border);
        recruitBtn.Disabled = !canRecruit;
        recruitBtn.Pressed += () =>
        {
            if (_playerClub.Balance >= prospect.Fee)
            {
                TransferMarket.AddFreeAgent(_playerClub, p, prospect.Fee);
                _gameState.RecordTransfer(new TransferRecord
                {
                    Type = TransferType.YouthRecruit,
                    PlayerId = p.Id,
                    PlayerName = p.Name,
                    ToClubId = _playerClub.Id,
                    Fee = prospect.Fee,
                    Day = _gameState.CurrentDay.Day,
                });
                BuildUI();
            }
        };
        hbox.AddChild(recruitBtn);

        return card;
    }

    private HoverCard CreateScoutedPlayerCard(Player player, decimal signFee, bool squadFull)
    {
        var card = HoverCard.Create(UITheme.Purple);
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        card.DisableTilt = true;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.Padding);
        card.AddChild(hbox);

        float ovr = player.PrimaryPosition == PlayerPosition.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        hbox.AddChild(UITheme.CreateBadge($"{ovr:F0}",
            UITheme.StatColor((int)ovr), UITheme.TextLight,
            UITheme.FontSizeBody, new Vector2(44, 40)));

        var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        info.AddChild(UITheme.CreateLabel(player.Name,
            UITheme.FontSizeBody, UITheme.TextDark));

        string feeText = signFee > 0 ? $"Sign fee: {FormatMoney(signFee)}" : "Free agent";
        info.AddChild(UITheme.CreateLabel(
            $"{player.PrimaryPosition} | Age {player.Age} | {feeText}",
            UITheme.FontSizeCaption, UITheme.TextSecondary));
        hbox.AddChild(info);

        bool canSign = !squadFull && (signFee == 0 || _playerClub.Balance >= signFee);
        string btnText = signFee > 0 ? $"Sign ({FormatMoney(signFee)})" : "Sign (Free)";
        var signBtn = UITheme.CreateFlatButton(btnText,
            canSign ? UITheme.Green : UITheme.Border);
        signBtn.Disabled = !canSign;
        signBtn.Pressed += () =>
        {
            if (signFee > 0 && _playerClub.Balance < signFee) return;

            TransferMarket.AddFreeAgent(_playerClub, player, signFee);
            _gameState.RecordTransfer(new TransferRecord
            {
                Type = TransferType.ScoutRecruit,
                PlayerId = player.Id,
                PlayerName = player.Name,
                ToClubId = _playerClub.Id,
                Fee = signFee,
                Day = _gameState.CurrentDay.Day,
            });
            BuildUI();
        };
        hbox.AddChild(signBtn);

        return card;
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private static string FormatMoney(decimal amount)
    {
        return amount switch
        {
            >= 1_000_000 => $"{amount / 1_000_000:F1}M",
            >= 1_000 => $"{amount / 1_000:F0}K",
            _ => $"{amount:F0}",
        };
    }
}
