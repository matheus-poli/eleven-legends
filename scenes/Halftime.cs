using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Gacha;
using ElevenLegends.Simulation;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

namespace ElevenLegends.Scenes;

/// <summary>
/// Halftime screen — locker room card selection and substitutions.
/// </summary>
public partial class HalftimeScreen : Control
{
    public static MatchState? PendingMatchState;
    public static MatchConfig? PendingConfig;
    public static MatchDayContext? PendingContext;

    private MatchState _matchState = null!;
    private MatchConfig _config = null!;
    private MatchDayContext _ctx = null!;
    private GameState _gameState = null!;
    private Club _playerClub = null!;
    private LockerRoomCard? _selectedCard;

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
    }

    private void BuildUI()
    {
        foreach (var child in GetChildren())
            child.QueueFree();

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

        // Score
        var score = Theme.CreateLabel(
            $"⏱️ Half Time: {_config.HomeTeam.Name} {_matchState.ScoreHome} - {_matchState.ScoreAway} {_config.AwayTeam.Name}",
            Theme.FontSizeHeading, Theme.TextPrimary, HorizontalAlignment.Center);
        root.AddChild(score);

        // Locker room cards section
        root.AddChild(Theme.CreateLabel("🎴 Choose a Locker Room Card",
            Theme.FontSizeHeading, Theme.Blue, HorizontalAlignment.Center));

        bool isHome = _ctx.PlayerFixture!.HomeClubId == _playerClub.Id;
        int scoreDiff = isHome
            ? _matchState.ScoreHome - _matchState.ScoreAway
            : _matchState.ScoreAway - _matchState.ScoreHome;
        var activePlayers = isHome ? _matchState.HomeActivePlayerIds : _matchState.AwayActivePlayerIds;
        float avgStamina = activePlayers.Count > 0
            ? activePlayers.Average(id => _matchState.PlayerStamina.GetValueOrDefault(id, 70f))
            : 70f;
        float avgMorale = _playerClub.Team.Players
            .Where(p => activePlayers.Contains(p.Id))
            .Select(p => (float)p.Morale)
            .DefaultIfEmpty(50f)
            .Average();

        var cards = LockerRoomCardGenerator.Generate(
            new SeededRng(_config.Seed + 100), scoreDiff, avgStamina, avgMorale);
        var cardsHbox = new HBoxContainer();
        cardsHbox.AddThemeConstantOverride("separation", Theme.Padding);
        root.AddChild(cardsHbox);

        foreach (var card in cards)
        {
            var cardPanel = CreateCardUI(card);
            cardsHbox.AddChild(cardPanel);
        }

        // Continue button
        var continueBtn = Theme.CreateButton("▶️ Start Second Half", Theme.Green);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueBtn.Pressed += OnContinue;
        root.AddChild(continueBtn);
    }

    private PanelContainer CreateCardUI(LockerRoomCard card)
    {
        var panel = Theme.CreateCard();
        panel.CustomMinimumSize = new Vector2(220, 160);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        panel.AddChild(vbox);

        string emoji = card.Effect switch
        {
            CardEffect.MoraleBoost => "💪",
            CardEffect.StaminaRecovery => "⚡",
            CardEffect.TeamBuff => "🔥",
            CardEffect.OpponentDebuff => "❄️",
            _ => "🎴"
        };

        vbox.AddChild(Theme.CreateLabel($"{emoji} {card.Name}",
            Theme.FontSizeBody, Theme.TextPrimary, HorizontalAlignment.Center));
        vbox.AddChild(Theme.CreateLabel(card.Description,
            Theme.FontSizeSmall, Theme.TextSecondary, HorizontalAlignment.Center));
        vbox.AddChild(Theme.CreateLabel($"+{card.Magnitude}",
            Theme.FontSizeHeading, Theme.Green, HorizontalAlignment.Center));

        var selectBtn = Theme.CreateButton("Select",
            _selectedCard?.Name == card.Name ? Theme.Green : Theme.Blue);
        selectBtn.CustomMinimumSize = new Vector2(0, 40);
        var capturedCard = card;
        selectBtn.Pressed += () =>
        {
            _selectedCard = capturedCard;
            // Rebuild to show selection
            foreach (var child in GetChildren())
                child.QueueFree();
            BuildUI();
        };
        vbox.AddChild(selectBtn);

        return panel;
    }

    private void OnContinue()
    {
        bool isHome = _ctx.PlayerFixture!.HomeClubId == _playerClub.Id;

        // Apply card and simulate second half
        if (_selectedCard != null)
        {
            HalftimeProcessor.ApplyCard(_matchState, _config, _selectedCard, isHome);
        }

        var result = MatchSimulator.SimulateSecondHalf(_matchState, _config,
            new SeededRng(_config.Seed + 200));

        // Go to post-match
        PostMatchScreen.PendingResult = result;
        PostMatchScreen.PendingContext = _ctx;
        SceneManager.Instance.ChangeScene("res://scenes/PostMatch.tscn");
    }
}
