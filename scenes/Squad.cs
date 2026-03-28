using Godot;
using ElevenLegends.Data.Enums;
using PlayerPosition = ElevenLegends.Data.Enums.Position;
using ElevenLegends.Data.Models;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Squad management — player list with HoverCards and animated detail panel.
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
        foreach (Node child in GetChildren())
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

        // ─── Left: player list ────────────────────────────────────
        var leftPanel = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leftPanel.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        root.AddChild(leftPanel);

        // Header row
        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        leftPanel.AddChild(headerRow);

        headerRow.AddChild(UITheme.CreateLabel(
            $"{_playerClub.Name} — Squad ({_playerClub.Team.Players.Count})",
            UITheme.FontSizeHeading, UITheme.Blue));

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headerRow.AddChild(spacer);

        var backBtn = UITheme.CreateFlatButton("Back", UITheme.Border, UITheme.TextPrimary);
        backBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        headerRow.AddChild(backBtn);

        // Scrollable player list
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        leftPanel.AddChild(scroll);

        var playerList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        playerList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(playerList);

        // Sorted by position
        var sortedPlayers = _playerClub.Team.Players
            .OrderBy(p => PositionOrder(p.PrimaryPosition))
            .ThenByDescending(p => p.PrimaryPosition == PlayerPosition.GK
                ? p.Attributes.GoalkeeperOverall
                : p.Attributes.OutfieldOverall)
            .ToList();

        foreach (Player player in sortedPlayers)
        {
            var row = CreatePlayerRow(player);
            playerList.AddChild(row);
        }

        // Stagger entrance
        Anim.StaggerChildren(playerList, stagger: 0.03f, useScale: false);

        // ─── Right: player details ────────────────────────────────
        if (_selectedPlayer != null)
        {
            var detailPanel = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(380, 0),
            };
            detailPanel.AddThemeConstantOverride("separation", 8);
            root.AddChild(detailPanel);

            BuildPlayerDetails(detailPanel, _selectedPlayer);
            Anim.FadeIn(detailPanel, delay: 0.1f);
        }
    }

    // ─── Player row card ──────────────────────────────────────────────

    private HoverCard CreatePlayerRow(Player player)
    {
        bool isSelected = _selectedPlayer?.Id == player.Id;
        var card = HoverCard.Create(isSelected ? UITheme.Blue : null);
        card.DisableTilt = true; // Row cards — tilt feels wrong, just scale
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        card.CustomMinimumSize = new Vector2(0, 48);

        if (isSelected)
        {
            // Highlight selected row with subtle bg tint
            var style = new StyleBoxFlat
            {
                BgColor = UITheme.Blue.Lerp(UITheme.Card, 0.85f),
                CornerRadiusTopLeft = UITheme.CardCornerRadius,
                CornerRadiusTopRight = UITheme.CardCornerRadius,
                CornerRadiusBottomLeft = UITheme.CardCornerRadius,
                CornerRadiusBottomRight = UITheme.CardCornerRadius,
                ContentMarginLeft = UITheme.Padding,
                ContentMarginRight = UITheme.Padding,
                ContentMarginTop = UITheme.PaddingSmall,
                ContentMarginBottom = UITheme.PaddingSmall,
                ShadowColor = UITheme.Shadow,
                ShadowSize = 8,
                ShadowOffset = new Vector2(0, 4),
                BorderWidthLeft = 4,
                BorderColor = UITheme.Blue,
            };
            card.AddThemeStyleboxOverride("panel", style);
        }

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        card.AddChild(hbox);

        // OVR badge
        float ovr = player.PrimaryPosition == PlayerPosition.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        var badge = UITheme.CreateBadge($"{ovr:F0}",
            UITheme.StatColor((int)ovr), UITheme.TextLight,
            UITheme.FontSizeBody, new Vector2(40, 36));
        hbox.AddChild(badge);

        // Position chip
        var posLabel = UITheme.CreateLabel($"{player.PrimaryPosition}",
            UITheme.FontSizeCaption, UITheme.TextSecondary);
        posLabel.CustomMinimumSize = new Vector2(36, 0);
        hbox.AddChild(posLabel);

        // Name
        var nameLabel = UITheme.CreateLabel(player.Name,
            UITheme.FontSizeBody, UITheme.TextDark);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(nameLabel);

        // Age
        hbox.AddChild(UITheme.CreateLabel($"{player.Age}",
            UITheme.FontSizeSmall, UITheme.TextSecondary));

        // Morale bar
        var moraleBar = UITheme.CreateProgressBar(player.Morale, 100,
            player.Morale >= 70 ? UITheme.Green : player.Morale >= 40 ? UITheme.Yellow : UITheme.Red,
            null, new Vector2(48, 8));
        moraleBar.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        hbox.AddChild(moraleBar);

        // Click overlay
        var button = new Button
        {
            Flat = true,
            AnchorsPreset = (int)LayoutPreset.FullRect,
            Modulate = new Color(1, 1, 1, 0),
        };
        Player capturedPlayer = player;
        button.Pressed += () =>
        {
            _selectedPlayer = capturedPlayer;
            BuildUI();
        };
        card.AddChild(button);

        return card;
    }

    // ─── Player detail panel ──────────────────────────────────────────

    private void BuildPlayerDetails(VBoxContainer panel, Player player)
    {
        float ovr = player.PrimaryPosition == PlayerPosition.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        // Header card with name and rating
        var headerCard = UITheme.CreateCard(UITheme.RatingColor(ovr));
        panel.AddChild(headerCard);

        var headerVbox = new VBoxContainer();
        headerVbox.AddThemeConstantOverride("separation", 6);
        headerCard.AddChild(headerVbox);

        headerVbox.AddChild(UITheme.CreateLabel(player.Name,
            UITheme.FontSizeTitle, UITheme.TextDark, HorizontalAlignment.Center));

        var infoRow = new HBoxContainer();
        infoRow.AddThemeConstantOverride("separation", UITheme.Padding);
        headerVbox.AddChild(infoRow);

        infoRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var ovrBadge = UITheme.CreateBadge($"{ovr:F0}",
            UITheme.RatingColor(ovr), UITheme.TextDark,
            UITheme.FontSizeTitle, new Vector2(64, 64));
        infoRow.AddChild(ovrBadge);

        var infoVbox = new VBoxContainer();
        infoVbox.AddThemeConstantOverride("separation", 2);
        infoRow.AddChild(infoVbox);

        infoVbox.AddChild(UITheme.CreateLabel(
            $"{player.PrimaryPosition}", UITheme.FontSizeHeading, UITheme.TextDark));
        infoVbox.AddChild(UITheme.CreateLabel(
            $"Age {player.Age}", UITheme.FontSizeSmall, UITheme.TextSecondary));
        infoVbox.AddChild(UITheme.CreateLabel(
            $"Morale {player.Morale}%", UITheme.FontSizeSmall,
            player.Morale >= 70 ? UITheme.Green : UITheme.Yellow));

        infoRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Attributes in a scrollable area
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        panel.AddChild(scroll);

        var attrVbox = new VBoxContainer();
        attrVbox.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(attrVbox);

        PlayerAttributes attrs = player.Attributes;

        AddAttrSection(attrVbox, "Technical", UITheme.Blue, new (string, int)[]
        {
            ("Finishing", attrs.Finishing), ("Passing", attrs.Passing),
            ("Dribbling", attrs.Dribbling), ("First Touch", attrs.FirstTouch),
            ("Technique", attrs.Technique),
        });

        AddAttrSection(attrVbox, "Mental", UITheme.Yellow, new (string, int)[]
        {
            ("Decisions", attrs.Decisions), ("Composure", attrs.Composure),
            ("Positioning", attrs.Positioning), ("Anticipation", attrs.Anticipation),
            ("Off The Ball", attrs.OffTheBall),
        });

        AddAttrSection(attrVbox, "Physical", UITheme.Orange, new (string, int)[]
        {
            ("Speed", attrs.Speed), ("Acceleration", attrs.Acceleration),
            ("Stamina", attrs.Stamina), ("Strength", attrs.Strength),
            ("Agility", attrs.Agility),
        });

        if (player.PrimaryPosition == PlayerPosition.GK)
        {
            AddAttrSection(attrVbox, "Goalkeeper", UITheme.Purple, new (string, int)[]
            {
                ("Reflexes", attrs.Reflexes), ("Handling", attrs.Handling),
                ("GK Positioning", attrs.GkPositioning), ("Aerial", attrs.Aerial),
            });
        }

        // Traits
        if (player.Traits.Count > 0)
        {
            var traitCard = UITheme.CreateCard(UITheme.Purple);
            attrVbox.AddChild(traitCard);

            var traitVbox = new VBoxContainer();
            traitVbox.AddThemeConstantOverride("separation", 4);
            traitCard.AddChild(traitVbox);

            traitVbox.AddChild(UITheme.CreateLabel("Traits",
                UITheme.FontSizeBody, UITheme.Purple));
            foreach (string trait in player.Traits)
            {
                traitVbox.AddChild(UITheme.CreateLabel(
                    $"  ⚡ {trait}", UITheme.FontSizeSmall, UITheme.TextSecondary));
            }
        }
    }

    private void AddAttrSection(VBoxContainer parent, string title, Color color,
        (string label, int value)[] attrs)
    {
        parent.AddChild(UITheme.CreateLabel(title, UITheme.FontSizeBody, color));

        foreach ((string label, int value) in attrs)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            parent.AddChild(row);

            var lbl = UITheme.CreateLabel(label, UITheme.FontSizeSmall, UITheme.TextSecondary);
            lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(lbl);

            row.AddChild(UITheme.CreateLabel($"{value}",
                UITheme.FontSizeSmall, UITheme.StatColor(value)));

            var bar = UITheme.CreateProgressBar(value, 100,
                UITheme.StatColor(value), null, new Vector2(100, 10));
            bar.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(bar);
        }
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
