using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Pre-match screen — formation, tactics, opponent info.
/// Duolingo-style with accent cards and 3D buttons.
/// </summary>
public partial class PreMatch : Control
{
    private GameState _gameState = null!;
    private Club _playerClub = null!;
    private MatchDayContext _ctx = null!;
    private TacticalStyle _selectedStyle = TacticalStyle.Balanced;
    private Formation _selectedFormation = Formation.F442;

    public override void _Ready()
    {
        _gameState = SceneManager.Instance.CurrentGameState!;
        _playerClub = _gameState.PlayerClub;
        _ctx = _gameState.PrepareMatchDay();

        BuildUI();
    }

    private void BuildUI()
    {
        foreach (Node child in GetChildren())
            child.QueueFree();

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

        if (_ctx.PlayerFixture == null)
        {
            BuildEliminatedView(root);
            return;
        }

        bool isHome = _ctx.PlayerFixture.HomeClubId == _playerClub.Id;
        int opponentId = isHome ? _ctx.PlayerFixture.AwayClubId : _ctx.PlayerFixture.HomeClubId;
        Club opponent = _gameState.Clubs.First(c => c.Id == opponentId);

        // ─── Match header card ────────────────────────────────────
        var headerCard = UITheme.CreateCard(UITheme.Green);
        root.AddChild(headerCard);

        var headerVbox = new VBoxContainer();
        headerVbox.AddThemeConstantOverride("separation", 4);
        headerCard.AddChild(headerVbox);

        headerVbox.AddChild(UITheme.CreateLabel(
            $"{_playerClub.Name}  vs  {opponent.Name}",
            UITheme.FontSizeTitle, UITheme.TextDark, HorizontalAlignment.Center));

        headerVbox.AddChild(UITheme.CreateLabel(
            isHome ? "🏟️  Home" : "✈️  Away",
            UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center));

        // ─── Tactics card ─────────────────────────────────────────
        var tacticsCard = UITheme.CreateCard(UITheme.Blue);
        root.AddChild(tacticsCard);

        var tacticsVbox = new VBoxContainer();
        tacticsVbox.AddThemeConstantOverride("separation", 12);
        tacticsCard.AddChild(tacticsVbox);

        tacticsVbox.AddChild(UITheme.CreateLabel("Tactics",
            UITheme.FontSizeHeading, UITheme.Blue));

        // Formation selector
        tacticsVbox.AddChild(UITheme.CreateLabel("Formation",
            UITheme.FontSizeSmall, UITheme.TextSecondary));

        var formRow = new HBoxContainer();
        formRow.AddThemeConstantOverride("separation", 8);
        tacticsVbox.AddChild(formRow);

        Formation[] formations = [Formation.F442, Formation.F433, Formation.F352, Formation.F4231, Formation.F532];
        foreach (Formation f in formations)
        {
            bool sel = f.Name == _selectedFormation.Name;
            var btn = UITheme.CreateFlatButton(f.Name,
                sel ? UITheme.Blue : UITheme.Border,
                sel ? UITheme.TextLight : UITheme.TextPrimary);
            btn.CustomMinimumSize = new Vector2(90, 40);
            Formation captured = f;
            btn.Pressed += () => { _selectedFormation = captured; BuildUI(); };
            formRow.AddChild(btn);
        }

        // Style selector
        tacticsVbox.AddChild(UITheme.CreateLabel("Playing Style",
            UITheme.FontSizeSmall, UITheme.TextSecondary));

        var styleRow = new HBoxContainer();
        styleRow.AddThemeConstantOverride("separation", 8);
        tacticsVbox.AddChild(styleRow);

        foreach (TacticalStyle style in System.Enum.GetValues<TacticalStyle>())
        {
            bool sel = style == _selectedStyle;
            (string emoji, Color color) = style switch
            {
                TacticalStyle.Attacking => ("⚔️", UITheme.Red),
                TacticalStyle.Defensive => ("🛡️", UITheme.Blue),
                _ => ("⚖️", UITheme.Green),
            };

            var btn = UITheme.CreateFlatButton($"{emoji} {style}",
                sel ? color : UITheme.Border,
                sel ? UITheme.TextLight : UITheme.TextPrimary);
            btn.CustomMinimumSize = new Vector2(130, 40);
            TacticalStyle captured = style;
            btn.Pressed += () => { _selectedStyle = captured; BuildUI(); };
            styleRow.AddChild(btn);
        }

        // ─── Start match button ───────────────────────────────────
        var startBtn = UITheme.CreateButton("Start Match!", UITheme.Green);
        startBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        startBtn.CustomMinimumSize = new Vector2(300, 64);
        startBtn.Pressed += OnStartMatch;
        root.AddChild(startBtn);

        // ─── Entrance animations ──────────────────────────────────
        Anim.StaggerChildren(root, stagger: 0.06f, useScale: false);
    }

    private void BuildEliminatedView(VBoxContainer root)
    {
        root.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        root.AddChild(UITheme.CreateLabel(
            "Your club was eliminated. No match today.",
            UITheme.FontSizeHeading, UITheme.TextSecondary, HorizontalAlignment.Center));

        var skipBtn = UITheme.CreateButton("Continue", UITheme.Blue);
        skipBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        skipBtn.Pressed += () =>
        {
            _gameState.FinishDay(_ctx, null);
            SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        };
        root.AddChild(skipBtn);
        root.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
    }

    private void OnStartMatch()
    {
        var lineup = _playerClub.Team.StartingLineup.ToList();
        var tactics = new TacticalSetup
        {
            Formation = _selectedFormation,
            Style = _selectedStyle,
            StartingPlayerIds = lineup,
        };

        MatchConfig config = _gameState.BuildPlayerMatchConfig(_ctx, tactics);
        (MatchState state, _) = MatchSimulator.SimulateFirstHalf(config);

        MatchSimScreen.PendingMatchState = state;
        MatchSimScreen.PendingConfig = config;
        MatchSimScreen.PendingContext = _ctx;

        SceneManager.Instance.ChangeScene("res://scenes/MatchSimulation.tscn");
    }
}
