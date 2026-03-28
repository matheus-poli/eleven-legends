using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Match simulation screen — live event feed with score, possession, and animated events.
/// </summary>
public partial class MatchSimulation : Control
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

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = UITheme.PaddingLarge;
        root.OffsetRight = -UITheme.PaddingLarge;
        root.OffsetTop = UITheme.Padding;
        root.OffsetBottom = -UITheme.Padding;
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
        Halftime.PendingMatchState = _matchState;
        Halftime.PendingConfig = _config;
        Halftime.PendingContext = _ctx;
        SceneManager.Instance.ChangeScene("res://scenes/Halftime.tscn");
    }

    private void AddEventToFeed(MatchEvent evt)
    {
        string iconName = evt.Type switch
        {
            EventType.Goal => "football",
            EventType.Assist => "letter-a",
            EventType.Shot => "target",
            EventType.ShotOnTarget => "goal-net",
            EventType.Foul => "alert-triangle",
            EventType.YellowCard => "card-yellow",
            EventType.RedCard => "card-red",
            EventType.Save => "gloves",
            EventType.Substitution => "arrow-swap",
            _ => "clipboard",
        };

        bool isGoal = evt.Type == EventType.Goal;

        // Goal events get a card treatment
        if (isGoal)
        {
            var goalCard = UITheme.CreateCard(UITheme.Green);
            goalCard.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _eventFeed.AddChild(goalCard);

            var goalRow = new HBoxContainer();
            goalRow.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
            goalRow.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            goalCard.AddChild(goalRow);

            goalRow.AddChild(UITheme.CreateIcon(iconName, new Vector2(20, 20)));
            goalRow.AddChild(UITheme.CreateLabel(
                $"{evt.Tick}'  {evt.Description}",
                UITheme.FontSizeBody, UITheme.Green));

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

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
            _eventFeed.AddChild(row);

            row.AddChild(UITheme.CreateIcon(iconName, new Vector2(16, 16), color));
            row.AddChild(UITheme.CreateLabel(
                $"{evt.Tick}'  {evt.Description}",
                UITheme.FontSizeSmall, color));
        }
    }

    private void UpdateScore()
    {
        _scoreLabel!.Text =
            $"{_config.HomeTeam.Name}  {_matchState.ScoreHome} - {_matchState.ScoreAway}  {_config.AwayTeam.Name}";
        _possessionBar!.Value = _matchState.PossessionHome * 100;
    }
}
