using Godot;
using ElevenLegends.Data.Enums;
using PlayerPosition = ElevenLegends.Data.Enums.Position;
using ElevenLegends.Data.Models;
using ElevenLegends.Scenes.Components;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Squad management — football pitch with draggable player cards.
/// Left: pitch view with formation. Right: bench/reserve list.
/// Top: formation selector with OVR preview.
/// </summary>
public partial class Squad : Control
{
    /// <summary>Set before navigating to Squad to control where Back returns to.</summary>
    public static string ReturnScene = "res://scenes/DayHub.tscn";

    private GameState _gameState = null!;
    private Club _playerClub = null!;
    private PitchView _pitchView = null!;
    private VBoxContainer _benchList = null!;
    private VBoxContainer _benchPanel = null!;
    private HBoxContainer _formationRow = null!;
    private Formation _currentFormation = Formation.F442;
    private Player? _selectedPlayer;

    public override void _Ready()
    {
        _gameState = SceneManager.Instance.CurrentGameState!;
        _playerClub = _gameState.PlayerClub;
        _currentFormation = Formation.F442;
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
        root.OffsetLeft = UITheme.PaddingSmall;
        root.OffsetRight = -UITheme.PaddingSmall;
        root.OffsetTop = UITheme.PaddingSmall;
        root.OffsetBottom = -UITheme.PaddingSmall;
        root.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        AddChild(root);

        // ─── Top bar: title + formation selector + back ──────────
        var topBar = new HBoxContainer();
        topBar.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        root.AddChild(topBar);

        topBar.AddChild(UITheme.CreateLabel(
            $"{_playerClub.Name}", UITheme.FontSizeHeading, UITheme.Blue));

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        topBar.AddChild(spacer);

        var backBtn = UITheme.CreateFlatButton("Back", UITheme.Border, UITheme.TextPrimary);
        backBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene(ReturnScene);
        topBar.AddChild(backBtn);

        // ─── Formation selector row ──────────────────────────────
        _formationRow = new HBoxContainer();
        _formationRow.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        _formationRow.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        root.AddChild(_formationRow);

        BuildFormationButtons();

        // ─── Main content: pitch + bench ─────────────────────────
        var content = new HBoxContainer();
        content.AddThemeConstantOverride("separation", UITheme.Padding);
        content.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(content);

        // Pitch view (left, takes most space)
        _pitchView = new PitchView
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(500, 400),
        };
        content.AddChild(_pitchView);

        // Bench panel (right)
        _benchPanel = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(220, 0),
        };
        _benchPanel.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        content.AddChild(_benchPanel);

        _benchPanel.AddChild(UITheme.CreateLabel("Bench",
            UITheme.FontSizeBody, UITheme.TextSecondary));

        var benchScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        _benchPanel.AddChild(benchScroll);

        _benchList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _benchList.AddThemeConstantOverride("separation", 4);
        benchScroll.AddChild(_benchList);

        // Setup pitch with saved lineup (or optimizer fallback if no saved lineup)
        IReadOnlyList<int> savedLineup = _playerClub.Team.StartingLineup;
        IReadOnlyList<int> initialIds = savedLineup.Count == 11
            ? savedLineup
            : FormationOptimizer.OptimalLineup(_playerClub.Team.Players, _currentFormation);
        _pitchView.Setup(_playerClub.Team.Players, _currentFormation, initialIds);
        _pitchView.LineupChanged += RebuildBench;
        _pitchView.PlayerClicked += OnPlayerClicked;

        RebuildBench();

        // Entrance animation
        Anim.FadeIn(_pitchView, delay: 0.1f, duration: 0.4f);
    }

    private void BuildFormationButtons()
    {
        foreach (Node child in _formationRow.GetChildren())
            child.QueueFree();

        foreach (Formation formation in Formation.Presets)
        {
            float avgOvr = FormationOptimizer.AverageOverall(
                _playerClub.Team.Players, formation);

            bool isActive = formation.Name == _currentFormation.Name;

            var btn = new VBoxContainer();
            btn.AddThemeConstantOverride("separation", 2);

            Button formBtn;
            if (isActive)
            {
                formBtn = UITheme.CreateButton(formation.Name, UITheme.Green, UITheme.TextLight);
            }
            else
            {
                formBtn = UITheme.CreateFlatButton(formation.Name, UITheme.Border, UITheme.TextPrimary);
            }
            formBtn.CustomMinimumSize = new Vector2(90, 36);

            Formation captured = formation;
            formBtn.Pressed += () => OnFormationSelected(captured);
            btn.AddChild(formBtn);

            btn.AddChild(UITheme.CreateLabel($"OVR {avgOvr:F0}",
                UITheme.FontSizeCaption, isActive ? UITheme.Green : UITheme.TextSecondary,
                HorizontalAlignment.Center));

            _formationRow.AddChild(btn);
        }
    }

    private void OnFormationSelected(Formation formation)
    {
        // Keep the same 11 players on the pitch, just rearrange them
        // into the new formation's slot positions by best fit
        IReadOnlyList<int> currentOnPitch = _pitchView.SlotPlayerIds
            .Where(id => id > 0).ToList();

        IReadOnlyList<int> rearranged = RearrangeForFormation(
            currentOnPitch, formation);

        _currentFormation = formation;
        _pitchView.SetFormation(_currentFormation, rearranged);

        BuildFormationButtons();
        RebuildBench();
    }

    /// <summary>
    /// Rearranges the given player IDs into the best-fit slots for a new formation.
    /// Keeps the same 11 players — only changes which slot each occupies.
    /// </summary>
    private IReadOnlyList<int> RearrangeForFormation(
        IReadOnlyList<int> playerIds, Formation formation)
    {
        var playerMap = _playerClub.Team.Players.ToDictionary(p => p.Id);
        var available = new HashSet<int>(playerIds);
        int[] result = new int[formation.Positions.Count];

        // Assign each slot: pick the best available player for that position
        // Prioritize exact position match, then secondary, then best OVR
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

                // Strong preference for natural position match
                if (p.PrimaryPosition == (ElevenLegends.Data.Enums.Position)slotPos)
                    score += 20f;
                else if (p.SecondaryPosition == (ElevenLegends.Data.Enums.Position)slotPos)
                    score += 10f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = pid;
                }
            }

            if (bestId > 0)
            {
                result[slotIdx] = bestId;
                available.Remove(bestId);
            }
        }

        return result;
    }

    private void RebuildBench()
    {
        foreach (Node child in _benchList.GetChildren())
            child.QueueFree();

        IReadOnlyList<Player> bench = _pitchView.GetBenchPlayers();

        foreach (Player player in bench.OrderBy(p => PositionOrder(p.PrimaryPosition)))
        {
            var card = CreateBenchRow(player);
            _benchList.AddChild(card);
        }
    }

    private HoverCard CreateBenchRow(Player player)
    {
        var card = HoverCard.Create();
        card.DisableTilt = true;
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        card.CustomMinimumSize = new Vector2(0, 40);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        card.AddChild(hbox);

        float ovr = player.PrimaryPosition == PlayerPosition.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        var badge = UITheme.CreateBadge($"{ovr:F0}",
            UITheme.StatColor((int)ovr), UITheme.TextLight,
            UITheme.FontSizeCaption, new Vector2(32, 24));
        hbox.AddChild(badge);

        var posLabel = UITheme.CreateLabel($"{player.PrimaryPosition}",
            UITheme.FontSizeCaption, UITheme.TextSecondary);
        posLabel.CustomMinimumSize = new Vector2(28, 0);
        hbox.AddChild(posLabel);

        var nameLabel = UITheme.CreateLabel(player.Name,
            UITheme.FontSizeSmall, UITheme.TextDark);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hbox.AddChild(nameLabel);

        // Click to assign to first empty or worst-fit slot
        Player capturedPlayer = player;
        var clickBtn = new Button
        {
            Flat = true,
            Modulate = new Color(1, 1, 1, 0),
        };
        clickBtn.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        clickBtn.Pressed += () => OnBenchPlayerClicked(capturedPlayer);
        card.AddChild(clickBtn);

        return card;
    }

    private void OnBenchPlayerClicked(Player player)
    {
        // Find best-fit slot for this player
        int bestSlot = -1;
        float bestDelta = float.MinValue;

        for (int i = 0; i < _currentFormation.Positions.Count; i++)
        {
            Position slotPos = _currentFormation.Positions[i];
            float playerOvr = player.Attributes.OverallForPosition(slotPos);

            int currentId = _pitchView.SlotPlayerIds[i];
            float currentOvr = 0;
            if (currentId > 0)
            {
                Player? currentPlayer = _playerClub.Team.Players.FirstOrDefault(p => p.Id == currentId);
                if (currentPlayer != null)
                    currentOvr = currentPlayer.Attributes.OverallForPosition(slotPos);
            }

            float delta = playerOvr - currentOvr;

            // Prefer matching position
            if (player.PrimaryPosition == slotPos)
                delta += 10;
            else if (player.SecondaryPosition == slotPos)
                delta += 5;

            if (delta > bestDelta)
            {
                bestDelta = delta;
                bestSlot = i;
            }
        }

        if (bestSlot >= 0)
        {
            _pitchView.AssignPlayer(player.Id, bestSlot);
            RebuildBench();
        }
    }

    // Also update the team's starting lineup when leaving
    public override void _ExitTree()
    {
        if (_pitchView != null && _gameState != null)
        {
            List<int> newLineup = _pitchView.SlotPlayerIds
                .Where(id => id > 0).ToList();
            if (newLineup.Count == 11)
            {
                _playerClub.Team = _playerClub.Team with { StartingLineup = newLineup };
            }
        }
    }

    private void OnPlayerClicked(int playerId)
    {
        _selectedPlayer = _playerClub.Team.Players.FirstOrDefault(p => p.Id == playerId);
        if (_selectedPlayer == null) return;
        ShowPlayerDetails();
    }

    private void ShowPlayerDetails()
    {
        if (_selectedPlayer == null || _benchPanel == null) return;

        // Replace bench panel content with player details
        foreach (Node child in _benchPanel.GetChildren())
            child.QueueFree();

        Player player = _selectedPlayer;
        float ovr = player.PrimaryPosition == PlayerPosition.GK
            ? player.Attributes.GoalkeeperOverall
            : player.Attributes.OutfieldOverall;

        // Back to bench button
        var backBtn = UITheme.CreateFlatButton("Back to Bench", UITheme.Border, UITheme.TextPrimary);
        backBtn.Pressed += () =>
        {
            _selectedPlayer = null;
            RebuildBenchPanel();
        };
        _benchPanel.AddChild(backBtn);

        // Player header
        var headerCard = UITheme.CreateCard(UITheme.RatingColor(ovr));
        _benchPanel.AddChild(headerCard);

        var headerVbox = new VBoxContainer();
        headerVbox.AddThemeConstantOverride("separation", 4);
        headerCard.AddChild(headerVbox);

        headerVbox.AddChild(UITheme.CreateLabel(player.Name,
            UITheme.FontSizeHeading, UITheme.TextDark, HorizontalAlignment.Center));

        var infoRow = new HBoxContainer();
        infoRow.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        infoRow.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        headerVbox.AddChild(infoRow);

        infoRow.AddChild(UITheme.CreateBadge($"{ovr:F0}",
            UITheme.RatingColor(ovr), UITheme.TextDark,
            UITheme.FontSizeHeading, new Vector2(48, 40)));

        var infoVbox = new VBoxContainer();
        infoVbox.AddThemeConstantOverride("separation", 2);
        infoRow.AddChild(infoVbox);
        infoVbox.AddChild(UITheme.CreateLabel($"{player.PrimaryPosition}",
            UITheme.FontSizeBody, UITheme.TextDark));
        infoVbox.AddChild(UITheme.CreateLabel($"Age {player.Age}",
            UITheme.FontSizeCaption, UITheme.TextSecondary));
        infoVbox.AddChild(UITheme.CreateLabel($"Morale {player.Morale}%",
            UITheme.FontSizeCaption, player.Morale >= 70 ? UITheme.Green : UITheme.Yellow));

        // Scrollable attributes
        var attrScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        _benchPanel.AddChild(attrScroll);

        var attrVbox = new VBoxContainer();
        attrVbox.AddThemeConstantOverride("separation", 4);
        attrScroll.AddChild(attrVbox);

        PlayerAttributes attrs = player.Attributes;

        AddAttrSection(attrVbox, "Technical", UITheme.Blue,
            [("FIN", attrs.Finishing), ("PAS", attrs.Passing), ("DRI", attrs.Dribbling),
             ("1ST", attrs.FirstTouch), ("TEC", attrs.Technique)]);

        AddAttrSection(attrVbox, "Mental", UITheme.Yellow,
            [("DEC", attrs.Decisions), ("COM", attrs.Composure), ("POS", attrs.Positioning),
             ("ANT", attrs.Anticipation), ("OTB", attrs.OffTheBall)]);

        AddAttrSection(attrVbox, "Physical", UITheme.Orange,
            [("SPD", attrs.Speed), ("ACC", attrs.Acceleration), ("STA", attrs.Stamina),
             ("STR", attrs.Strength), ("AGI", attrs.Agility)]);

        if (player.PrimaryPosition == PlayerPosition.GK)
        {
            AddAttrSection(attrVbox, "Goalkeeper", UITheme.Purple,
                [("REF", attrs.Reflexes), ("HAN", attrs.Handling),
                 ("GKP", attrs.GkPositioning), ("AER", attrs.Aerial)]);
        }
    }

    private static void AddAttrSection(VBoxContainer parent, string title, Color color,
        (string label, int value)[] attrs)
    {
        parent.AddChild(UITheme.CreateLabel(title, UITheme.FontSizeCaption, color));

        foreach ((string label, int value) in attrs)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 4);
            parent.AddChild(row);

            row.AddChild(UITheme.CreateLabel(label, 11, UITheme.TextSecondary));

            var bar = UITheme.CreateProgressBar(value, 100,
                UITheme.StatColor(value), null, new Vector2(60, 8));
            bar.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            bar.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(bar);

            row.AddChild(UITheme.CreateLabel($"{value}", 11, UITheme.StatColor(value)));
        }
    }

    private void RebuildBenchPanel()
    {
        if (_benchPanel == null) return;

        foreach (Node child in _benchPanel.GetChildren())
            child.QueueFree();

        _benchPanel.AddChild(UITheme.CreateLabel("Bench",
            UITheme.FontSizeBody, UITheme.TextSecondary));

        var benchScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        _benchPanel.AddChild(benchScroll);

        _benchList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _benchList.AddThemeConstantOverride("separation", 4);
        benchScroll.AddChild(_benchList);

        RebuildBench();
    }

    private static int PositionOrder(PlayerPosition pos) => pos switch
    {
        PlayerPosition.GK => 0,
        PlayerPosition.CB => 1,
        PlayerPosition.LB => 2,
        PlayerPosition.RB => 3,
        PlayerPosition.CDM => 4,
        PlayerPosition.CM => 5,
        PlayerPosition.CAM => 6,
        PlayerPosition.LM => 7,
        PlayerPosition.RM => 8,
        PlayerPosition.LW => 9,
        PlayerPosition.RW => 10,
        PlayerPosition.CF => 11,
        PlayerPosition.ST => 12,
        _ => 99,
    };
}
