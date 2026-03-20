using Godot;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

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
        var bg = Theme.CreateBackground(Theme.Background);
        AddChild(bg);

        var root = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = Theme.PaddingLarge,
            OffsetRight = -Theme.PaddingLarge,
            OffsetTop = Theme.PaddingLarge,
            OffsetBottom = -Theme.PaddingLarge,
        };
        root.AddThemeConstantOverride("separation", Theme.Padding);
        AddChild(root);

        // Header
        var header = Theme.CreateLabel("Choose Your Club", Theme.FontSizeTitle,
            Theme.TextPrimary, HorizontalAlignment.Center);
        root.AddChild(header);

        var subtitle = Theme.CreateLabel("Select a club to manage this season",
            Theme.FontSizeBody, Theme.TextSecondary, HorizontalAlignment.Center);
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
        grid.AddThemeConstantOverride("h_separation", Theme.Padding);
        grid.AddThemeConstantOverride("v_separation", Theme.Padding);
        scroll.AddChild(grid);

        // Group by country
        var grouped = _clubs.GroupBy(c => c.Country).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            // Country header spanning full width
            var countryLabel = Theme.CreateLabel($"🌍 {group.Key}",
                Theme.FontSizeHeading, Theme.Blue);
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
        var backBtn = Theme.CreateButton("← Back", Theme.TextSecondary);
        backBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/MainMenu.tscn");
        root.AddChild(backBtn);
    }

    private PanelContainer CreateClubCard(Club club)
    {
        var card = Theme.CreateCard();
        card.CustomMinimumSize = new Vector2(250, 120);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        card.AddChild(vbox);

        // Club name
        var name = Theme.CreateLabel(club.Name, Theme.FontSizeBody, Theme.TextPrimary);
        vbox.AddChild(name);

        // Stats
        float avgOverall = club.Team.Players.Average(p =>
            p.PrimaryPosition == Data.Enums.Position.GK
                ? p.Attributes.GoalkeeperOverall
                : p.Attributes.OutfieldOverall);

        var stats = Theme.CreateLabel(
            $"💰 {club.Balance:C0}  ⭐ {club.Reputation}  📊 {avgOverall:F0}",
            Theme.FontSizeSmall, Theme.TextSecondary);
        vbox.AddChild(stats);

        var playerCount = Theme.CreateLabel(
            $"👥 {club.Team.Players.Count} players",
            Theme.FontSizeCaption, Theme.TextSecondary);
        vbox.AddChild(playerCount);

        // Click handler
        var button = new Button
        {
            Text = "Manage →",
            CustomMinimumSize = new Vector2(0, 36),
        };
        var btnStyle = new StyleBoxFlat
        {
            BgColor = Theme.Green,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
        };
        button.AddThemeStyleboxOverride("normal", btnStyle);
        button.AddThemeColorOverride("font_color", Theme.TextLight);
        button.AddThemeFontSizeOverride("font_size", Theme.FontSizeSmall);
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
