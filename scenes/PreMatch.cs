using Godot;
using ElevenLegends.Data.Enums;
using PlayerPosition = ElevenLegends.Data.Enums.Position;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Pre-match screen — formation, tactics, opponent info.
/// Formation selector rearranges the saved lineup (doesn't replace it).
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

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = UITheme.PaddingLarge;
        root.OffsetRight = -UITheme.PaddingLarge;
        root.OffsetTop = UITheme.Padding;
        root.OffsetBottom = -UITheme.Padding;
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

        string venueText = isHome ? "Home" : "Away";
        var venueLabel = UITheme.CreateIconLabel(
            isHome ? "football" : "arrow-swap", venueText,
            UITheme.FontSizeBody, UITheme.TextSecondary);
        venueLabel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        headerVbox.AddChild(venueLabel);

        // ─── Tactics card ─────────────────────────────────────────
        var tacticsCard = UITheme.CreateCard(UITheme.Blue);
        root.AddChild(tacticsCard);

        var tacticsVbox = new VBoxContainer();
        tacticsVbox.AddThemeConstantOverride("separation", 10);
        tacticsCard.AddChild(tacticsVbox);

        tacticsVbox.AddChild(UITheme.CreateLabel("Tactics",
            UITheme.FontSizeHeading, UITheme.Blue));

        // Formation selector with OVR preview
        tacticsVbox.AddChild(UITheme.CreateLabel("Formation",
            UITheme.FontSizeSmall, UITheme.TextSecondary));

        var formRow = new HBoxContainer();
        formRow.AddThemeConstantOverride("separation", 6);
        tacticsVbox.AddChild(formRow);

        foreach (Formation f in Formation.Presets)
        {
            bool sel = f.Name == _selectedFormation.Name;
            float avgOvr = FormationOptimizer.AverageOverall(
                _playerClub.Team.Players, f);

            var btnVbox = new VBoxContainer();
            btnVbox.AddThemeConstantOverride("separation", 2);

            var btn = UITheme.CreateFlatButton(f.Name,
                sel ? UITheme.Blue : UITheme.Border,
                sel ? UITheme.TextLight : UITheme.TextPrimary);
            btn.CustomMinimumSize = new Vector2(80, 36);
            Formation captured = f;
            btn.Pressed += () => OnFormationChanged(captured);
            btnVbox.AddChild(btn);

            btnVbox.AddChild(UITheme.CreateLabel($"OVR {avgOvr:F0}",
                UITheme.FontSizeCaption, sel ? UITheme.Blue : UITheme.TextSecondary,
                HorizontalAlignment.Center));

            formRow.AddChild(btnVbox);
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
            (string iconName, Color color) = style switch
            {
                TacticalStyle.Attacking => ("sword", UITheme.Red),
                TacticalStyle.Defensive => ("shield-check", UITheme.Blue),
                _ => ("scale", UITheme.Green),
            };

            var btnRow = UITheme.CreateIconLabel(iconName, $"{style}",
                UITheme.FontSizeSmall, sel ? UITheme.TextLight : UITheme.TextPrimary,
                iconTint: sel ? UITheme.TextLight : color);

            var btn = UITheme.CreateFlatButton("",
                sel ? color : UITheme.Border);
            btn.CustomMinimumSize = new Vector2(130, 40);
            // Replace the flat button's child with our icon+label
            foreach (Node child in btn.GetChildren()) child.QueueFree();
            btn.AddChild(btnRow);
            TacticalStyle captured = style;
            btn.Pressed += () => { _selectedStyle = captured; BuildUI(); };
            styleRow.AddChild(btn);
        }

        // ─── Action buttons ───────────────────────────────────────
        var actionRow = new HBoxContainer();
        actionRow.AddThemeConstantOverride("separation", UITheme.Padding);
        root.AddChild(actionRow);

        var squadBtn = UITheme.CreateButton("Edit Squad", UITheme.BlueDark);
        squadBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/Squad.tscn");
        actionRow.AddChild(squadBtn);

        var startBtn = UITheme.CreateButton("Start Match!", UITheme.Green);
        startBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        startBtn.CustomMinimumSize = new Vector2(0, 56);
        startBtn.Pressed += OnStartMatch;
        actionRow.AddChild(startBtn);

        // ─── Entrance animations ──────────────────────────────────
        Anim.StaggerChildren(root, stagger: 0.06f, useScale: false);
    }

    private void OnFormationChanged(Formation formation)
    {
        // Rearrange existing lineup into new formation positions
        IReadOnlyList<int> currentLineup = _playerClub.Team.StartingLineup;
        IReadOnlyList<int> rearranged = RearrangeForFormation(currentLineup, formation);

        // Save rearranged lineup back to team
        _playerClub.Team = _playerClub.Team with { StartingLineup = rearranged.ToList() };

        _selectedFormation = formation;
        BuildUI();
    }

    private IReadOnlyList<int> RearrangeForFormation(
        IReadOnlyList<int> playerIds, Formation formation)
    {
        var playerMap = _playerClub.Team.Players.ToDictionary(p => p.Id);
        var available = new HashSet<int>(playerIds.Where(id => id > 0));
        int[] result = new int[formation.Positions.Count];

        // GK first (scarcest), then others
        int[] slotOrder = Enumerable.Range(0, formation.Positions.Count)
            .OrderBy(i => formation.Positions[i] == PlayerPosition.GK ? 0 : 1)
            .ToArray();

        foreach (int slotIdx in slotOrder)
        {
            PlayerPosition slotPos = formation.Positions[slotIdx];
            int bestId = -1;
            float bestScore = float.MinValue;

            foreach (int pid in available)
            {
                if (!playerMap.TryGetValue(pid, out Player? p)) continue;
                float score = p.Attributes.OverallForPosition(
                    (ElevenLegends.Data.Enums.Position)slotPos);
                if (p.PrimaryPosition == (ElevenLegends.Data.Enums.Position)slotPos)
                    score += 20f;
                else if (p.SecondaryPosition == (ElevenLegends.Data.Enums.Position)slotPos)
                    score += 10f;
                if (score > bestScore) { bestScore = score; bestId = pid; }
            }

            if (bestId > 0) { result[slotIdx] = bestId; available.Remove(bestId); }
        }

        return result;
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

        MatchSimulation.PendingMatchState = state;
        MatchSimulation.PendingConfig = config;
        MatchSimulation.PendingContext = _ctx;

        SceneManager.Instance.ChangeScene("res://scenes/MatchSimulation.tscn");
    }
}
