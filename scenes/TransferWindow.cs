using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.Transfers;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

namespace ElevenLegends.Scenes;

/// <summary>
/// Transfer Window screen — buy, sell, loan, youth, scout.
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
        foreach (var child in GetChildren())
            child.QueueFree();

        var bg = Theme.CreateBackground(Theme.Background);
        AddChild(bg);

        var root = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = Theme.PaddingLarge,
            OffsetRight = -Theme.PaddingLarge,
            OffsetTop = Theme.Padding,
            OffsetBottom = -Theme.Padding,
        };
        root.AddThemeConstantOverride("separation", Theme.Padding);
        AddChild(root);

        // Header
        root.AddChild(Theme.CreateLabel("💰 Transfer Window",
            Theme.FontSizeTitle, Theme.Orange, HorizontalAlignment.Center));
        root.AddChild(Theme.CreateLabel(
            $"Budget: {_playerClub.Balance:C0}  |  Squad: {_playerClub.Team.Players.Count} players",
            Theme.FontSizeBody, Theme.TextSecondary, HorizontalAlignment.Center));

        // Tab bar
        var tabs = new HBoxContainer();
        tabs.AddThemeConstantOverride("separation", 8);
        root.AddChild(tabs);

        var tabDefs = new[] { ("buy", "🛒 Buy"), ("sell", "💵 Sell"), ("youth", "🌱 Youth"), ("scout", "🔭 Scout") };
        foreach (var (id, label) in tabDefs)
        {
            var btn = Theme.CreateButton(label,
                _activeTab == id ? Theme.Orange : Theme.Border,
                _activeTab == id ? Theme.TextLight : Theme.TextPrimary);
            btn.CustomMinimumSize = new Vector2(140, 44);
            var capturedId = id;
            btn.Pressed += () => { _activeTab = capturedId; BuildUI(); };
            tabs.AddChild(btn);
        }

        // Content area
        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddChild(scroll);

        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(content);

        switch (_activeTab)
        {
            case "buy": BuildBuyTab(content); break;
            case "sell": BuildSellTab(content); break;
            case "youth": BuildYouthTab(content); break;
            case "scout": BuildScoutTab(content); break;
        }

        // Bottom buttons
        var bottomHbox = new HBoxContainer();
        bottomHbox.AddThemeConstantOverride("separation", Theme.Padding);
        root.AddChild(bottomHbox);

        var doneBtn = Theme.CreateButton("✅ Done — Advance Day", Theme.Green);
        doneBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        doneBtn.Pressed += OnDone;
        bottomHbox.AddChild(doneBtn);

        var squadBtn = Theme.CreateButton("📋 Squad", Theme.BlueDark);
        squadBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/Squad.tscn");
        bottomHbox.AddChild(squadBtn);
    }

    private void BuildBuyTab(VBoxContainer content)
    {
        var available = TransferMarket.GetAvailablePlayers(
            _gameState.Clubs.ToList(), excludeClubId: _playerClub.Id);

        if (available.Count == 0)
        {
            content.AddChild(Theme.CreateLabel("No players available for transfer.",
                Theme.FontSizeBody, Theme.TextSecondary));
            return;
        }

        foreach (var (player, club, price) in available.Take(20))
        {
            var card = CreatePlayerTransferCard(player, club, "buy");
            content.AddChild(card);
        }
    }

    private void BuildSellTab(VBoxContainer content)
    {
        var sellable = TransferMarket.GetSellablePlayers(_playerClub);

        if (sellable.Count == 0)
        {
            content.AddChild(Theme.CreateLabel("No players can be sold (squad at minimum).",
                Theme.FontSizeBody, Theme.TextSecondary));
            return;
        }

        foreach (var (player, price) in sellable)
        {
            var card = CreatePlayerTransferCard(player, _playerClub, "sell");
            content.AddChild(card);
        }
    }

    private void BuildYouthTab(VBoxContainer content)
    {
        content.AddChild(Theme.CreateLabel("🌱 Youth Academy",
            Theme.FontSizeHeading, Theme.Green));
        content.AddChild(Theme.CreateLabel(
            "Generate 3 youth prospects. Pick one to join your squad.",
            Theme.FontSizeSmall, Theme.TextSecondary));

        var generateBtn = Theme.CreateButton("🎴 Generate Prospects", Theme.Green);
        generateBtn.Pressed += () =>
        {
            int nextId = _gameState.GetNextPlayerId(3);
            var rng = new Simulation.SeededRng(nextId);
            var prospects = YouthAcademy.GenerateProspects(rng, _playerClub.Country, nextId);

            // Show prospect cards
            foreach (var child in content.GetChildren())
                child.QueueFree();

            content.AddChild(Theme.CreateLabel("Choose a prospect:",
                Theme.FontSizeHeading, Theme.Green));

            foreach (var p in prospects)
            {
                var card = Theme.CreateCard();
                card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                content.AddChild(card);

                var vbox = new VBoxContainer();
                vbox.AddThemeConstantOverride("separation", 4);
                card.AddChild(vbox);

                float ovr = p.Prospect.PrimaryPosition == Position.GK
                    ? p.Prospect.Attributes.GoalkeeperOverall
                    : p.Prospect.Attributes.OutfieldOverall;

                vbox.AddChild(Theme.CreateLabel(
                    $"{p.Prospect.Name} — {p.Prospect.PrimaryPosition} — Age {p.Prospect.Age}",
                    Theme.FontSizeBody, Theme.TextPrimary));
                vbox.AddChild(Theme.CreateLabel(
                    $"Overall: {ovr:F0}  |  Fee: {p.Fee:C0}",
                    Theme.FontSizeSmall, Theme.TextSecondary));

                var recruitBtn = Theme.CreateButton("Recruit", Theme.Green);
                recruitBtn.CustomMinimumSize = new Vector2(120, 36);
                var capturedP = p;
                recruitBtn.Pressed += () =>
                {
                    if (_playerClub.Balance >= capturedP.Fee)
                    {
                        TransferMarket.AddFreeAgent(_playerClub, capturedP.Prospect, capturedP.Fee);
                        _gameState.RecordTransfer(new TransferRecord
                        {
                            Type = TransferType.YouthRecruit,
                            PlayerId = capturedP.Prospect.Id,
                            PlayerName = capturedP.Prospect.Name,
                            ToClubId = _playerClub.Id,
                            Fee = capturedP.Fee,
                            Day = _gameState.CurrentDay.Day
                        });
                        BuildUI();
                    }
                };
                vbox.AddChild(recruitBtn);
            }
        };
        content.AddChild(generateBtn);
    }

    private void BuildScoutTab(VBoxContainer content)
    {
        content.AddChild(Theme.CreateLabel("🔭 Scouting Regions",
            Theme.FontSizeHeading, Theme.Blue));

        var regions = ScoutingSystem.GetRegions();
        foreach (var region in regions)
        {
            var hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 8);
            content.AddChild(hbox);

            hbox.AddChild(Theme.CreateLabel(
                $"{region.Name} — Cost: {region.Cost:C0}",
                Theme.FontSizeBody, Theme.TextPrimary));

            var scoutBtn = Theme.CreateButton("Scout", Theme.Blue);
            scoutBtn.CustomMinimumSize = new Vector2(100, 36);
            var capturedRegion = region;
            scoutBtn.Pressed += () =>
            {
                if (_playerClub.Balance >= capturedRegion.Cost)
                {
                    _playerClub.Balance -= capturedRegion.Cost;
                    int nextId = _gameState.GetNextPlayerId(5);
                    var rng = new Simulation.SeededRng(nextId);
                    var players = ScoutingSystem.Scout(rng, capturedRegion, nextId);

                    // Show scouted players
                    foreach (var child in content.GetChildren())
                        child.QueueFree();

                    content.AddChild(Theme.CreateLabel(
                        $"Scouted {players.Count} free agents from {capturedRegion.Name}:",
                        Theme.FontSizeHeading, Theme.Blue));

                    foreach (var p in players)
                    {
                        var card = Theme.CreateCard();
                        content.AddChild(card);

                        var vbox = new VBoxContainer();
                        vbox.AddThemeConstantOverride("separation", 4);
                        card.AddChild(vbox);

                        float ovr = p.PrimaryPosition == Position.GK
                            ? p.Attributes.GoalkeeperOverall
                            : p.Attributes.OutfieldOverall;

                        vbox.AddChild(Theme.CreateLabel(
                            $"{p.Name} — {p.PrimaryPosition} — Age {p.Age} — OVR {ovr:F0}",
                            Theme.FontSizeBody, Theme.TextPrimary));

                        var signBtn = Theme.CreateButton("Sign (Free)", Theme.Green);
                        signBtn.CustomMinimumSize = new Vector2(120, 36);
                        var capturedPlayer = p;
                        signBtn.Pressed += () =>
                        {
                            TransferMarket.AddFreeAgent(_playerClub, capturedPlayer, 0);
                            _gameState.RecordTransfer(new TransferRecord
                            {
                                Type = TransferType.ScoutRecruit,
                                PlayerId = capturedPlayer.Id,
                                PlayerName = capturedPlayer.Name,
                                ToClubId = _playerClub.Id,
                                Fee = 0,
                                Day = _gameState.CurrentDay.Day
                            });
                            BuildUI();
                        };
                        vbox.AddChild(signBtn);
                    }
                }
            };
            hbox.AddChild(scoutBtn);
        }
    }

    private PanelContainer CreatePlayerTransferCard(Player player, Club club, string action)
    {
        var card = Theme.CreateCard();
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", Theme.Padding);
        card.AddChild(hbox);

        float ovr = player.PrimaryPosition == Position.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;
        decimal value = PlayerValuation.Calculate(player, club.Reputation);

        var infoVbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        infoVbox.AddChild(Theme.CreateLabel(
            $"{player.Name}", Theme.FontSizeBody, Theme.TextPrimary));
        infoVbox.AddChild(Theme.CreateLabel(
            $"{player.PrimaryPosition} | Age {player.Age} | OVR {ovr:F0} | {club.Name}",
            Theme.FontSizeSmall, Theme.TextSecondary));
        infoVbox.AddChild(Theme.CreateLabel(
            $"Value: {value:C0}", Theme.FontSizeSmall, Theme.Green));
        hbox.AddChild(infoVbox);

        if (action == "buy")
        {
            var buyBtn = Theme.CreateButton($"Buy {value:C0}", Theme.Green);
            buyBtn.CustomMinimumSize = new Vector2(140, 40);
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
                        Day = _gameState.CurrentDay.Day
                    });
                    BuildUI();
                }
            };
            hbox.AddChild(buyBtn);
        }
        else if (action == "sell")
        {
            var sellBtn = Theme.CreateButton($"Sell {value:C0}", Theme.Pink);
            sellBtn.CustomMinimumSize = new Vector2(140, 40);
            sellBtn.Pressed += () =>
            {
                // Find a buyer (first AI club that can afford)
                var buyer = _gameState.Clubs.FirstOrDefault(c =>
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
                        Day = _gameState.CurrentDay.Day
                    });
                    BuildUI();
                }
            };
            hbox.AddChild(sellBtn);
        }

        return card;
    }

    private void OnDone()
    {
        // Advance the transfer day
        _gameState.AdvanceDay();

        try
        {
            var sm = new SaveManager(System.IO.Path.Combine(OS.GetUserDataDir(), "saves"));
            sm.AutoSave(_gameState);
        }
        catch { /* silent */ }

        SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
    }
}
