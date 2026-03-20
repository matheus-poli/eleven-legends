using Godot;
using ElevenLegends.Data.Enums;
using PlayerPosition = ElevenLegends.Data.Enums.Position;
using ElevenLegends.Data.Models;
using ElevenLegends.UI;

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

        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        var root = new HBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = UITheme.PaddingLarge,
            OffsetRight = -UITheme.PaddingLarge,
            OffsetTop = UITheme.Padding,
            OffsetBottom = -UITheme.Padding,
        };
        root.AddThemeConstantOverride("separation", UITheme.Padding);
        AddChild(root);

        // Left side — player list
        var leftPanel = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leftPanel.AddThemeConstantOverride("separation", 4);
        root.AddChild(leftPanel);

        var headerHbox = new HBoxContainer();
        leftPanel.AddChild(headerHbox);

        headerHbox.AddChild(UITheme.CreateLabel(
            $"📋 {_playerClub.Name} — Squad ({_playerClub.Team.Players.Count})",
            UITheme.FontSizeHeading, UITheme.Blue));

        var backBtn = UITheme.CreateButton("← Back", UITheme.Border, UITheme.TextPrimary);
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
            .ThenByDescending(p => p.PrimaryPosition == PlayerPosition.GK
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
        var card = UITheme.CreateCard();
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        if (isSelected)
        {
            var style = new StyleBoxFlat
            {
                BgColor = UITheme.Blue.Lerp(UITheme.Background, 0.7f),
                CornerRadiusTopLeft = UITheme.CornerRadius,
                CornerRadiusTopRight = UITheme.CornerRadius,
                CornerRadiusBottomLeft = UITheme.CornerRadius,
                CornerRadiusBottomRight = UITheme.CornerRadius,
                ContentMarginLeft = UITheme.Padding,
                ContentMarginRight = UITheme.Padding,
                ContentMarginTop = 8,
                ContentMarginBottom = 8,
            };
            card.AddThemeStyleboxOverride("panel", style);
        }

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        card.AddChild(hbox);

        float ovr = player.PrimaryPosition == PlayerPosition.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        Color ovrColor = ovr switch
        {
            >= 80 => UITheme.Green,
            >= 60 => UITheme.Blue,
            >= 40 => UITheme.Yellow,
            _ => UITheme.Pink
        };

        hbox.AddChild(UITheme.CreateLabel($"{ovr:F0}", UITheme.FontSizeHeading,
            ovrColor, HorizontalAlignment.Center));
        hbox.AddChild(UITheme.CreateLabel($"{player.PrimaryPosition}",
            UITheme.FontSizeSmall, UITheme.TextSecondary));

        var nameLabel = UITheme.CreateLabel(player.Name,
            UITheme.FontSizeBody, UITheme.TextPrimary);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(nameLabel);

        hbox.AddChild(UITheme.CreateLabel($"Age {player.Age}",
            UITheme.FontSizeSmall, UITheme.TextSecondary));

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
        var detailCard = UITheme.CreateCard();
        panel.AddChild(detailCard);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        detailCard.AddChild(vbox);

        float ovr = player.PrimaryPosition == PlayerPosition.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        vbox.AddChild(UITheme.CreateLabel(player.Name,
            UITheme.FontSizeTitle, UITheme.TextPrimary, HorizontalAlignment.Center));
        vbox.AddChild(UITheme.CreateLabel(
            $"{player.PrimaryPosition} | Age {player.Age}",
            UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center));
        vbox.AddChild(UITheme.CreateLabel(
            $"Overall: {ovr:F0}",
            UITheme.FontSizeHeading, UITheme.Green, HorizontalAlignment.Center));

        // Attributes
        var attrs = player.Attributes;
        vbox.AddChild(UITheme.CreateLabel("Technical", UITheme.FontSizeBody, UITheme.Blue));
        AddAttrRow(vbox, "Finishing", attrs.Finishing);
        AddAttrRow(vbox, "Passing", attrs.Passing);
        AddAttrRow(vbox, "Dribbling", attrs.Dribbling);
        AddAttrRow(vbox, "First Touch", attrs.FirstTouch);
        AddAttrRow(vbox, "Technique", attrs.Technique);

        vbox.AddChild(UITheme.CreateLabel("Mental", UITheme.FontSizeBody, UITheme.Yellow));
        AddAttrRow(vbox, "Decisions", attrs.Decisions);
        AddAttrRow(vbox, "Composure", attrs.Composure);
        AddAttrRow(vbox, "Positioning", attrs.Positioning);
        AddAttrRow(vbox, "Anticipation", attrs.Anticipation);
        AddAttrRow(vbox, "Off The Ball", attrs.OffTheBall);

        vbox.AddChild(UITheme.CreateLabel("Physical", UITheme.FontSizeBody, UITheme.Orange));
        AddAttrRow(vbox, "Speed", attrs.Speed);
        AddAttrRow(vbox, "Acceleration", attrs.Acceleration);
        AddAttrRow(vbox, "Stamina", attrs.Stamina);
        AddAttrRow(vbox, "Strength", attrs.Strength);
        AddAttrRow(vbox, "Agility", attrs.Agility);

        if (player.PrimaryPosition == PlayerPosition.GK)
        {
            vbox.AddChild(UITheme.CreateLabel("Goalkeeper", UITheme.FontSizeBody, UITheme.Pink));
            AddAttrRow(vbox, "Reflexes", attrs.Reflexes);
            AddAttrRow(vbox, "Handling", attrs.Handling);
            AddAttrRow(vbox, "Positioning", attrs.GkPositioning);
            AddAttrRow(vbox, "Aerial", attrs.Aerial);
        }

        // Traits
        if (player.Traits.Count > 0)
        {
            vbox.AddChild(UITheme.CreateLabel("Traits", UITheme.FontSizeBody, UITheme.Pink));
            foreach (var trait in player.Traits)
            {
                vbox.AddChild(UITheme.CreateLabel(
                    $"  ⚡ {trait}", UITheme.FontSizeSmall, UITheme.TextSecondary));
            }
        }
    }

    private void AddAttrRow(VBoxContainer parent, string label, int value)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(hbox);

        var lbl = UITheme.CreateLabel(label, UITheme.FontSizeSmall, UITheme.TextSecondary);
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(lbl);

        Color c = value switch
        {
            >= 80 => UITheme.Green,
            >= 60 => UITheme.Blue,
            >= 40 => UITheme.Yellow,
            _ => UITheme.Pink
        };

        hbox.AddChild(UITheme.CreateLabel($"{value}", UITheme.FontSizeSmall, c));

        var bar = new ProgressBar
        {
            MinValue = 0, MaxValue = 100, Value = value, ShowPercentage = false,
            CustomMinimumSize = new Vector2(100, 12),
        };
        hbox.AddChild(bar);
    }

    private static int PositionOrder(PlayerPosition pos) => pos switch
    {
        PlayerPosition.GK => 0,
        PlayerPosition.CB => 1,
        PlayerPosition.LB => 2,
        PlayerPosition.RB => 3,
        PlayerPosition.CDM => 4,
        PlayerPosition.CM => 5,
        PlayerPosition.CAM => 6,
        PlayerPosition.LM => 7,
        PlayerPosition.RM => 8,
        PlayerPosition.LW => 9,
        PlayerPosition.RW => 10,
        PlayerPosition.CF => 11,
        PlayerPosition.ST => 12,
        _ => 99,
    };
}
