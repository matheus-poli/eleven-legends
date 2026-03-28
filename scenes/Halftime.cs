using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Gacha;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Halftime screen — locker room card selection with HoverCard effects.
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
        foreach (Node child in GetChildren())
            child.QueueFree();

        var bg = UITheme.CreateGradientBackground(
            UITheme.BlueDark, new Color("0F1B2D"));
        AddChild(bg);

        var root = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = UITheme.PaddingLarge,
            OffsetRight = -UITheme.PaddingLarge,
            OffsetTop = UITheme.PaddingLarge,
            OffsetBottom = -UITheme.PaddingLarge,
        };
        root.AddThemeConstantOverride("separation", UITheme.PaddingLarge);
        AddChild(root);

        // ─── Score header ─────────────────────────────────────────
        root.AddChild(UITheme.CreateLabel(
            $"Half Time",
            UITheme.FontSizeHeading, UITheme.TextLight, HorizontalAlignment.Center));

        root.AddChild(UITheme.CreateLabel(
            $"{_config.HomeTeam.Name}  {_matchState.ScoreHome} - {_matchState.ScoreAway}  {_config.AwayTeam.Name}",
            UITheme.FontSizeTitle, UITheme.TextLight, HorizontalAlignment.Center));

        // ─── Card selection prompt ────────────────────────────────
        root.AddChild(UITheme.CreateLabel("Choose a Locker Room Card",
            UITheme.FontSizeBody, new Color(1, 1, 1, 0.7f), HorizontalAlignment.Center));

        // ─── Generate cards ───────────────────────────────────────
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

        // ─── Card row ─────────────────────────────────────────────
        var cardRow = new HBoxContainer();
        cardRow.AddThemeConstantOverride("separation", UITheme.PaddingLarge);
        root.AddChild(cardRow);

        cardRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        foreach (LockerRoomCard card in cards)
        {
            HoverCard cardUI = CreateCardUI(card);
            cardRow.AddChild(cardUI);
        }

        cardRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        // ─── Continue button ──────────────────────────────────────
        root.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

        var continueBtn = UITheme.CreateButton("Start Second Half", UITheme.Green);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueBtn.CustomMinimumSize = new Vector2(280, 56);
        continueBtn.Pressed += OnContinue;
        root.AddChild(continueBtn);

        // ─── Entrance animations ──────────────────────────────────
        Anim.StaggerChildren(cardRow, stagger: 0.15f, duration: 0.5f);
    }

    private HoverCard CreateCardUI(LockerRoomCard card)
    {
        bool isSelected = _selectedCard?.Name == card.Name;

        Color cardColor = card.Effect switch
        {
            CardEffect.MoraleBoost => UITheme.Green,
            CardEffect.StaminaRecovery => UITheme.Blue,
            CardEffect.TeamBuff => UITheme.Orange,
            CardEffect.OpponentDebuff => UITheme.Purple,
            _ => UITheme.Yellow,
        };

        var hoverCard = HoverCard.Create(isSelected ? UITheme.Yellow : cardColor);
        hoverCard.CustomMinimumSize = new Vector2(200, 220);

        if (isSelected)
        {
            // Brighter bg for selected
            var style = new StyleBoxFlat
            {
                BgColor = cardColor.Lerp(UITheme.White, 0.85f),
                CornerRadiusTopLeft = UITheme.CardCornerRadius,
                CornerRadiusTopRight = UITheme.CardCornerRadius,
                CornerRadiusBottomLeft = UITheme.CardCornerRadius,
                CornerRadiusBottomRight = UITheme.CardCornerRadius,
                ContentMarginLeft = UITheme.Padding,
                ContentMarginRight = UITheme.Padding,
                ContentMarginTop = UITheme.Padding,
                ContentMarginBottom = UITheme.Padding,
                ShadowColor = new Color(cardColor, 0.4f),
                ShadowSize = 16,
                ShadowOffset = new Vector2(0, 4),
                BorderWidthTop = 4,
                BorderColor = UITheme.Yellow,
            };
            hoverCard.AddThemeStyleboxOverride("panel", style);
        }

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        hoverCard.AddChild(vbox);

        string emoji = card.Effect switch
        {
            CardEffect.MoraleBoost => "💪",
            CardEffect.StaminaRecovery => "⚡",
            CardEffect.TeamBuff => "🔥",
            CardEffect.OpponentDebuff => "❄️",
            _ => "🎴",
        };

        // Emoji icon
        vbox.AddChild(UITheme.CreateLabel(emoji, UITheme.FontSizeDisplay,
            cardColor, HorizontalAlignment.Center));

        // Card name
        vbox.AddChild(UITheme.CreateLabel(card.Name,
            UITheme.FontSizeBody, UITheme.TextDark, HorizontalAlignment.Center));

        // Description
        vbox.AddChild(UITheme.CreateLabel(card.Description,
            UITheme.FontSizeSmall, UITheme.TextSecondary, HorizontalAlignment.Center));

        // Magnitude badge
        var magBadge = UITheme.CreateBadge($"+{card.Magnitude}",
            cardColor, UITheme.TextLight, UITheme.FontSizeHeading, new Vector2(60, 40));
        magBadge.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        vbox.AddChild(magBadge);

        // Click to select
        var btn = new Button
        {
            Flat = true,
            AnchorsPreset = (int)LayoutPreset.FullRect,
            Modulate = new Color(1, 1, 1, 0),
        };
        LockerRoomCard captured = card;
        btn.Pressed += () =>
        {
            _selectedCard = captured;
            BuildUI();
        };
        hoverCard.AddChild(btn);

        return hoverCard;
    }

    private void OnContinue()
    {
        bool isHome = _ctx.PlayerFixture!.HomeClubId == _playerClub.Id;

        if (_selectedCard != null)
        {
            HalftimeProcessor.ApplyCard(_matchState, _config, _selectedCard, isHome);
        }

        MatchResult result = MatchSimulator.SimulateSecondHalf(_matchState, _config,
            new SeededRng(_config.Seed + 200));

        PostMatchScreen.PendingResult = result;
        PostMatchScreen.PendingContext = _ctx;
        SceneManager.Instance.ChangeScene("res://scenes/PostMatch.tscn");
    }
}
