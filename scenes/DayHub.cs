using Godot;
using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

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

        var bg = Theme.CreateBackground(Theme.Background);
        AddChild(bg);

        _root = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = Theme.PaddingLarge,
            OffsetRight = -Theme.PaddingLarge,
            OffsetTop = Theme.Padding,
            OffsetBottom = -Theme.Padding,
        };
        _root.AddThemeConstantOverride("separation", Theme.Padding);
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
        hbox.AddThemeConstantOverride("separation", Theme.Padding);
        _root.AddChild(hbox);

        var clubName = Theme.CreateLabel(
            $"⚽ {_playerClub.Name}", Theme.FontSizeHeading, Theme.TextPrimary);
        clubName.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(clubName);

        var money = Theme.CreateLabel(
            $"💰 {_playerClub.Balance:C0}", Theme.FontSizeBody, Theme.Green);
        hbox.AddChild(money);

        var rep = Theme.CreateLabel(
            $"⭐ {_gameState.Manager.Reputation}", Theme.FontSizeBody, Theme.Yellow);
        hbox.AddChild(rep);

        var squad = Theme.CreateLabel(
            $"👥 {_playerClub.Team.Players.Count}", Theme.FontSizeBody, Theme.Blue);
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

        var card = Theme.CreateCard();
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _root.AddChild(card);

        var cardContent = new VBoxContainer();
        cardContent.AddThemeConstantOverride("separation", 8);
        card.AddChild(cardContent);

        var dayHeader = Theme.CreateLabel(
            $"{dayEmoji} Day {day.Day} — {dayName}",
            Theme.FontSizeHeading, Theme.TextPrimary, HorizontalAlignment.Center);
        cardContent.AddChild(dayHeader);

        var dayProgress = Theme.CreateLabel(
            $"Season progress: {_gameState.CurrentDayIndex + 1} / {_gameState.Calendar.Count}",
            Theme.FontSizeSmall, Theme.TextSecondary, HorizontalAlignment.Center);
        cardContent.AddChild(dayProgress);
    }

    private void BuildCalendarPreview()
    {
        var card = Theme.CreateCard();
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
                DayType.Training => Theme.Blue,
                DayType.Rest => Theme.TextSecondary,
                DayType.MatchDay => Theme.Green,
                DayType.MundialMatchDay => Theme.Yellow,
                DayType.TransferWindow => Theme.Orange,
                _ => Theme.Border
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

        var legend = Theme.CreateLabel(
            "🔵 Train  ⚪ Rest  🟢 National  🟡 Mundial  🟠 Transfers",
            Theme.FontSizeCaption, Theme.TextSecondary);
        hbox.AddChild(legend);
    }

    private void BuildActionButtons()
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", Theme.Padding);
        _root.AddChild(hbox);

        var day = _gameState.CurrentDay;

        if (day.Type is DayType.MatchDay or DayType.MundialMatchDay)
        {
            var matchBtn = Theme.CreateButton("⚽ Play Match", Theme.Green);
            matchBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            matchBtn.Pressed += OnPlayMatch;
            hbox.AddChild(matchBtn);
        }
        else if (day.Type == DayType.TransferWindow)
        {
            var transferBtn = Theme.CreateButton("💰 Open Transfers", Theme.Orange);
            transferBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            transferBtn.Pressed += OnOpenTransfers;
            hbox.AddChild(transferBtn);
        }
        else
        {
            var advanceBtn = Theme.CreateButton("⏭️ Advance Day", Theme.Blue);
            advanceBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            advanceBtn.Pressed += OnAdvanceDay;
            hbox.AddChild(advanceBtn);
        }

        // Squad button (always available)
        var squadBtn = Theme.CreateButton("📋 Squad", Theme.BlueDark);
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
