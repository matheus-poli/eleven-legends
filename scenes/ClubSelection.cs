using Godot;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Club selection screen — pick your club to manage.
/// </summary>
public partial class ClubSelection : Control
{
    private List<Club> _clubs = [];
    private readonly int _seed = (int)Time.GetTicksMsec();

    public override void _Ready()
    {
        _clubs = TeamGenerator.Generate(_seed);
        BuildUI();
    }

    private void BuildUI()
    {
        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        var root = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = UITheme.PaddingLarge,
            OffsetRight = -UITheme.PaddingLarge,
            OffsetTop = UITheme.PaddingLarge,
            OffsetBottom = -UITheme.PaddingLarge,
        };
        root.AddThemeConstantOverride("separation", UITheme.Padding);
        AddChild(root);

        // Header
        var header = UITheme.CreateLabel("Choose Your Club", UITheme.FontSizeTitle,
            UITheme.TextPrimary, HorizontalAlignment.Center);
        root.AddChild(header);

        var subtitle = UITheme.CreateLabel("Select a club to manage this season",
            UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center);
        root.AddChild(subtitle);

        // Scroll container for club cards
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        root.AddChild(scroll);

        // Grid of club cards
        var grid = new GridContainer
        {
            Columns = 4,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        grid.AddThemeConstantOverride("h_separation", UITheme.Padding);
        grid.AddThemeConstantOverride("v_separation", UITheme.Padding);
        scroll.AddChild(grid);

        // Group by country
        var grouped = _clubs.GroupBy(c => c.Country).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            // Country header spanning full width
            var countryLabel = UITheme.CreateLabel($"🌍 {group.Key}",
                UITheme.FontSizeHeading, UITheme.Blue);
            grid.AddChild(countryLabel);

            // Spacers for remaining columns
            for (int i = 1; i < 4; i++)
                grid.AddChild(new Control());

            foreach (var club in group)
            {
                var card = CreateClubCard(club);
                grid.AddChild(card);
            }
        }

        // Back button
        var backBtn = UITheme.CreateButton("← Back", UITheme.TextSecondary);
        backBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/MainMenu.tscn");
        root.AddChild(backBtn);
    }

    private PanelContainer CreateClubCard(Club club)
    {
        var card = UITheme.CreateCard();
        card.CustomMinimumSize = new Vector2(250, 120);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        card.AddChild(vbox);

        // Club name
        var name = UITheme.CreateLabel(club.Name, UITheme.FontSizeBody, UITheme.TextPrimary);
        vbox.AddChild(name);

        // Stats
        float avgOverall = club.Team.Players.Average(p =>
            p.PrimaryPosition == Data.Enums.Position.GK
                ? p.Attributes.GoalkeeperOverall
                : p.Attributes.OutfieldOverall);

        var stats = UITheme.CreateLabel(
            $"💰 {club.Balance:C0}  ⭐ {club.Reputation}  📊 {avgOverall:F0}",
            UITheme.FontSizeSmall, UITheme.TextSecondary);
        vbox.AddChild(stats);

        var playerCount = UITheme.CreateLabel(
            $"👥 {club.Team.Players.Count} players",
            UITheme.FontSizeCaption, UITheme.TextSecondary);
        vbox.AddChild(playerCount);

        // Click handler
        var button = new Button
        {
            Text = "Manage →",
            CustomMinimumSize = new Vector2(0, 36),
        };
        var btnStyle = new StyleBoxFlat
        {
            BgColor = UITheme.Green,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
        };
        button.AddThemeStyleboxOverride("normal", btnStyle);
        button.AddThemeColorOverride("font_color", UITheme.TextLight);
        button.AddThemeFontSizeOverride("font_size", UITheme.FontSizeSmall);
        button.Pressed += () => SelectClub(club);
        vbox.AddChild(button);

        return card;
    }

    private void SelectClub(Club club)
    {
        var manager = new ManagerState
        {
            Name = "You",
            ClubId = club.Id,
            Reputation = 50
        };

        var gameState = new GameState(_clubs, manager, _seed);
        SceneManager.Instance.CurrentGameState = gameState;
        SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
    }
}
