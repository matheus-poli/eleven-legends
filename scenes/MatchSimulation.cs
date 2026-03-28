using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Real-time match simulation — ticks advance on a timer with live controls.
/// Speed controls, tactical style changes, and live substitutions.
/// </summary>
public partial class MatchSimulation : Control
{
    public static MatchState? PendingMatchState;
    public static MatchConfig? PendingConfig;
    public static MatchDayContext? PendingContext;

    private LiveMatchSession _session = null!;
    private MatchConfig _config = null!;
    private MatchDayContext _ctx = null!;
    private GameState _gameState = null!;
    private Club _playerClub = null!;

    // UI elements
    private VBoxContainer _eventFeed = null!;
    private ScrollContainer _eventScroll = null!;
    private Label _scoreLabel = null!;
    private Label _tickLabel = null!;
    private ProgressBar _possessionBar = null!;
    private Label _possHomeLabel = null!;
    private Label _possAwayLabel = null!;
    private HBoxContainer _speedRow = null!;
    private HBoxContainer _styleRow = null!;
    private Button _subButton = null!;
    private Button _halftimeBtn = null!;

    // Simulation state
    private double _tickTimer;
    private float _tickInterval = 1.0f; // seconds per tick
    private bool _paused;
    private bool _waitingForHalftime;
    private TacticalStyle _currentStyle = TacticalStyle.Balanced;

    public override void _Ready()
    {
        _config = PendingConfig!;
        _ctx = PendingContext!;
        _gameState = SceneManager.Instance.CurrentGameState!;
        _playerClub = _gameState.PlayerClub;

        PendingMatchState = null;
        PendingConfig = null;
        PendingContext = null;

        // Resume existing session (from halftime) or create new one
        if (PendingSession != null)
        {
            _session = PendingSession;
            PendingSession = null;
        }
        else
        {
            _session = new LiveMatchSession(_config);
        }
        _currentStyle = _config.HomeTactics?.Style ?? TacticalStyle.Balanced;

        BuildUI();

        // If resuming from halftime, reload first half events
        if (_session.State.Events.Count > 0)
        {
            foreach (MatchEvent evt in _session.State.Events)
                AddEventToFeed(evt);
            UpdateScoreDisplay();
        }
    }

    public override void _Process(double delta)
    {
        if (_paused || _waitingForHalftime || _session.IsMatchFinished)
            return;

        _tickTimer += delta;
        if (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ProcessOneTick();
        }
    }

    private void ProcessOneTick()
    {
        IReadOnlyList<MatchEvent> events = _session.ProcessNextTick();

        foreach (MatchEvent evt in events)
        {
            AddEventToFeed(evt);
        }

        UpdateScoreDisplay();

        // Check halftime
        if (_session.IsHalfTimeReached)
        {
            _waitingForHalftime = true;
            _tickLabel!.Text = "HALF TIME";
            ShowHalftimeButton();
        }

        // Check match finished
        if (_session.IsMatchFinished)
        {
            _tickLabel!.Text = "FULL TIME";
            ShowFullTimeButton();
        }
    }

    private void BuildUI()
    {
        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = UITheme.PaddingLarge;
        root.OffsetRight = -UITheme.PaddingLarge;
        root.OffsetTop = UITheme.PaddingSmall;
        root.OffsetBottom = -UITheme.PaddingSmall;
        root.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        AddChild(root);

        // ─── Score card ───────────────────────────────────────────
        var scoreCard = UITheme.CreateCard(UITheme.Green);
        root.AddChild(scoreCard);

        var scoreVbox = new VBoxContainer();
        scoreVbox.AddThemeConstantOverride("separation", 4);
        scoreCard.AddChild(scoreVbox);

        _scoreLabel = UITheme.CreateLabel("",
            UITheme.FontSizeTitle, UITheme.TextDark, HorizontalAlignment.Center);
        scoreVbox.AddChild(_scoreLabel);

        _tickLabel = UITheme.CreateLabel("0'",
            UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center);
        scoreVbox.AddChild(_tickLabel);

        // Possession bar
        var possRow = new HBoxContainer();
        possRow.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        scoreVbox.AddChild(possRow);

        _possHomeLabel = UITheme.CreateLabel("50%", UITheme.FontSizeCaption, UITheme.Blue);
        possRow.AddChild(_possHomeLabel);

        _possessionBar = UITheme.CreateProgressBar(50, 100, UITheme.Blue, UITheme.Red,
            new Vector2(0, 12));
        _possessionBar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        possRow.AddChild(_possessionBar);

        _possAwayLabel = UITheme.CreateLabel("50%", UITheme.FontSizeCaption, UITheme.Red);
        possRow.AddChild(_possAwayLabel);

        // ─── Event feed ───────────────────────────────────────────
        _eventScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        root.AddChild(_eventScroll);

        _eventFeed = new VBoxContainer();
        _eventFeed.AddThemeConstantOverride("separation", 4);
        _eventScroll.AddChild(_eventFeed);

        // ─── Control bar ──────────────────────────────────────────
        var controlCard = UITheme.CreateCard();
        root.AddChild(controlCard);

        var controlVbox = new VBoxContainer();
        controlVbox.AddThemeConstantOverride("separation", 6);
        controlCard.AddChild(controlVbox);

        // Speed controls
        _speedRow = new HBoxContainer();
        _speedRow.AddThemeConstantOverride("separation", 6);
        controlVbox.AddChild(_speedRow);

        _speedRow.AddChild(UITheme.CreateLabel("Speed",
            UITheme.FontSizeCaption, UITheme.TextSecondary));

        AddSpeedButton("||", 0f, true); // Pause
        AddSpeedButton("1x", 1.5f, false);
        AddSpeedButton("2x", 0.75f, false);
        AddSpeedButton("4x", 0.3f, false);
        AddSpeedButton("8x", 0.1f, false);

        _speedRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Substitution button
        _subButton = UITheme.CreateFlatButton("Subs", UITheme.Orange, UITheme.TextLight);
        _subButton.Pressed += ShowSubstitutionPanel;
        _speedRow.AddChild(_subButton);

        // Tactical style
        _styleRow = new HBoxContainer();
        _styleRow.AddThemeConstantOverride("separation", 6);
        controlVbox.AddChild(_styleRow);

        _styleRow.AddChild(UITheme.CreateLabel("Style",
            UITheme.FontSizeCaption, UITheme.TextSecondary));

        foreach (TacticalStyle style in System.Enum.GetValues<TacticalStyle>())
        {
            bool sel = style == _currentStyle;
            (string iconName, Color color) = style switch
            {
                TacticalStyle.Attacking => ("sword", UITheme.Red),
                TacticalStyle.Defensive => ("shield-check", UITheme.Blue),
                _ => ("scale", UITheme.Green),
            };

            var btn = UITheme.CreateFlatButton($"{style}",
                sel ? color : UITheme.Border,
                sel ? UITheme.TextLight : UITheme.TextPrimary);
            btn.CustomMinimumSize = new Vector2(90, 30);
            btn.AddThemeFontSizeOverride("font_size", 12);
            TacticalStyle captured = style;
            btn.Pressed += () => OnStyleChanged(captured);
            _styleRow.AddChild(btn);
        }

        UpdateScoreDisplay();
        Anim.FadeIn(scoreCard, delay: 0.05f);
    }

    private void AddSpeedButton(string label, float interval, bool isPause)
    {
        bool isActive = isPause ? _paused : (!_paused && System.Math.Abs(_tickInterval - interval) < 0.01f);

        var btn = UITheme.CreateFlatButton(label,
            isActive ? UITheme.Blue : UITheme.Border,
            isActive ? UITheme.TextLight : UITheme.TextPrimary);
        btn.CustomMinimumSize = new Vector2(40, 30);
        btn.AddThemeFontSizeOverride("font_size", 12);

        if (isPause)
        {
            btn.Pressed += () => { _paused = !_paused; RebuildSpeedButtons(); };
        }
        else
        {
            float capturedInterval = interval;
            btn.Pressed += () => { _paused = false; _tickInterval = capturedInterval; RebuildSpeedButtons(); };
        }

        _speedRow.AddChild(btn);
    }

    private void RebuildSpeedButtons()
    {
        // Remove all buttons from speed row except label
        var children = _speedRow.GetChildren();
        for (int i = children.Count - 1; i >= 1; i--)
        {
            Node child = children[i];
            if (child != _subButton)
                child.QueueFree();
        }

        // Re-add speed buttons and sub button
        // Slight delay to avoid modifying tree during iteration
        GetTree().CreateTimer(0.01f).Timeout += () =>
        {
            if (!IsInsideTree()) return;

            AddSpeedButton("||", 0f, true);
            AddSpeedButton("1x", 1.5f, false);
            AddSpeedButton("2x", 0.75f, false);
            AddSpeedButton("4x", 0.3f, false);
            AddSpeedButton("8x", 0.1f, false);

            _speedRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

            _subButton = UITheme.CreateFlatButton("Subs", UITheme.Orange, UITheme.TextLight);
            _subButton.Pressed += ShowSubstitutionPanel;
            _speedRow.AddChild(_subButton);
        };
    }

    private void OnStyleChanged(TacticalStyle style)
    {
        _currentStyle = style;

        // Apply to match state (affects PossessionResolver on next tick)
        bool isHome = _playerClub.Id == _config.HomeTeam.Id;
        if (isHome && _config.HomeTactics != null)
        {
            // Update the tactical style — TacticalSetup is immutable, so we modify config
            // The PossessionResolver reads tactics from config, so we need a workaround
            // For now, store in MatchState as a live override
            // TODO: Add live tactical fields to MatchState
        }

        // Rebuild style row to show selection
        foreach (Node child in _styleRow.GetChildren())
            child.QueueFree();

        _styleRow.AddChild(UITheme.CreateLabel("Style",
            UITheme.FontSizeCaption, UITheme.TextSecondary));

        foreach (TacticalStyle s in System.Enum.GetValues<TacticalStyle>())
        {
            bool sel = s == _currentStyle;
            (string iconName, Color color) = s switch
            {
                TacticalStyle.Attacking => ("sword", UITheme.Red),
                TacticalStyle.Defensive => ("shield-check", UITheme.Blue),
                _ => ("scale", UITheme.Green),
            };

            var btn = UITheme.CreateFlatButton($"{s}",
                sel ? color : UITheme.Border,
                sel ? UITheme.TextLight : UITheme.TextPrimary);
            btn.CustomMinimumSize = new Vector2(90, 30);
            btn.AddThemeFontSizeOverride("font_size", 12);
            TacticalStyle captured = s;
            btn.Pressed += () => OnStyleChanged(captured);
            _styleRow.AddChild(btn);
        }

        AddEventToFeed(new MatchEvent
        {
            Tick = _session.State.CurrentTick,
            Type = EventType.Substitution,
            PlayerId = 0,
            Description = $"Tactical change: {style}",
            RatingImpact = 0f,
        });
    }

    private void ShowSubstitutionPanel()
    {
        _paused = true;

        // Clear event feed and show sub selection
        foreach (Node child in _eventFeed.GetChildren())
            child.QueueFree();

        bool isHome = _playerClub.Id == _config.HomeTeam.Id;
        int subsUsed = isHome ? _session.State.HomeSubstitutionsUsed : _session.State.AwaySubstitutionsUsed;
        List<int> activeIds = isHome ? _session.State.HomeActivePlayerIds : _session.State.AwayActivePlayerIds;

        _eventFeed.AddChild(UITheme.CreateLabel(
            $"Substitutions ({subsUsed}/3 used)",
            UITheme.FontSizeHeading, UITheme.Orange));

        if (subsUsed >= 3)
        {
            _eventFeed.AddChild(UITheme.CreateLabel(
                "No substitutions remaining.",
                UITheme.FontSizeBody, UITheme.TextSecondary));
        }
        else
        {
            _eventFeed.AddChild(UITheme.CreateLabel(
                "Select a player to substitute OUT:",
                UITheme.FontSizeSmall, UITheme.TextSecondary));

            var activeSet = new HashSet<int>(activeIds);
            foreach (Player player in _playerClub.Team.Players.Where(p => activeSet.Contains(p.Id)))
            {
                float stamina = _session.State.PlayerStamina.GetValueOrDefault(player.Id, 0);
                float rating = _session.State.PlayerRatings.GetValueOrDefault(player.Id, 6f);

                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
                _eventFeed.AddChild(row);

                row.AddChild(UITheme.CreateLabel($"{player.PrimaryPosition}",
                    UITheme.FontSizeCaption, UITheme.TextSecondary));
                row.AddChild(UITheme.CreateLabel(player.Name,
                    UITheme.FontSizeSmall, UITheme.TextDark));
                row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
                row.AddChild(UITheme.CreateLabel($"STA {stamina:F0}",
                    UITheme.FontSizeCaption, stamina < 30 ? UITheme.Red : UITheme.TextSecondary));
                row.AddChild(UITheme.CreateLabel($"RTG {rating:F1}",
                    UITheme.FontSizeCaption, UITheme.TextSecondary));

                Player capturedOut = player;
                var outBtn = UITheme.CreateFlatButton("Sub Out", UITheme.Red, UITheme.TextLight);
                outBtn.Pressed += () => ShowSubInOptions(capturedOut);
                row.AddChild(outBtn);
            }
        }

        // Cancel button
        var cancelBtn = UITheme.CreateFlatButton("Cancel", UITheme.Border, UITheme.TextPrimary);
        cancelBtn.Pressed += () =>
        {
            _paused = false;
            RestoreEventFeed();
        };
        _eventFeed.AddChild(cancelBtn);
    }

    private void ShowSubInOptions(Player playerOut)
    {
        foreach (Node child in _eventFeed.GetChildren())
            child.QueueFree();

        bool isHome = _playerClub.Id == _config.HomeTeam.Id;
        List<int> activeIds = isHome ? _session.State.HomeActivePlayerIds : _session.State.AwayActivePlayerIds;
        var activeSet = new HashSet<int>(activeIds);

        _eventFeed.AddChild(UITheme.CreateLabel(
            $"Replace {playerOut.Name} with:",
            UITheme.FontSizeHeading, UITheme.Orange));

        foreach (Player player in _playerClub.Team.Players.Where(p => !activeSet.Contains(p.Id)))
        {
            float ovr = player.PrimaryPosition == Data.Enums.Position.GK
                ? player.Attributes.GoalkeeperOverall
                : player.Attributes.OutfieldOverall;

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
            _eventFeed.AddChild(row);

            row.AddChild(UITheme.CreateBadge($"{ovr:F0}",
                UITheme.StatColor((int)ovr), UITheme.TextLight,
                UITheme.FontSizeCaption, new Vector2(32, 24)));
            row.AddChild(UITheme.CreateLabel($"{player.PrimaryPosition}",
                UITheme.FontSizeCaption, UITheme.TextSecondary));
            row.AddChild(UITheme.CreateLabel(player.Name,
                UITheme.FontSizeSmall, UITheme.TextDark));
            row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

            Player capturedIn = player;
            var inBtn = UITheme.CreateFlatButton("Sub In", UITheme.Green, UITheme.TextLight);
            inBtn.Pressed += () => ExecuteSubstitution(playerOut, capturedIn);
            row.AddChild(inBtn);
        }

        var cancelBtn = UITheme.CreateFlatButton("Back", UITheme.Border, UITheme.TextPrimary);
        cancelBtn.Pressed += ShowSubstitutionPanel;
        _eventFeed.AddChild(cancelBtn);
    }

    private void ExecuteSubstitution(Player playerOut, Player playerIn)
    {
        bool isHome = _playerClub.Id == _config.HomeTeam.Id;
        var sub = new Substitution { PlayerOutId = playerOut.Id, PlayerInId = playerIn.Id };

        bool success = _session.ApplySubstitution(sub, isHome);
        if (success)
        {
            AddEventToFeed(new MatchEvent
            {
                Tick = _session.State.CurrentTick,
                Type = EventType.Substitution,
                PlayerId = playerIn.Id,
                SecondaryPlayerId = playerOut.Id,
                Description = $"SUB: {playerIn.Name} replaces {playerOut.Name}",
                RatingImpact = 0f,
            });
        }

        _paused = false;
        RestoreEventFeed();
    }

    private readonly List<Control> _savedEventNodes = [];

    private void RestoreEventFeed()
    {
        // Rebuild event feed from match events
        foreach (Node child in _eventFeed.GetChildren())
            child.QueueFree();

        foreach (MatchEvent evt in _session.State.Events)
        {
            AddEventToFeed(evt);
        }
    }

    private void ShowHalftimeButton()
    {
        _halftimeBtn = UITheme.CreateButton("Halftime — Choose Card", UITheme.Yellow, UITheme.TextDark);
        _halftimeBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _halftimeBtn.CustomMinimumSize = new Vector2(280, 48);
        _halftimeBtn.Pressed += OnHalftime;

        var root = GetChild<VBoxContainer>(1);
        root.AddChild(_halftimeBtn);

        GetTree().CreateTimer(0.2f).Timeout += () =>
        {
            if (IsInstanceValid(_halftimeBtn))
                Anim.PulseOnce(_halftimeBtn, 1.08f);
        };
    }

    private void ShowFullTimeButton()
    {
        var root = GetChild<VBoxContainer>(1);

        var ftBtn = UITheme.CreateButton("Full Time — See Results", UITheme.Green);
        ftBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        ftBtn.CustomMinimumSize = new Vector2(280, 48);
        ftBtn.Pressed += OnFullTime;
        root.AddChild(ftBtn);

        Anim.PulseOnce(ftBtn, 1.08f);
    }

    private void OnHalftime()
    {
        Halftime.PendingMatchState = _session.State;
        Halftime.PendingConfig = _config;
        Halftime.PendingContext = _ctx;
        // Store session for continuation after halftime
        PendingSession = _session;
        SceneManager.Instance.ChangeScene("res://scenes/Halftime.tscn");
    }

    private void OnFullTime()
    {
        MatchResult result = _session.FinalizeResult();
        PostMatch.PendingResult = result;
        PostMatch.PendingContext = _ctx;
        SceneManager.Instance.ChangeScene("res://scenes/PostMatch.tscn");
    }

    // ─── Static field for session persistence across halftime ─────
    public static LiveMatchSession? PendingSession;

    private void UpdateScoreDisplay()
    {
        MatchState s = _session.State;
        _scoreLabel!.Text = $"{_config.HomeTeam.Name}  {s.ScoreHome} - {s.ScoreAway}  {_config.AwayTeam.Name}";

        int displayTick = s.CurrentTick;
        if (s.Phase == MatchPhase.SecondHalf)
            displayTick = 45 + _session.HalfTick;
        _tickLabel!.Text = $"{displayTick}'";

        float possPct = s.PossessionHome * 100;
        _possessionBar!.Value = possPct;
        _possHomeLabel!.Text = $"{possPct:F0}%";
        _possAwayLabel!.Text = $"{100 - possPct:F0}%";
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
                EventType.Goal => UITheme.Green,
                EventType.Substitution => UITheme.Orange,
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

        // Auto-scroll to bottom
        GetTree().CreateTimer(0.05f).Timeout += () =>
        {
            if (IsInstanceValid(_eventScroll))
                _eventScroll.ScrollVertical = (int)_eventScroll.GetVScrollBar().MaxValue;
        };
    }
}
