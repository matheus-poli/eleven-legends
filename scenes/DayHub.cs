using Godot;
using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Day Hub — central game loop screen.
/// Duolingo-style dashboard with progress bar, day card, calendar, and action buttons.
/// </summary>
public partial class DayHub : Control
{
    private GameState _gameState = null!;
    private Club _playerClub = null!;
    private SaveManager _saveManager = null!;
    private VBoxContainer _root = null!;

    public override void _Ready()
    {
        _gameState = SceneManager.Instance.CurrentGameState!;
        _playerClub = _gameState.PlayerClub;
        _saveManager = new SaveManager(
            System.IO.Path.Combine(OS.GetUserDataDir(), "saves"));

        BuildUI();
    }

    private void BuildUI()
    {
        // Clear previous UI
        foreach (Node child in GetChildren())
            child.QueueFree();

        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        _root = new VBoxContainer();
        _root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _root.OffsetLeft = UITheme.PaddingLarge;
        _root.OffsetRight = -UITheme.PaddingLarge;
        _root.OffsetTop = UITheme.Padding;
        _root.OffsetBottom = -UITheme.Padding;
        _root.AddThemeConstantOverride("separation", UITheme.Padding);
        AddChild(_root);

        BuildTopBar();
        BuildSeasonProgress();
        BuildDayCard();
        BuildCalendarPreview();
        BuildActionButtons();

        // Entrance animation
        Anim.StaggerChildren(_root, stagger: 0.06f, useScale: false);
    }

    // ─── Top bar: club name + stats chips ─────────────────────────────

    private void BuildTopBar()
    {
        var topCard = UITheme.CreateCard();
        _root.AddChild(topCard);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.Padding);
        topCard.AddChild(hbox);

        var clubName = UITheme.CreateLabel(
            _playerClub.Name, UITheme.FontSizeHeading, UITheme.TextDark);
        clubName.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(clubName);

        hbox.AddChild(CreateInfoChip(FormatMoney(_playerClub.Balance), "Budget",
            "res://assets/icons/coin.svg", UITheme.Green));
        hbox.AddChild(CreateInfoChip($"{_gameState.Manager.Reputation}", "Rep",
            "res://assets/icons/star.svg", UITheme.Yellow));
        hbox.AddChild(CreateInfoChip($"{_playerClub.Team.Players.Count}", "Squad",
            "res://assets/icons/users.svg", UITheme.Blue));
    }

    // ─── Season progress bar ──────────────────────────────────────────

    private void BuildSeasonProgress()
    {
        float progress = (float)(_gameState.CurrentDayIndex + 1) / _gameState.Calendar.Count;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        _root.AddChild(hbox);

        var label = UITheme.CreateLabel("Season",
            UITheme.FontSizeCaption, UITheme.TextSecondary);
        hbox.AddChild(label);

        var bar = UITheme.CreateProgressBar(
            progress * 100, 100, UITheme.Green, null,
            new Vector2(0, 12));
        bar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(bar);

        var pct = UITheme.CreateLabel($"{progress * 100:F0}%",
            UITheme.FontSizeCaption, UITheme.Green);
        hbox.AddChild(pct);
    }

    // ─── Day card (main focal point) ──────────────────────────────────

    private void BuildDayCard()
    {
        if (_gameState.IsSeasonOver)
        {
            SceneManager.Instance.ChangeScene("res://scenes/SeasonEnd.tscn");
            return;
        }

        SeasonDay day = _gameState.CurrentDay;

        // Pick accent color and icon by day type
        (string iconName, string dayName, Color accent) = day.Type switch
        {
            DayType.Training => ("dumbbell", "Training Day", UITheme.Blue),
            DayType.Rest => ("moon", "Rest Day", UITheme.TextSecondary),
            DayType.MatchDay => ("football", "Match Day — National", UITheme.Green),
            DayType.MundialMatchDay => ("trophy", "Match Day — Mundial", UITheme.Yellow),
            DayType.TransferWindow => ("banknote", "Transfer Window", UITheme.Orange),
            _ => ("calendar", "Day", UITheme.TextSecondary),
        };

        var dayCard = UITheme.CreateCard(accent);
        dayCard.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _root.AddChild(dayCard);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        dayCard.AddChild(vbox);

        // Day icon + number
        var dayHeader = UITheme.CreateIconLabel(iconName, $"Day {day.Day}",
            UITheme.FontSizeTitle, accent);
        dayHeader.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        vbox.AddChild(dayHeader);

        vbox.AddChild(UITheme.CreateLabel(dayName,
            UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center));
    }

    // ─── Calendar preview (colored dots for upcoming days) ────────────

    private void BuildCalendarPreview()
    {
        var card = UITheme.CreateCard();
        _root.AddChild(card);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        card.AddChild(vbox);

        var dotsRow = new HBoxContainer();
        dotsRow.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(dotsRow);

        int start = _gameState.CurrentDayIndex;
        int end = Mathf.Min(start + 14, _gameState.Calendar.Count);

        for (int i = start; i < end; i++)
        {
            DayType dtype = _gameState.Calendar[i].Type;
            Color dotColor = dtype switch
            {
                DayType.Training => UITheme.Blue,
                DayType.Rest => UITheme.Border,
                DayType.MatchDay => UITheme.Green,
                DayType.MundialMatchDay => UITheme.Yellow,
                DayType.TransferWindow => UITheme.Orange,
                _ => UITheme.Border,
            };

            bool isCurrent = i == start;
            float size = isCurrent ? 24 : 16;

            var dot = new ColorRect
            {
                CustomMinimumSize = new Vector2(size, size),
                Color = dotColor,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };

            // Round the dots using a stylebox
            var dotPanel = new PanelContainer
            {
                CustomMinimumSize = new Vector2(size, size),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            var dotStyle = new StyleBoxFlat
            {
                BgColor = dotColor,
                CornerRadiusTopLeft = (int)(size / 2),
                CornerRadiusTopRight = (int)(size / 2),
                CornerRadiusBottomLeft = (int)(size / 2),
                CornerRadiusBottomRight = (int)(size / 2),
            };
            dotPanel.AddThemeStyleboxOverride("panel", dotStyle);
            dotsRow.AddChild(dotPanel);
        }

        // Legend
        var legendRow = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
        };
        legendRow.AddThemeConstantOverride("separation", UITheme.Padding);
        vbox.AddChild(legendRow);

        AddLegendItem(legendRow, UITheme.Blue, "Train");
        AddLegendItem(legendRow, UITheme.Border, "Rest");
        AddLegendItem(legendRow, UITheme.Green, "National");
        AddLegendItem(legendRow, UITheme.Yellow, "Mundial");
        AddLegendItem(legendRow, UITheme.Orange, "Transfer");
    }

    // ─── Action buttons ───────────────────────────────────────────────

    private void BuildActionButtons()
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.Padding);
        _root.AddChild(hbox);

        SeasonDay day = _gameState.CurrentDay;

        if (day.Type is DayType.MatchDay or DayType.MundialMatchDay)
        {
            var matchBtn = UITheme.CreateButton("Play Match", UITheme.Green);
            matchBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            matchBtn.Pressed += OnPlayMatch;
            hbox.AddChild(matchBtn);
        }
        else if (day.Type == DayType.TransferWindow)
        {
            var transferBtn = UITheme.CreateButton("Open Transfers", UITheme.Orange);
            transferBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            transferBtn.Pressed += OnOpenTransfers;
            hbox.AddChild(transferBtn);

            var advanceBtn = UITheme.CreateButton("Advance Day", UITheme.Green);
            advanceBtn.Pressed += OnAdvanceDay;
            hbox.AddChild(advanceBtn);
        }
        else
        {
            var advanceBtn = UITheme.CreateButton("Advance Day", UITheme.Blue);
            advanceBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            advanceBtn.Pressed += OnAdvanceDay;
            hbox.AddChild(advanceBtn);
        }

        var squadBtn = UITheme.CreateButton("Squad", UITheme.BlueDark);
        squadBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/Squad.tscn");
        hbox.AddChild(squadBtn);
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private static void AddLegendItem(HBoxContainer parent, Color color, string text)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        parent.AddChild(hbox);

        var dot = new PanelContainer { CustomMinimumSize = new Vector2(10, 10) };
        dot.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var dotStyle = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = 5, CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5, CornerRadiusBottomRight = 5,
        };
        dot.AddThemeStyleboxOverride("panel", dotStyle);
        hbox.AddChild(dot);

        hbox.AddChild(UITheme.CreateLabel(text, UITheme.FontSizeCaption, UITheme.TextSecondary));
    }

    private static Control CreateInfoChip(string value, string label, string iconPath, Color color)
    {
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);

        // Badge with icon + value
        var badgeStyle = new StyleBoxFlat
        {
            BgColor = color,
            CornerRadiusTopLeft = UITheme.BadgeCornerRadius,
            CornerRadiusTopRight = UITheme.BadgeCornerRadius,
            CornerRadiusBottomLeft = UITheme.BadgeCornerRadius,
            CornerRadiusBottomRight = UITheme.BadgeCornerRadius,
            ContentMarginLeft = UITheme.PaddingSmall,
            ContentMarginRight = UITheme.PaddingSmall + 2,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        };

        var badge = new PanelContainer();
        badge.AddThemeStyleboxOverride("panel", badgeStyle);

        var badgeRow = new HBoxContainer();
        badgeRow.AddThemeConstantOverride("separation", 4);
        badge.AddChild(badgeRow);

        // SVG icon
        Texture2D? iconTex = GD.Load<Texture2D>(iconPath);
        if (iconTex != null)
        {
            var icon = new TextureRect
            {
                Texture = iconTex,
                CustomMinimumSize = new Vector2(16, 16),
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
            };
            badgeRow.AddChild(icon);
        }

        var valueLabel = UITheme.CreateLabel(value, UITheme.FontSizeSmall, UITheme.TextLight);
        badgeRow.AddChild(valueLabel);

        vbox.AddChild(badge);

        // Label below
        vbox.AddChild(UITheme.CreateLabel(label,
            UITheme.FontSizeCaption, UITheme.TextSecondary, HorizontalAlignment.Center));

        return vbox;
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

    // ─── Navigation handlers ──────────────────────────────────────────

    private void OnAdvanceDay()
    {
        DayResult result = _gameState.AdvanceDay();
        AutoSave();

        if (result.GameOver || result.Finished || result.Victory)
        {
            SceneManager.Instance.ChangeScene("res://scenes/SeasonEnd.tscn");
            return;
        }

        BuildUI();
    }

    private void OnPlayMatch()
    {
        SceneManager.Instance.ChangeScene("res://scenes/PreMatch.tscn");
    }

    private void OnOpenTransfers()
    {
        SceneManager.Instance.ChangeScene("res://scenes/TransferWindow.tscn");
    }

    private void AutoSave()
    {
        try { _saveManager.AutoSave(_gameState); }
        catch (System.Exception ex) { GD.PrintErr($"Autosave failed: {ex.Message}"); }
    }
}
