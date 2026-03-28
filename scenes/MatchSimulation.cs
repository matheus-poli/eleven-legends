using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Match simulation screen — live event feed with score, possession, and animated events.
/// </summary>
public partial class MatchSimScreen : Control
{
    public static MatchState? PendingMatchState;
    public static MatchConfig? PendingConfig;
    public static MatchDayContext? PendingContext;

    private MatchState _matchState = null!;
    private MatchConfig _config = null!;
    private MatchDayContext _ctx = null!;
    private GameState _gameState = null!;
    private Club _playerClub = null!;

    private VBoxContainer _eventFeed = null!;
    private Label _scoreLabel = null!;
    private Label _tickLabel = null!;
    private ProgressBar _possessionBar = null!;

    public override void _Ready()
    {
        _matchState = PendingMatchState!;
        _config = PendingConfig!;
        _ctx = PendingContext!;
        _gameState = SceneManager.Instance.CurrentGameState!;
        _playerClub = _gameState.PlayerClub;

        PendingMatchState = null;
        PendingConfig = null;
        PendingContext = null;

        BuildUI();
        ShowFirstHalfEvents();
    }

    private void BuildUI()
    {
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

        // ─── Score card ───────────────────────────────────────────
        var scoreCard = UITheme.CreateCard(UITheme.Green);
        root.AddChild(scoreCard);

        var scoreVbox = new VBoxContainer();
        scoreVbox.AddThemeConstantOverride("separation", 6);
        scoreCard.AddChild(scoreVbox);

        _scoreLabel = UITheme.CreateLabel(
            $"{_config.HomeTeam.Name}  {_matchState.ScoreHome} - {_matchState.ScoreAway}  {_config.AwayTeam.Name}",
            UITheme.FontSizeTitle, UITheme.TextDark, HorizontalAlignment.Center);
        scoreVbox.AddChild(_scoreLabel);

        _tickLabel = UITheme.CreateLabel("",
            UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center);
        scoreVbox.AddChild(_tickLabel);

        // Possession bar
        scoreVbox.AddChild(UITheme.CreateLabel("Possession",
            UITheme.FontSizeCaption, UITheme.TextSecondary, HorizontalAlignment.Center));

        var possRow = new HBoxContainer();
        possRow.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        scoreVbox.AddChild(possRow);

        possRow.AddChild(UITheme.CreateLabel(
            $"{_matchState.PossessionHome * 100:F0}%",
            UITheme.FontSizeSmall, UITheme.Blue));

        _possessionBar = UITheme.CreateProgressBar(
            _matchState.PossessionHome * 100, 100, UITheme.Blue, UITheme.Red,
            new Vector2(0, 16));
        _possessionBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        possRow.AddChild(_possessionBar);

        possRow.AddChild(UITheme.CreateLabel(
            $"{(1 - _matchState.PossessionHome) * 100:F0}%",
            UITheme.FontSizeSmall, UITheme.Red));

        // ─── Event feed ───────────────────────────────────────────
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        root.AddChild(scroll);

        _eventFeed = new VBoxContainer();
        _eventFeed.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_eventFeed);

        Anim.FadeIn(scoreCard, delay: 0.05f);
    }

    private void ShowFirstHalfEvents()
    {
        _tickLabel!.Text = "Half Time";

        foreach (MatchEvent evt in _matchState.Events)
        {
            AddEventToFeed(evt);
        }
        UpdateScore();

        // Halftime button
        var root = GetChild<VBoxContainer>(1);
        var halftimeBtn = UITheme.CreateButton("Halftime — Choose Card", UITheme.Yellow, UITheme.TextDark);
        halftimeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        halftimeBtn.CustomMinimumSize = new Vector2(280, 56);
        halftimeBtn.Pressed += OnHalftime;
        root.AddChild(halftimeBtn);

        // Animate the button entrance
        GetTree().CreateTimer(0.3f).Timeout += () =>
        {
            if (IsInstanceValid(halftimeBtn))
                Anim.PulseOnce(halftimeBtn, 1.08f);
        };
    }

    private void OnHalftime()
    {
        HalftimeScreen.PendingMatchState = _matchState;
        HalftimeScreen.PendingConfig = _config;
        HalftimeScreen.PendingContext = _ctx;
        SceneManager.Instance.ChangeScene("res://scenes/Halftime.tscn");
    }

    private void AddEventToFeed(MatchEvent evt)
    {
        string emoji = evt.Type switch
        {
            EventType.Goal => "⚽",
            EventType.Assist => "🅰️",
            EventType.Shot => "🎯",
            EventType.ShotOnTarget => "🥅",
            EventType.Foul => "⚠️",
            EventType.YellowCard => "🟡",
            EventType.RedCard => "🔴",
            EventType.Save => "🧤",
            EventType.Substitution => "🔄",
            _ => "📋",
        };

        bool isGoal = evt.Type == EventType.Goal;

        // Goal events get a card treatment
        if (isGoal)
        {
            var goalCard = UITheme.CreateCard(UITheme.Green);
            goalCard.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _eventFeed.AddChild(goalCard);

            var goalLabel = UITheme.CreateLabel(
                $"{evt.Tick}'  {emoji}  {evt.Description}",
                UITheme.FontSizeBody, UITheme.Green, HorizontalAlignment.Center);
            goalCard.AddChild(goalLabel);

            Anim.PulseOnce(goalCard, 1.05f);
        }
        else
        {
            Color color = evt.Type switch
            {
                EventType.YellowCard => UITheme.Yellow,
                EventType.RedCard => UITheme.Red,
                _ => UITheme.TextSecondary,
            };

            var label = UITheme.CreateLabel(
                $"  {evt.Tick}'  {emoji}  {evt.Description}",
                UITheme.FontSizeSmall, color);
            _eventFeed.AddChild(label);
        }
    }

    private void UpdateScore()
    {
        _scoreLabel!.Text =
            $"{_config.HomeTeam.Name}  {_matchState.ScoreHome} - {_matchState.ScoreAway}  {_config.AwayTeam.Name}";
        _possessionBar!.Value = _matchState.PossessionHome * 100;
    }
}
