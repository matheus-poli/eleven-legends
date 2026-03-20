using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

namespace ElevenLegends.Scenes;

/// <summary>
/// Pre-match screen — formation, tactics, opponent info.
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

        if (_ctx.PlayerFixture == null)
        {
            // Player eliminated — skip match
            var eliminated = Theme.CreateLabel(
                "Your club was eliminated. No match today.",
                Theme.FontSizeHeading, Theme.TextSecondary, HorizontalAlignment.Center);
            root.AddChild(eliminated);

            var skipBtn = Theme.CreateButton("Continue", Theme.Blue);
            skipBtn.Pressed += () =>
            {
                _gameState.FinishDay(_ctx, null);
                SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
            };
            root.AddChild(skipBtn);
            return;
        }

        bool isHome = _ctx.PlayerFixture.HomeClubId == _playerClub.Id;
        var opponentId = isHome ? _ctx.PlayerFixture.AwayClubId : _ctx.PlayerFixture.HomeClubId;
        var opponent = _gameState.Clubs.First(c => c.Id == opponentId);

        // Match header
        var header = Theme.CreateLabel(
            $"⚽ {_playerClub.Name}  vs  {opponent.Name}",
            Theme.FontSizeTitle, Theme.TextPrimary, HorizontalAlignment.Center);
        root.AddChild(header);

        var venue = Theme.CreateLabel(
            isHome ? "🏟️ Home" : "✈️ Away",
            Theme.FontSizeBody, Theme.TextSecondary, HorizontalAlignment.Center);
        root.AddChild(venue);

        // Tactics section
        var tacticsCard = Theme.CreateCard();
        root.AddChild(tacticsCard);

        var tacticsVbox = new VBoxContainer();
        tacticsVbox.AddThemeConstantOverride("separation", 12);
        tacticsCard.AddChild(tacticsVbox);

        tacticsVbox.AddChild(Theme.CreateLabel("🧠 Tactics", Theme.FontSizeHeading, Theme.Blue));

        // Formation buttons
        var formLabel = Theme.CreateLabel("Formation:", Theme.FontSizeBody, Theme.TextSecondary);
        tacticsVbox.AddChild(formLabel);

        var formHbox = new HBoxContainer();
        formHbox.AddThemeConstantOverride("separation", 8);
        tacticsVbox.AddChild(formHbox);

        var formations = new[] { Formation.F442, Formation.F433, Formation.F352, Formation.F4231, Formation.F532 };
        foreach (var f in formations)
        {
            bool isSelected = f.Name == _selectedFormation.Name;
            var btn = Theme.CreateButton(f.Name, isSelected ? Theme.Green : Theme.Border,
                isSelected ? Theme.TextLight : Theme.TextPrimary);
            btn.CustomMinimumSize = new Vector2(100, 44);
            var capturedF = f;
            btn.Pressed += () =>
            {
                _selectedFormation = capturedF;
                BuildUI();
            };
            formHbox.AddChild(btn);
        }

        // Style buttons
        var styleLabel = Theme.CreateLabel("Style:", Theme.FontSizeBody, Theme.TextSecondary);
        tacticsVbox.AddChild(styleLabel);

        var styleHbox = new HBoxContainer();
        styleHbox.AddThemeConstantOverride("separation", 8);
        tacticsVbox.AddChild(styleHbox);

        foreach (TacticalStyle style in System.Enum.GetValues<TacticalStyle>())
        {
            bool isSelected = style == _selectedStyle;
            string emoji = style switch
            {
                TacticalStyle.Attacking => "⚔️",
                TacticalStyle.Defensive => "🛡️",
                _ => "⚖️"
            };
            var btn = Theme.CreateButton($"{emoji} {style}", isSelected ? Theme.Blue : Theme.Border,
                isSelected ? Theme.TextLight : Theme.TextPrimary);
            btn.CustomMinimumSize = new Vector2(140, 44);
            var capturedStyle = style;
            btn.Pressed += () =>
            {
                _selectedStyle = capturedStyle;
                BuildUI();
            };
            styleHbox.AddChild(btn);
        }

        // Start match button
        var startBtn = Theme.CreateButton("🏟️  Start Match!", Theme.Green);
        startBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        startBtn.CustomMinimumSize = new Vector2(300, 64);
        startBtn.Pressed += OnStartMatch;
        root.AddChild(startBtn);
    }

    private void OnStartMatch()
    {
        // Build tactical setup
        var lineup = _playerClub.Team.StartingLineup.ToList();
        var tactics = new TacticalSetup
        {
            Formation = _selectedFormation,
            Style = _selectedStyle,
            StartingPlayerIds = lineup
        };

        var config = _gameState.BuildPlayerMatchConfig(_ctx, tactics);

        // Simulate first half
        var (state, _) = MatchSimulator.SimulateFirstHalf(config);

        // Pass data through static fields (Godot scene change limitation)
        MatchSimScreen.PendingMatchState = state;
        MatchSimScreen.PendingConfig = config;
        MatchSimScreen.PendingContext = _ctx;

        SceneManager.Instance.ChangeScene("res://scenes/MatchSimulation.tscn");
    }
}
