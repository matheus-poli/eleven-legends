using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

namespace ElevenLegends.Scenes;

/// <summary>
/// Match simulation screen — shows match events tick by tick.
/// </summary>
public partial class MatchSimScreen : Control
{
    // Static state passed from PreMatch (workaround for scene transitions)
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

    private bool _isHalftime;
    private MatchResult? _finalResult;

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

        // Show first half events
        ShowFirstHalfEvents();
    }

    private void BuildUI()
    {
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

        bool isHome = _ctx.PlayerFixture!.HomeClubId == _playerClub.Id;
        var opponentId = isHome ? _ctx.PlayerFixture.AwayClubId : _ctx.PlayerFixture.HomeClubId;
        var opponent = _gameState.Clubs.First(c => c.Id == opponentId);

        // Score display
        var scoreCard = Theme.CreateCard();
        root.AddChild(scoreCard);

        var scoreVbox = new VBoxContainer();
        scoreVbox.AddThemeConstantOverride("separation", 4);
        scoreCard.AddChild(scoreVbox);

        _scoreLabel = Theme.CreateLabel(
            $"{_config.HomeTeam.Name}  {_matchState.ScoreHome} - {_matchState.ScoreAway}  {_config.AwayTeam.Name}",
            Theme.FontSizeTitle, Theme.TextPrimary, HorizontalAlignment.Center);
        scoreVbox.AddChild(_scoreLabel);

        _tickLabel = Theme.CreateLabel("", Theme.FontSizeBody,
            Theme.TextSecondary, HorizontalAlignment.Center);
        scoreVbox.AddChild(_tickLabel);

        // Possession bar
        var possLabel = Theme.CreateLabel("Possession", Theme.FontSizeSmall,
            Theme.TextSecondary, HorizontalAlignment.Center);
        scoreVbox.AddChild(possLabel);

        _possessionBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = _matchState.PossessionHome * 100,
            CustomMinimumSize = new Vector2(0, 24),
            ShowPercentage = true,
        };
        scoreVbox.AddChild(_possessionBar);

        // Event feed (scrollable)
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(scroll);

        _eventFeed = new VBoxContainer();
        _eventFeed.AddThemeConstantOverride("separation", 4);
        scroll.AddChild(_eventFeed);
    }

    private void ShowFirstHalfEvents()
    {
        _tickLabel!.Text = "⏱️ Half Time";

        foreach (var evt in _matchState.Events)
        {
            AddEventToFeed(evt);
        }

        UpdateScore();

        // Add halftime button
        var root = GetChild<VBoxContainer>(1); // The VBoxContainer after bg
        var halftimeBtn = Theme.CreateButton("🎴 Halftime — Choose Card", Theme.Yellow, Theme.TextPrimary);
        halftimeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        halftimeBtn.Pressed += OnHalftime;
        root.AddChild(halftimeBtn);
    }

    private void OnHalftime()
    {
        // Go to halftime screen
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
            _ => "📋"
        };

        Color color = evt.Type switch
        {
            EventType.Goal => Theme.Green,
            EventType.YellowCard => Theme.Yellow,
            EventType.RedCard => Theme.Pink,
            _ => Theme.TextSecondary
        };

        var label = Theme.CreateLabel(
            $"  {evt.Tick}' {emoji} {evt.Description}",
            evt.Type == EventType.Goal ? Theme.FontSizeBody : Theme.FontSizeSmall,
            color);
        _eventFeed.AddChild(label);
    }

    private void UpdateScore()
    {
        _scoreLabel!.Text =
            $"{_config.HomeTeam.Name}  {_matchState.ScoreHome} - {_matchState.ScoreAway}  {_config.AwayTeam.Name}";
        _possessionBar!.Value = _matchState.PossessionHome * 100;
    }
}
