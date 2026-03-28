using Godot;
using ElevenLegends.Data.Enums;
using PlayerPosition = ElevenLegends.Data.Enums.Position;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.Transfers;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Transfer Window — buy, sell, youth academy, scouting with Duolingo-style cards.
/// </summary>
public partial class TransferWindowScreen : Control
{
    private GameState _gameState = null!;
    private Club _playerClub = null!;
    private string _activeTab = "buy";

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

        var root = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = UITheme.PaddingLarge,
            OffsetRight = -UITheme.PaddingLarge,
            OffsetTop = UITheme.Padding,
            OffsetBottom = -UITheme.Padding,
        };
        root.AddThemeConstantOverride("separation", UITheme.Padding);
        AddChild(root);

        // ─── Header card ──────────────────────────────────────────
        var headerCard = UITheme.CreateCard(UITheme.Orange);
        root.AddChild(headerCard);

        var headerVbox = new VBoxContainer();
        headerVbox.AddThemeConstantOverride("separation", 4);
        headerCard.AddChild(headerVbox);

        headerVbox.AddChild(UITheme.CreateLabel("Transfer Window",
            UITheme.FontSizeTitle, UITheme.Orange, HorizontalAlignment.Center));
        headerVbox.AddChild(UITheme.CreateLabel(
            $"Budget: {FormatMoney(_playerClub.Balance)}  •  Squad: {_playerClub.Team.Players.Count}",
            UITheme.FontSizeSmall, UITheme.TextSecondary, HorizontalAlignment.Center));

        // ─── Tab bar ──────────────────────────────────────────────
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 8);
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
            string captured = id;
            btn.Pressed += () => { _activeTab = captured; BuildUI(); };
            tabs.AddChild(btn);
        }

        // ─── Content area ─────────────────────────────────────────
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        root.AddChild(scroll);

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        content.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(content);

        switch (_activeTab)
        {
            case "buy": BuildBuyTab(content); break;
            case "sell": BuildSellTab(content); break;
            case "youth": BuildYouthTab(content); break;
            case "scout": BuildScoutTab(content); break;
        }

        // ─── Bottom actions ───────────────────────────────────────
        var bottomRow = new HBoxContainer();
        bottomRow.AddThemeConstantOverride("separation", UITheme.Padding);
        root.AddChild(bottomRow);

        var doneBtn = UITheme.CreateButton("Done — Advance Day", UITheme.Green);
        doneBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        doneBtn.Pressed += OnDone;
        bottomRow.AddChild(doneBtn);

        var squadBtn = UITheme.CreateButton("Squad", UITheme.BlueDark);
        squadBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/Squad.tscn");
        bottomRow.AddChild(squadBtn);

        Anim.StaggerChildren(content, stagger: 0.03f, useScale: false);
    }

    // ─── Buy tab ──────────────────────────────────────────────────────

    private void BuildBuyTab(VBoxContainer content)
    {
        var available = TransferMarket.GetAvailablePlayers(
            _gameState.Clubs.ToList(), excludeClubId: _playerClub.Id);

        if (available.Count == 0)
        {
            content.AddChild(UITheme.CreateLabel("No players available.",
                UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center));
            return;
        }

        foreach ((Player player, Club club, decimal price) in available.Take(20))
        {
            content.AddChild(CreateTransferCard(player, club, "buy"));
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

        foreach ((Player player, decimal price) in sellable)
        {
            content.AddChild(CreateTransferCard(player, _playerClub, "sell"));
        }
    }

    // ─── Youth tab ────────────────────────────────────────────────────

    private void BuildYouthTab(VBoxContainer content)
    {
        content.AddChild(UITheme.CreateLabel("Youth Academy",
            UITheme.FontSizeHeading, UITheme.Blue));
        content.AddChild(UITheme.CreateLabel(
            "Generate 3 youth prospects. Pick one to join your squad.",
            UITheme.FontSizeSmall, UITheme.TextSecondary));

        var generateBtn = UITheme.CreateButton("Generate Prospects", UITheme.Green);
        generateBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        generateBtn.Pressed += () =>
        {
            int nextId = _gameState.GetNextPlayerId(3);
            var rng = new Simulation.SeededRng(nextId);
            var prospects = YouthAcademy.GenerateProspects(rng, _playerClub.Country, nextId);

            foreach (Node child in content.GetChildren())
                child.QueueFree();

            content.AddChild(UITheme.CreateLabel("Choose a prospect:",
                UITheme.FontSizeHeading, UITheme.Blue));

            foreach (var p in prospects)
            {
                var card = CreateProspectCard(p);
                content.AddChild(card);
            }
        };
        content.AddChild(generateBtn);
    }

    // ─── Scout tab ────────────────────────────────────────────────────

    private void BuildScoutTab(VBoxContainer content)
    {
        content.AddChild(UITheme.CreateLabel("Scouting Regions",
            UITheme.FontSizeHeading, UITheme.Purple));

        var regions = ScoutingSystem.GetRegions();
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
            info.AddChild(UITheme.CreateLabel($"Cost: {FormatMoney(region.Cost)}",
                UITheme.FontSizeSmall, UITheme.TextSecondary));
            hbox.AddChild(info);

            var scoutBtn = UITheme.CreateFlatButton("Scout", UITheme.Purple);
            var capturedRegion = region;
            scoutBtn.Pressed += () =>
            {
                if (_playerClub.Balance >= capturedRegion.Cost)
                {
                    _playerClub.Balance -= capturedRegion.Cost;
                    int nextId = _gameState.GetNextPlayerId(5);
                    var rng = new Simulation.SeededRng(nextId);
                    var players = ScoutingSystem.Scout(rng, capturedRegion, nextId);

                    foreach (Node child in content.GetChildren())
                        child.QueueFree();

                    content.AddChild(UITheme.CreateLabel(
                        $"Scouted {players.Count} free agents from {capturedRegion.Name}:",
                        UITheme.FontSizeHeading, UITheme.Purple));

                    foreach (Player p in players)
                    {
                        content.AddChild(CreateScoutedPlayerCard(p));
                    }
                }
            };
            hbox.AddChild(scoutBtn);
        }
    }

    // ─── Card factories ───────────────────────────────────────────────

    private HoverCard CreateTransferCard(Player player, Club club, string action)
    {
        var card = HoverCard.Create();
        card.DisableTilt = true;
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.Padding);
        card.AddChild(hbox);

        float ovr = player.PrimaryPosition == PlayerPosition.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;
        decimal value = PlayerValuation.Calculate(player, club.Reputation);

        // OVR badge
        hbox.AddChild(UITheme.CreateBadge($"{ovr:F0}",
            UITheme.StatColor((int)ovr), UITheme.TextLight,
            UITheme.FontSizeBody, new Vector2(44, 40)));

        // Info
        var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        info.AddThemeConstantOverride("separation", 2);
        info.AddChild(UITheme.CreateLabel(player.Name, UITheme.FontSizeBody, UITheme.TextDark));
        info.AddChild(UITheme.CreateLabel(
            $"{player.PrimaryPosition} • Age {player.Age} • {club.Name}",
            UITheme.FontSizeCaption, UITheme.TextSecondary));
        hbox.AddChild(info);

        // Value
        hbox.AddChild(UITheme.CreateLabel(FormatMoney(value),
            UITheme.FontSizeBody, UITheme.Green));

        // Action button
        if (action == "buy")
        {
            var buyBtn = UITheme.CreateFlatButton("Buy", UITheme.Green);
            buyBtn.Pressed += () =>
            {
                if (TransferMarket.ExecuteBuy(_playerClub, club, player, value))
                {
                    _gameState.RecordTransfer(new TransferRecord
                    {
                        Type = TransferType.Buy,
                        PlayerId = player.Id,
                        PlayerName = player.Name,
                        FromClubId = club.Id,
                        ToClubId = _playerClub.Id,
                        Fee = value,
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
                    c.Id != _playerClub.Id && c.Balance >= value &&
                    c.Team.Players.Count < TransferMarket.MaxSquadSize);

                if (buyer != null && TransferMarket.ExecuteBuy(buyer, _playerClub, player, value))
                {
                    _gameState.RecordTransfer(new TransferRecord
                    {
                        Type = TransferType.Sell,
                        PlayerId = player.Id,
                        PlayerName = player.Name,
                        FromClubId = _playerClub.Id,
                        ToClubId = buyer.Id,
                        Fee = value,
                        Day = _gameState.CurrentDay.Day,
                    });
                    BuildUI();
                }
            };
            hbox.AddChild(sellBtn);
        }

        return card;
    }

    private HoverCard CreateProspectCard((Player Prospect, decimal Fee) prospect)
    {
        var card = HoverCard.Create(UITheme.Blue);
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        card.DisableTilt = true;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.Padding);
        card.AddChild(hbox);

        float ovr = prospect.Prospect.PrimaryPosition == PlayerPosition.GK
            ? prospect.Prospect.Attributes.GoalkeeperOverall
            : prospect.Prospect.Attributes.OutfieldOverall;

        hbox.AddChild(UITheme.CreateBadge($"{ovr:F0}",
            UITheme.StatColor((int)ovr), UITheme.TextLight,
            UITheme.FontSizeBody, new Vector2(44, 40)));

        var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        info.AddChild(UITheme.CreateLabel(
            $"{prospect.Prospect.Name}", UITheme.FontSizeBody, UITheme.TextDark));
        info.AddChild(UITheme.CreateLabel(
            $"{prospect.Prospect.PrimaryPosition} • Age {prospect.Prospect.Age} • Fee: {FormatMoney(prospect.Fee)}",
            UITheme.FontSizeCaption, UITheme.TextSecondary));
        hbox.AddChild(info);

        var recruitBtn = UITheme.CreateFlatButton("Recruit", UITheme.Green);
        recruitBtn.Pressed += () =>
        {
            if (_playerClub.Balance >= prospect.Fee)
            {
                TransferMarket.AddFreeAgent(_playerClub, prospect.Prospect, prospect.Fee);
                _gameState.RecordTransfer(new TransferRecord
                {
                    Type = TransferType.YouthRecruit,
                    PlayerId = prospect.Prospect.Id,
                    PlayerName = prospect.Prospect.Name,
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

    private HoverCard CreateScoutedPlayerCard(Player player)
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
        info.AddChild(UITheme.CreateLabel(
            $"{player.PrimaryPosition} • Age {player.Age} • OVR {ovr:F0}",
            UITheme.FontSizeCaption, UITheme.TextSecondary));
        hbox.AddChild(info);

        var signBtn = UITheme.CreateFlatButton("Sign (Free)", UITheme.Green);
        signBtn.Pressed += () =>
        {
            TransferMarket.AddFreeAgent(_playerClub, player, 0);
            _gameState.RecordTransfer(new TransferRecord
            {
                Type = TransferType.ScoutRecruit,
                PlayerId = player.Id,
                PlayerName = player.Name,
                ToClubId = _playerClub.Id,
                Fee = 0,
                Day = _gameState.CurrentDay.Day,
            });
            BuildUI();
        };
        hbox.AddChild(signBtn);

        return card;
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private void OnDone()
    {
        _gameState.AdvanceDay();

        try
        {
            var sm = new SaveManager(System.IO.Path.Combine(OS.GetUserDataDir(), "saves"));
            sm.AutoSave(_gameState);
        }
        catch { /* silent */ }

        SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
    }

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
