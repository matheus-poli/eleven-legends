using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

namespace ElevenLegends.Scenes;

/// <summary>
/// Squad management screen — view and manage players.
/// </summary>
public partial class SquadScreen : Control
{
    private GameState _gameState = null!;
    private Club _playerClub = null!;
    private Player? _selectedPlayer;

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

        var root = new HBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = Theme.PaddingLarge,
            OffsetRight = -Theme.PaddingLarge,
            OffsetTop = Theme.Padding,
            OffsetBottom = -Theme.Padding,
        };
        root.AddThemeConstantOverride("separation", Theme.Padding);
        AddChild(root);

        // Left side — player list
        var leftPanel = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leftPanel.AddThemeConstantOverride("separation", 4);
        root.AddChild(leftPanel);

        var headerHbox = new HBoxContainer();
        leftPanel.AddChild(headerHbox);

        headerHbox.AddChild(Theme.CreateLabel(
            $"📋 {_playerClub.Name} — Squad ({_playerClub.Team.Players.Count})",
            Theme.FontSizeHeading, Theme.Blue));

        var backBtn = Theme.CreateButton("← Back", Theme.Border, Theme.TextPrimary);
        backBtn.CustomMinimumSize = new Vector2(80, 36);
        backBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        headerHbox.AddChild(backBtn);

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        leftPanel.AddChild(scroll);

        var playerList = new VBoxContainer();
        playerList.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(playerList);

        // Sort by position
        var sortedPlayers = _playerClub.Team.Players
            .OrderBy(p => PositionOrder(p.PrimaryPosition))
            .ThenByDescending(p => p.PrimaryPosition == Position.GK
                ? p.Attributes.GoalkeeperOverall
                : p.Attributes.OutfieldOverall)
            .ToList();

        foreach (var player in sortedPlayers)
        {
            var row = CreatePlayerRow(player);
            playerList.AddChild(row);
        }

        // Right side — player details
        if (_selectedPlayer != null)
        {
            var detailPanel = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(360, 0)
            };
            detailPanel.AddThemeConstantOverride("separation", 8);
            root.AddChild(detailPanel);

            BuildPlayerDetails(detailPanel, _selectedPlayer);
        }
    }

    private PanelContainer CreatePlayerRow(Player player)
    {
        bool isSelected = _selectedPlayer?.Id == player.Id;
        var card = Theme.CreateCard();
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        if (isSelected)
        {
            var style = new StyleBoxFlat
            {
                BgColor = Theme.Blue.Lerp(Theme.Background, 0.7f),
                CornerRadiusTopLeft = Theme.CornerRadius,
                CornerRadiusTopRight = Theme.CornerRadius,
                CornerRadiusBottomLeft = Theme.CornerRadius,
                CornerRadiusBottomRight = Theme.CornerRadius,
                ContentMarginLeft = Theme.Padding,
                ContentMarginRight = Theme.Padding,
                ContentMarginTop = 8,
                ContentMarginBottom = 8,
            };
            card.AddThemeStyleboxOverride("panel", style);
        }

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        card.AddChild(hbox);

        float ovr = player.PrimaryPosition == Position.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        Color ovrColor = ovr switch
        {
            >= 80 => Theme.Green,
            >= 60 => Theme.Blue,
            >= 40 => Theme.Yellow,
            _ => Theme.Pink
        };

        hbox.AddChild(Theme.CreateLabel($"{ovr:F0}", Theme.FontSizeHeading,
            ovrColor, HorizontalAlignment.Center));
        hbox.AddChild(Theme.CreateLabel($"{player.PrimaryPosition}",
            Theme.FontSizeSmall, Theme.TextSecondary));

        var nameLabel = Theme.CreateLabel(player.Name,
            Theme.FontSizeBody, Theme.TextPrimary);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(nameLabel);

        hbox.AddChild(Theme.CreateLabel($"Age {player.Age}",
            Theme.FontSizeSmall, Theme.TextSecondary));

        // Make it clickable
        var button = new Button
        {
            Flat = true,
            AnchorsPreset = (int)LayoutPreset.FullRect,
            Modulate = new Color(1, 1, 1, 0), // Invisible overlay
        };
        var capturedPlayer = player;
        button.Pressed += () =>
        {
            _selectedPlayer = capturedPlayer;
            BuildUI();
        };
        card.AddChild(button);

        return card;
    }

    private void BuildPlayerDetails(VBoxContainer panel, Player player)
    {
        var detailCard = Theme.CreateCard();
        panel.AddChild(detailCard);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        detailCard.AddChild(vbox);

        float ovr = player.PrimaryPosition == Position.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        vbox.AddChild(Theme.CreateLabel(player.Name,
            Theme.FontSizeTitle, Theme.TextPrimary, HorizontalAlignment.Center));
        vbox.AddChild(Theme.CreateLabel(
            $"{player.PrimaryPosition} | Age {player.Age}",
            Theme.FontSizeBody, Theme.TextSecondary, HorizontalAlignment.Center));
        vbox.AddChild(Theme.CreateLabel(
            $"Overall: {ovr:F0}",
            Theme.FontSizeHeading, Theme.Green, HorizontalAlignment.Center));

        // Attributes
        var attrs = player.Attributes;
        vbox.AddChild(Theme.CreateLabel("Technical", Theme.FontSizeBody, Theme.Blue));
        AddAttrRow(vbox, "Finishing", attrs.Finishing);
        AddAttrRow(vbox, "Passing", attrs.Passing);
        AddAttrRow(vbox, "Dribbling", attrs.Dribbling);
        AddAttrRow(vbox, "First Touch", attrs.FirstTouch);
        AddAttrRow(vbox, "Technique", attrs.Technique);

        vbox.AddChild(Theme.CreateLabel("Mental", Theme.FontSizeBody, Theme.Yellow));
        AddAttrRow(vbox, "Decisions", attrs.Decisions);
        AddAttrRow(vbox, "Composure", attrs.Composure);
        AddAttrRow(vbox, "Positioning", attrs.Positioning);
        AddAttrRow(vbox, "Anticipation", attrs.Anticipation);
        AddAttrRow(vbox, "Off The Ball", attrs.OffTheBall);

        vbox.AddChild(Theme.CreateLabel("Physical", Theme.FontSizeBody, Theme.Orange));
        AddAttrRow(vbox, "Speed", attrs.Speed);
        AddAttrRow(vbox, "Acceleration", attrs.Acceleration);
        AddAttrRow(vbox, "Stamina", attrs.Stamina);
        AddAttrRow(vbox, "Strength", attrs.Strength);
        AddAttrRow(vbox, "Agility", attrs.Agility);

        if (player.PrimaryPosition == Position.GK)
        {
            vbox.AddChild(Theme.CreateLabel("Goalkeeper", Theme.FontSizeBody, Theme.Pink));
            AddAttrRow(vbox, "Reflexes", attrs.Reflexes);
            AddAttrRow(vbox, "Handling", attrs.Handling);
            AddAttrRow(vbox, "Positioning", attrs.GkPositioning);
            AddAttrRow(vbox, "Aerial", attrs.Aerial);
        }

        // Traits
        if (player.Traits.Count > 0)
        {
            vbox.AddChild(Theme.CreateLabel("Traits", Theme.FontSizeBody, Theme.Pink));
            foreach (var trait in player.Traits)
            {
                vbox.AddChild(Theme.CreateLabel(
                    $"  ⚡ {trait}", Theme.FontSizeSmall, Theme.TextSecondary));
            }
        }
    }

    private void AddAttrRow(VBoxContainer parent, string label, int value)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(hbox);

        var lbl = Theme.CreateLabel(label, Theme.FontSizeSmall, Theme.TextSecondary);
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(lbl);

        Color c = value switch
        {
            >= 80 => Theme.Green,
            >= 60 => Theme.Blue,
            >= 40 => Theme.Yellow,
            _ => Theme.Pink
        };

        hbox.AddChild(Theme.CreateLabel($"{value}", Theme.FontSizeSmall, c));

        var bar = new ProgressBar
        {
            MinValue = 0, MaxValue = 100, Value = value, ShowPercentage = false,
            CustomMinimumSize = new Vector2(100, 12),
        };
        hbox.AddChild(bar);
    }

    private static int PositionOrder(Position pos) => pos switch
    {
        Position.GK => 0,
        Position.CB => 1,
        Position.LB => 2,
        Position.RB => 3,
        Position.CDM => 4,
        Position.CM => 5,
        Position.CAM => 6,
        Position.LM => 7,
        Position.RM => 8,
        Position.LW => 9,
        Position.RW => 10,
        Position.CF => 11,
        Position.ST => 12,
        _ => 99,
    };
}
