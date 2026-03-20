using Godot;
using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Day Hub — central screen of the game loop.
/// Shows current day, club info, and navigation to match/transfers/squad.
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
        foreach (var child in GetChildren())
            child.QueueFree();

        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        _root = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = UITheme.PaddingLarge,
            OffsetRight = -UITheme.PaddingLarge,
            OffsetTop = UITheme.Padding,
            OffsetBottom = -UITheme.Padding,
        };
        _root.AddThemeConstantOverride("separation", UITheme.Padding);
        AddChild(_root);

        // Top bar: Club name + money
        BuildTopBar();

        // Day card
        BuildDayCard();

        // Calendar preview
        BuildCalendarPreview();

        // Action buttons
        BuildActionButtons();
    }

    private void BuildTopBar()
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.Padding);
        _root.AddChild(hbox);

        var clubName = UITheme.CreateLabel(
            $"⚽ {_playerClub.Name}", UITheme.FontSizeHeading, UITheme.TextPrimary);
        clubName.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(clubName);

        var money = UITheme.CreateLabel(
            $"💰 {_playerClub.Balance:C0}", UITheme.FontSizeBody, UITheme.Green);
        hbox.AddChild(money);

        var rep = UITheme.CreateLabel(
            $"⭐ {_gameState.Manager.Reputation}", UITheme.FontSizeBody, UITheme.Yellow);
        hbox.AddChild(rep);

        var squad = UITheme.CreateLabel(
            $"👥 {_playerClub.Team.Players.Count}", UITheme.FontSizeBody, UITheme.Blue);
        hbox.AddChild(squad);
    }

    private void BuildDayCard()
    {
        if (_gameState.IsSeasonOver)
        {
            SceneManager.Instance.ChangeScene("res://scenes/SeasonEnd.tscn");
            return;
        }

        var day = _gameState.CurrentDay;
        string dayEmoji = day.Type switch
        {
            DayType.Training => "🏋️",
            DayType.Rest => "😴",
            DayType.MatchDay => "⚽",
            DayType.MundialMatchDay => "🏆",
            DayType.TransferWindow => "💰",
            _ => "📅"
        };

        string dayName = day.Type switch
        {
            DayType.Training => "Training Day",
            DayType.Rest => "Rest Day",
            DayType.MatchDay => "Match Day — National",
            DayType.MundialMatchDay => "Match Day — Mundial",
            DayType.TransferWindow => "Transfer Window",
            _ => "Day"
        };

        var card = UITheme.CreateCard();
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _root.AddChild(card);

        var cardContent = new VBoxContainer();
        cardContent.AddThemeConstantOverride("separation", 8);
        card.AddChild(cardContent);

        var dayHeader = UITheme.CreateLabel(
            $"{dayEmoji} Day {day.Day} — {dayName}",
            UITheme.FontSizeHeading, UITheme.TextPrimary, HorizontalAlignment.Center);
        cardContent.AddChild(dayHeader);

        var dayProgress = UITheme.CreateLabel(
            $"Season progress: {_gameState.CurrentDayIndex + 1} / {_gameState.Calendar.Count}",
            UITheme.FontSizeSmall, UITheme.TextSecondary, HorizontalAlignment.Center);
        cardContent.AddChild(dayProgress);
    }

    private void BuildCalendarPreview()
    {
        var card = UITheme.CreateCard();
        _root.AddChild(card);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        card.AddChild(hbox);

        // Show next 10 days as colored dots
        int start = _gameState.CurrentDayIndex;
        int end = Mathf.Min(start + 12, _gameState.Calendar.Count);

        for (int i = start; i < end; i++)
        {
            var dayType = _gameState.Calendar[i].Type;
            Color color = dayType switch
            {
                DayType.Training => UITheme.Blue,
                DayType.Rest => UITheme.TextSecondary,
                DayType.MatchDay => UITheme.Green,
                DayType.MundialMatchDay => UITheme.Yellow,
                DayType.TransferWindow => UITheme.Orange,
                _ => UITheme.Border
            };

            var dot = new ColorRect
            {
                CustomMinimumSize = new Vector2(i == start ? 28 : 20, 28),
                Color = color,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };

            if (i == start)
            {
                // Current day is larger with border
                dot.CustomMinimumSize = new Vector2(28, 28);
            }

            hbox.AddChild(dot);
        }

        var legend = UITheme.CreateLabel(
            "🔵 Train  ⚪ Rest  🟢 National  🟡 Mundial  🟠 Transfers",
            UITheme.FontSizeCaption, UITheme.TextSecondary);
        hbox.AddChild(legend);
    }

    private void BuildActionButtons()
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.Padding);
        _root.AddChild(hbox);

        var day = _gameState.CurrentDay;

        if (day.Type is DayType.MatchDay or DayType.MundialMatchDay)
        {
            var matchBtn = UITheme.CreateButton("⚽ Play Match", UITheme.Green);
            matchBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            matchBtn.Pressed += OnPlayMatch;
            hbox.AddChild(matchBtn);
        }
        else if (day.Type == DayType.TransferWindow)
        {
            var transferBtn = UITheme.CreateButton("💰 Open Transfers", UITheme.Orange);
            transferBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            transferBtn.Pressed += OnOpenTransfers;
            hbox.AddChild(transferBtn);
        }
        else
        {
            var advanceBtn = UITheme.CreateButton("⏭️ Advance Day", UITheme.Blue);
            advanceBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            advanceBtn.Pressed += OnAdvanceDay;
            hbox.AddChild(advanceBtn);
        }

        // Squad button (always available)
        var squadBtn = UITheme.CreateButton("📋 Squad", UITheme.BlueDark);
        squadBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/Squad.tscn");
        hbox.AddChild(squadBtn);
    }

    private void OnAdvanceDay()
    {
        var result = _gameState.AdvanceDay();
        AutoSave();

        if (result.GameOver)
        {
            SceneManager.Instance.ChangeScene("res://scenes/SeasonEnd.tscn");
            return;
        }

        if (result.Finished || result.Victory)
        {
            SceneManager.Instance.ChangeScene("res://scenes/SeasonEnd.tscn");
            return;
        }

        // Rebuild UI for new day
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
