using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes.Components;

/// <summary>
/// Read-only dual-team pitch view for match simulation.
/// Shows both teams' formations with live player rating badges (SofaScore-style).
/// Home team at bottom, away team at top (mirrored Y).
/// </summary>
public partial class MatchPitchView : Control
{
    private static readonly Color PitchGreen = new("2E7D32");
    private static readonly Color PitchGreenLight = new("388E3C");
    private static readonly Color LineWhite = new("FFFFFF30");
    private static readonly Color HomeTint = new("D0E8FF");
    private static readonly Color AwayTint = new("FFD0D0");

    private MatchConfig _config = null!;
    private Formation _homeFormation = Formation.F442;
    private Formation _awayFormation = Formation.F442;
    private Dictionary<int, Player> _allPlayers = new();
    private List<int> _homePlayerIds = [];
    private List<int> _awayPlayerIds = [];
    private Dictionary<int, float> _ratings = new();

    // Card nodes for efficient updates
    private readonly Dictionary<int, Label> _ratingLabels = new();
    private readonly Dictionary<int, PanelContainer> _ratingBadges = new();

    public void Setup(MatchConfig config, Formation? homeFormation = null, Formation? awayFormation = null)
    {
        _config = config;
        _homeFormation = homeFormation ?? config.HomeTactics?.Formation ?? Formation.F442;
        _awayFormation = awayFormation ?? config.AwayTactics?.Formation ?? Formation.F442;

        _allPlayers = config.HomeTeam.Players
            .Concat(config.AwayTeam.Players)
            .ToDictionary(p => p.Id);

        _homePlayerIds = (config.HomeTactics?.StartingPlayerIds ?? config.HomeTeam.StartingLineup).ToList();
        _awayPlayerIds = (config.AwayTactics?.StartingPlayerIds ?? config.AwayTeam.StartingLineup).ToList();

        RebuildCards();
    }

    /// <summary>
    /// Updates the active player lists (for substitutions during match).
    /// </summary>
    public void UpdateActivePlayers(List<int> homeIds, List<int> awayIds)
    {
        _homePlayerIds = homeIds;
        _awayPlayerIds = awayIds;
        RebuildCards();
    }

    /// <summary>
    /// Updates player rating badges in real-time. Call each tick.
    /// </summary>
    public void UpdateRatings(Dictionary<int, float> ratings)
    {
        _ratings = ratings;

        foreach (KeyValuePair<int, float> kvp in ratings)
        {
            if (_ratingLabels.TryGetValue(kvp.Key, out Label? label))
            {
                label.Text = $"{kvp.Value:F1}";
            }
            if (_ratingBadges.TryGetValue(kvp.Key, out PanelContainer? badge))
            {
                Color ratingColor = RatingBadgeColor(kvp.Value);
                var style = new StyleBoxFlat
                {
                    BgColor = ratingColor,
                    CornerRadiusTopLeft = 6,
                    CornerRadiusTopRight = 6,
                    CornerRadiusBottomLeft = 6,
                    CornerRadiusBottomRight = 6,
                    ContentMarginLeft = 3,
                    ContentMarginRight = 3,
                    ContentMarginTop = 1,
                    ContentMarginBottom = 1,
                };
                badge.AddThemeStyleboxOverride("panel", style);
            }
        }
    }

    public override void _Ready()
    {
        ClipContents = true;
    }

    public override void _Draw()
    {
        DrawPitch();
    }

    private void DrawPitch()
    {
        Vector2 size = Size;
        float w = size.X;
        float h = size.Y;
        float lw = 1.5f;

        // Main pitch
        DrawRect(new Rect2(0, 0, w, h), PitchGreen);

        // Stripes
        int stripes = 12;
        float stripeH = h / stripes;
        for (int i = 0; i < stripes; i += 2)
            DrawRect(new Rect2(0, i * stripeH, w, stripeH), PitchGreenLight);

        // Border
        DrawRect(new Rect2(0, 0, w, h), LineWhite, false, lw);

        // Center line + circle
        DrawLine(new Vector2(0, h / 2), new Vector2(w, h / 2), LineWhite, lw);
        DrawArc(new Vector2(w / 2, h / 2), h * 0.08f, 0, Mathf.Tau, 48, LineWhite, lw);
        DrawCircle(new Vector2(w / 2, h / 2), 2, LineWhite);

        // Penalty boxes
        float boxW = w * 0.40f;
        float boxH = h * 0.12f;
        DrawRect(new Rect2((w - boxW) / 2, 0, boxW, boxH), LineWhite, false, lw);
        DrawRect(new Rect2((w - boxW) / 2, h - boxH, boxW, boxH), LineWhite, false, lw);

        // Small boxes
        float sboxW = w * 0.18f;
        float sboxH = h * 0.05f;
        DrawRect(new Rect2((w - sboxW) / 2, 0, sboxW, sboxH), LineWhite, false, lw);
        DrawRect(new Rect2((w - sboxW) / 2, h - sboxH, sboxW, sboxH), LineWhite, false, lw);
    }

    private void RebuildCards()
    {
        // Clear old cards
        foreach (Node child in GetChildren())
            child.QueueFree();
        _ratingLabels.Clear();
        _ratingBadges.Clear();

        Vector2 pitchSize = Size;
        if (pitchSize.X < 10 || pitchSize.Y < 10) return;

        Vector2 cardSize = new(86, 80);

        // Home team (bottom half — Y coordinates as-is from FormationLayout)
        PlaceTeamCards(_homePlayerIds, _homeFormation, _config.HomeTeam,
            pitchSize, cardSize, mirrorY: false, HomeTint);

        // Away team (top half — mirrored Y)
        PlaceTeamCards(_awayPlayerIds, _awayFormation, _config.AwayTeam,
            pitchSize, cardSize, mirrorY: true, AwayTint);

        QueueRedraw();
    }

    private void PlaceTeamCards(List<int> playerIds, Formation formation, Team team,
        Vector2 pitchSize, Vector2 cardSize, bool mirrorY, Color tint)
    {
        IReadOnlyList<(float X, float Y)> positions = FormationLayout.GetPositions(formation);

        for (int i = 0; i < System.Math.Min(playerIds.Count, positions.Count); i++)
        {
            int playerId = playerIds[i];
            if (playerId <= 0 || !_allPlayers.ContainsKey(playerId)) continue;

            Player player = _allPlayers[playerId];
            (float nx, float ny) = positions[i];

            // Mirror Y for away team
            if (mirrorY)
                ny = 1.0f - ny;

            Vector2 pos = new Vector2(nx, ny) * pitchSize - cardSize / 2;

            Control card = CreateMatchCard(player, tint);
            card.Position = pos;
            card.Size = cardSize;
            AddChild(card);
        }
    }

    private Control CreateMatchCard(Player player, Color tint)
    {
        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(86, 80);

        var style = new StyleBoxFlat
        {
            BgColor = tint,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);
        panel.AddChild(vbox);

        // Rating badge
        float rating = _ratings.GetValueOrDefault(player.Id, 6.0f);
        Color ratingColor = RatingBadgeColor(rating);

        var ratingBadge = new PanelContainer();
        var badgeStyle = new StyleBoxFlat
        {
            BgColor = ratingColor,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
        };
        ratingBadge.AddThemeStyleboxOverride("panel", badgeStyle);
        ratingBadge.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;

        var ratingLabel = new Label
        {
            Text = $"{rating:F1}",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        ratingLabel.AddThemeFontSizeOverride("font_size", UITheme.FontSizeSmall);
        ratingLabel.AddThemeColorOverride("font_color", UITheme.White);
        ratingBadge.AddChild(ratingLabel);
        vbox.AddChild(ratingBadge);

        _ratingLabels[player.Id] = ratingLabel;
        _ratingBadges[player.Id] = ratingBadge;

        // Player name
        string displayName = player.Name.Length > 10
            ? player.Name[..9] + "."
            : player.Name;
        var nameLabel = UITheme.CreateLabel(displayName,
            UITheme.FontSizeCaption, UITheme.TextDark, HorizontalAlignment.Center);
        nameLabel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        vbox.AddChild(nameLabel);

        // Position
        var posLabel = UITheme.CreateLabel($"{player.PrimaryPosition}",
            UITheme.FontSizeCaption - 1, UITheme.TextSecondary, HorizontalAlignment.Center);
        posLabel.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        vbox.AddChild(posLabel);

        return panel;
    }

    private static Color RatingBadgeColor(float rating)
    {
        if (rating >= 8.0f) return new Color("1DB954"); // Excellent — green
        if (rating >= 7.0f) return new Color("58CC02"); // Good — light green
        if (rating >= 6.5f) return new Color("FFC800"); // Average — yellow
        if (rating >= 6.0f) return new Color("FF9600"); // Below avg — orange
        return new Color("FF4B4B"); // Poor — red
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && _config != null)
            RebuildCards();
    }
}
