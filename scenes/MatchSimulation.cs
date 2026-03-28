using Godot;
using ElevenLegends.Data.Enums;
using PlayerPosition = ElevenLegends.Data.Enums.Position;
using ElevenLegends.Data.Models;
using ElevenLegends.Scenes.Components;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Real-time match simulation with live pitch view (SofaScore-style).
/// Left: dual-team pitch with live ratings. Right: event feed.
/// Bottom: speed + tactical controls.
/// </summary>
public partial class MatchSimulation : Control
{
    public static MatchConfig? PendingConfig;
    public static MatchDayContext? PendingContext;
    public static LiveMatchSession? PendingSession;
    // Legacy compat
    public static MatchState? PendingMatchState;

    private LiveMatchSession _session = null!;
    private MatchConfig _config = null!;
    private MatchDayContext _ctx = null!;
    private GameState _gameState = null!;
    private Club _playerClub = null!;

    // UI
    private MatchPitchView _pitchView = null!;
    private VBoxContainer _eventFeed = null!;
    private ScrollContainer _eventScroll = null!;
    private Label _scoreLabel = null!;
    private Label _tickLabel = null!;
    private ProgressBar _possessionBar = null!;
    private Label _possHomeLabel = null!;
    private Label _possAwayLabel = null!;
    private HBoxContainer _controlRow = null!;
    private VBoxContainer _rightPanel = null!;

    // Simulation state
    private double _tickTimer;
    private float _tickInterval = 1.0f;
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

        // Reload existing events if resuming from halftime
        if (_session.State.Events.Count > 0)
        {
            foreach (MatchEvent evt in _session.State.Events)
                AddEventToFeed(evt);
            UpdateDisplay();
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
            AddEventToFeed(evt);

        UpdateDisplay();

        if (_session.IsHalfTimeReached)
        {
            _waitingForHalftime = true;
            _tickLabel!.Text = "HT";
            ShowActionButton("Halftime", UITheme.Yellow, UITheme.TextDark, OnHalftime);
        }

        if (_session.IsMatchFinished)
        {
            _tickLabel!.Text = "FT";
            ShowActionButton("Full Time — Results", UITheme.Green, UITheme.TextLight, OnFullTime);
        }
    }

    // ─── UI Building ──────────────────────────────────────────────────

    private void BuildUI()
    {
        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = UITheme.PaddingSmall;
        root.OffsetRight = -UITheme.PaddingSmall;
        root.OffsetTop = UITheme.PaddingSmall;
        root.OffsetBottom = -UITheme.PaddingSmall;
        root.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        AddChild(root);

        // ─── Score bar (compact) ──────────────────────────────────
        var scoreBar = new HBoxContainer();
        scoreBar.AddThemeConstantOverride("separation", UITheme.Padding);
        root.AddChild(scoreBar);

        _scoreLabel = UITheme.CreateLabel("",
            UITheme.FontSizeHeading, UITheme.TextDark);
        _scoreLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scoreBar.AddChild(_scoreLabel);

        _tickLabel = UITheme.CreateLabel("0'",
            UITheme.FontSizeHeading, UITheme.Green);
        scoreBar.AddChild(_tickLabel);

        _possHomeLabel = UITheme.CreateLabel("50%", UITheme.FontSizeCaption, UITheme.Blue);
        scoreBar.AddChild(_possHomeLabel);

        _possessionBar = UITheme.CreateProgressBar(50, 100, UITheme.Blue, UITheme.Red,
            new Vector2(150, 10));
        _possessionBar.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        scoreBar.AddChild(_possessionBar);

        _possAwayLabel = UITheme.CreateLabel("50%", UITheme.FontSizeCaption, UITheme.Red);
        scoreBar.AddChild(_possAwayLabel);

        // ─── Main content: pitch + event feed ─────────────────────
        var content = new HBoxContainer();
        content.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        content.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(content);

        // Left: pitch view
        _pitchView = new MatchPitchView
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.8f,
        };
        content.AddChild(_pitchView);

        Formation homeForm = _config.HomeTactics?.Formation ?? Formation.F442;
        Formation awayForm = _config.AwayTactics?.Formation ?? Formation.F442;
        _pitchView.Setup(_config, homeForm, awayForm);

        // Right: event feed + controls
        _rightPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.0f,
        };
        _rightPanel.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        content.AddChild(_rightPanel);

        _eventScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        _rightPanel.AddChild(_eventScroll);

        _eventFeed = new VBoxContainer();
        _eventFeed.AddThemeConstantOverride("separation", 3);
        _eventScroll.AddChild(_eventFeed);

        // ─── Control bar ──────────────────────────────────────────
        _controlRow = new HBoxContainer();
        _controlRow.AddThemeConstantOverride("separation", 4);
        _rightPanel.AddChild(_controlRow);

        BuildControlBar();

        UpdateDisplay();
    }

    private void BuildControlBar()
    {
        foreach (Node child in _controlRow.GetChildren())
            child.QueueFree();

        // Speed buttons
        AddSpeedBtn("||", 0f, _paused);
        AddSpeedBtn("1x", 1.5f);
        AddSpeedBtn("2x", 0.75f);
        AddSpeedBtn("4x", 0.3f);
        AddSpeedBtn("8x", 0.1f);

        _controlRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // Style buttons
        foreach (TacticalStyle style in System.Enum.GetValues<TacticalStyle>())
        {
            bool sel = style == _currentStyle;
            Color color = style switch
            {
                TacticalStyle.Attacking => UITheme.Red,
                TacticalStyle.Defensive => UITheme.Blue,
                _ => UITheme.Green,
            };
            string label = style switch
            {
                TacticalStyle.Attacking => "ATK",
                TacticalStyle.Defensive => "DEF",
                _ => "BAL",
            };

            var btn = UITheme.CreateFlatButton(label,
                sel ? color : UITheme.Border,
                sel ? UITheme.TextLight : UITheme.TextSecondary);
            btn.CustomMinimumSize = new Vector2(40, 28);
            btn.AddThemeFontSizeOverride("font_size", 10);
            TacticalStyle captured = style;
            btn.Pressed += () => { _currentStyle = captured; BuildControlBar(); };
            _controlRow.AddChild(btn);
        }

        // Subs button
        var subBtn = UITheme.CreateFlatButton("Subs", UITheme.Orange, UITheme.TextLight);
        subBtn.CustomMinimumSize = new Vector2(44, 28);
        subBtn.AddThemeFontSizeOverride("font_size", 10);
        subBtn.Pressed += ShowSubstitutionPanel;
        _controlRow.AddChild(subBtn);
    }

    private void AddSpeedBtn(string label, float interval, bool forceActive = false)
    {
        bool active = forceActive || (!_paused && System.Math.Abs(_tickInterval - interval) < 0.01f);
        var btn = UITheme.CreateFlatButton(label,
            active ? UITheme.Blue : UITheme.Border,
            active ? UITheme.TextLight : UITheme.TextSecondary);
        btn.CustomMinimumSize = new Vector2(32, 28);
        btn.AddThemeFontSizeOverride("font_size", 10);

        if (label == "||")
            btn.Pressed += () => { _paused = !_paused; BuildControlBar(); };
        else
        {
            float cap = interval;
            btn.Pressed += () => { _paused = false; _tickInterval = cap; BuildControlBar(); };
        }

        _controlRow.AddChild(btn);
    }

    // ─── Display Updates ──────────────────────────────────────────────

    private void UpdateDisplay()
    {
        MatchState s = _session.State;

        _scoreLabel!.Text = $"{_config.HomeTeam.Name}  {s.ScoreHome} - {s.ScoreAway}  {_config.AwayTeam.Name}";

        int displayTick = s.Phase == MatchPhase.SecondHalf ? 45 + _session.HalfTick : s.CurrentTick;
        _tickLabel!.Text = $"{displayTick}'";

        float pct = s.PossessionHome * 100;
        _possessionBar!.Value = pct;
        _possHomeLabel!.Text = $"{pct:F0}%";
        _possAwayLabel!.Text = $"{100 - pct:F0}%";

        // Update pitch ratings
        _pitchView.UpdateRatings(s.PlayerRatings);
    }

    // ─── Event Feed ───────────────────────────────────────────────────

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

        Color color = evt.Type switch
        {
            EventType.Goal => UITheme.Green,
            EventType.YellowCard => UITheme.Yellow,
            EventType.RedCard => UITheme.Red,
            EventType.Substitution => UITheme.Orange,
            _ => UITheme.TextSecondary,
        };

        bool isGoal = evt.Type == EventType.Goal;

        if (isGoal)
        {
            var goalCard = UITheme.CreateCard(UITheme.Green);
            goalCard.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _eventFeed.AddChild(goalCard);

            var goalRow = UITheme.CreateIconLabel("football", $"{evt.Tick}' {evt.Description}",
                UITheme.FontSizeSmall, UITheme.Green, new Vector2(14, 14));
            goalCard.AddChild(goalRow);

            Anim.PulseOnce(goalCard, 1.05f);
        }
        else
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);
            _eventFeed.AddChild(row);

            row.AddChild(UITheme.CreateIcon(iconName, new Vector2(12, 12), color));
            row.AddChild(UITheme.CreateLabel($"{evt.Tick}' {evt.Description}", 11, color));
        }

        // Auto-scroll
        GetTree().CreateTimer(0.05f).Timeout += () =>
        {
            if (IsInstanceValid(_eventScroll))
                _eventScroll.ScrollVertical = (int)_eventScroll.GetVScrollBar().MaxValue;
        };
    }

    // ─── Substitutions ────────────────────────────────────────────────

    private void ShowSubstitutionPanel()
    {
        _paused = true;
        foreach (Node child in _eventFeed.GetChildren())
            child.QueueFree();

        bool isHome = _playerClub.Id == _config.HomeTeam.Id;
        int subsUsed = isHome ? _session.State.HomeSubstitutionsUsed : _session.State.AwaySubstitutionsUsed;
        List<int> activeIds = isHome ? _session.State.HomeActivePlayerIds : _session.State.AwayActivePlayerIds;

        _eventFeed.AddChild(UITheme.CreateLabel($"Subs ({subsUsed}/3)", UITheme.FontSizeBody, UITheme.Orange));

        if (subsUsed >= 3)
        {
            _eventFeed.AddChild(UITheme.CreateLabel("No subs remaining.", UITheme.FontSizeSmall, UITheme.TextSecondary));
        }
        else
        {
            var activeSet = new HashSet<int>(activeIds);
            foreach (Player player in _playerClub.Team.Players.Where(p => activeSet.Contains(p.Id)))
            {
                float stamina = _session.State.PlayerStamina.GetValueOrDefault(player.Id, 0);
                float rating = _session.State.PlayerRatings.GetValueOrDefault(player.Id, 6f);

                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 4);
                _eventFeed.AddChild(row);

                row.AddChild(UITheme.CreateLabel($"{player.PrimaryPosition}", 10, UITheme.TextSecondary));
                var name = UITheme.CreateLabel(player.Name, 11, UITheme.TextDark);
                name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.AddChild(name);
                row.AddChild(UITheme.CreateLabel($"{stamina:F0}", 10,
                    stamina < 30 ? UITheme.Red : UITheme.TextSecondary));

                Player cap = player;
                var outBtn = UITheme.CreateFlatButton("Out", UITheme.Red, UITheme.TextLight);
                outBtn.CustomMinimumSize = new Vector2(36, 24);
                outBtn.AddThemeFontSizeOverride("font_size", 10);
                outBtn.Pressed += () => ShowSubInOptions(cap);
                row.AddChild(outBtn);
            }
        }

        var cancelBtn = UITheme.CreateFlatButton("Cancel", UITheme.Border, UITheme.TextPrimary);
        cancelBtn.Pressed += () => { _paused = false; RestoreEventFeed(); };
        _eventFeed.AddChild(cancelBtn);
    }

    private void ShowSubInOptions(Player playerOut)
    {
        foreach (Node child in _eventFeed.GetChildren())
            child.QueueFree();

        bool isHome = _playerClub.Id == _config.HomeTeam.Id;
        var activeSet = new HashSet<int>(isHome
            ? _session.State.HomeActivePlayerIds
            : _session.State.AwayActivePlayerIds);

        _eventFeed.AddChild(UITheme.CreateLabel($"Replace {playerOut.Name}:", UITheme.FontSizeSmall, UITheme.Orange));

        foreach (Player player in _playerClub.Team.Players.Where(p => !activeSet.Contains(p.Id)))
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);
            _eventFeed.AddChild(row);

            float ovr = player.PrimaryPosition == PlayerPosition.GK
                ? player.Attributes.GoalkeeperOverall
                : player.Attributes.OutfieldOverall;

            row.AddChild(UITheme.CreateLabel($"{ovr:F0}", 10, UITheme.StatColor((int)ovr)));
            row.AddChild(UITheme.CreateLabel($"{player.PrimaryPosition}", 10, UITheme.TextSecondary));
            var name = UITheme.CreateLabel(player.Name, 11, UITheme.TextDark);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(name);

            Player capIn = player;
            var inBtn = UITheme.CreateFlatButton("In", UITheme.Green, UITheme.TextLight);
            inBtn.CustomMinimumSize = new Vector2(32, 24);
            inBtn.AddThemeFontSizeOverride("font_size", 10);
            inBtn.Pressed += () => ExecuteSub(playerOut, capIn);
            row.AddChild(inBtn);
        }

        var backBtn = UITheme.CreateFlatButton("Back", UITheme.Border, UITheme.TextPrimary);
        backBtn.Pressed += ShowSubstitutionPanel;
        _eventFeed.AddChild(backBtn);
    }

    private void ExecuteSub(Player playerOut, Player playerIn)
    {
        bool isHome = _playerClub.Id == _config.HomeTeam.Id;
        _session.ApplySubstitution(
            new Substitution { PlayerOutId = playerOut.Id, PlayerInId = playerIn.Id }, isHome);

        // Update pitch with new active players
        _pitchView.UpdateActivePlayers(
            _session.State.HomeActivePlayerIds,
            _session.State.AwayActivePlayerIds);

        _paused = false;
        RestoreEventFeed();
    }

    private void RestoreEventFeed()
    {
        foreach (Node child in _eventFeed.GetChildren())
            child.QueueFree();
        foreach (MatchEvent evt in _session.State.Events)
            AddEventToFeed(evt);
    }

    // ─── Navigation ───────────────────────────────────────────────────

    private void ShowActionButton(string text, Color bg, Color fg, System.Action action)
    {
        var btn = UITheme.CreateButton(text, bg, fg);
        btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        btn.CustomMinimumSize = new Vector2(0, 40);
        btn.Pressed += () => action();
        _rightPanel.AddChild(btn);
        Anim.PulseOnce(btn, 1.05f);
    }

    private void OnHalftime()
    {
        Halftime.PendingMatchState = _session.State;
        Halftime.PendingConfig = _config;
        Halftime.PendingContext = _ctx;
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
}
