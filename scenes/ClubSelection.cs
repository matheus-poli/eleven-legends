using Godot;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Club selection screen — pick your club with animated HoverCards.
/// Cards grouped by country with accent-colored headers.
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
        // ─── Background ───────────────────────────────────────────
        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        // ─── Root layout ──────────────────────────────────────────
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, margin: UITheme.PaddingLarge);
        root.AddThemeConstantOverride("separation", UITheme.Padding);
        AddChild(root);

        // ─── Header ──────────────────────────────────────────────
        var headerCard = UITheme.CreateCard(UITheme.Green);
        root.AddChild(headerCard);

        var headerVbox = new VBoxContainer();
        headerVbox.AddThemeConstantOverride("separation", 4);
        headerCard.AddChild(headerVbox);

        headerVbox.AddChild(UITheme.CreateLabel("Choose Your Club",
            UITheme.FontSizeTitle, UITheme.TextDark, HorizontalAlignment.Center));
        headerVbox.AddChild(UITheme.CreateLabel("Select a club to begin your career",
            UITheme.FontSizeSmall, UITheme.TextSecondary, HorizontalAlignment.Center));

        // ─── Scrollable club grid ─────────────────────────────────
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        root.AddChild(scroll);

        var scrollContent = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        scrollContent.AddThemeConstantOverride("separation", UITheme.PaddingLarge);
        scroll.AddChild(scrollContent);

        // ─── Club cards grouped by country ────────────────────────
        Color[] countryColors = [UITheme.Blue, UITheme.Green, UITheme.Orange, UITheme.Purple];
        int colorIdx = 0;

        var grouped = _clubs.GroupBy(c => c.Country).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            Color accent = countryColors[colorIdx % countryColors.Length];
            colorIdx++;

            // Country header
            var countryHeader = new HBoxContainer();
            countryHeader.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
            scrollContent.AddChild(countryHeader);

            var colorBar = new ColorRect
            {
                CustomMinimumSize = new Vector2(4, 0),
                Color = accent,
                SizeFlagsVertical = SizeFlags.ExpandFill,
            };
            countryHeader.AddChild(colorBar);

            countryHeader.AddChild(UITheme.CreateLabel(group.Key,
                UITheme.FontSizeHeading, accent));

            // Grid of clubs for this country
            var grid = new GridContainer
            {
                Columns = 4,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            grid.AddThemeConstantOverride("h_separation", UITheme.Padding);
            grid.AddThemeConstantOverride("v_separation", UITheme.Padding);
            scrollContent.AddChild(grid);

            foreach (Club club in group)
            {
                var card = CreateClubCard(club, accent);
                grid.AddChild(card);
            }
        }

        // ─── Back button ──────────────────────────────────────────
        var backBtn = UITheme.CreateFlatButton("Back", UITheme.Border, UITheme.TextPrimary);
        backBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        backBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/MainMenu.tscn");
        root.AddChild(backBtn);

        // ─── Entrance animations ──────────────────────────────────
        Anim.FadeIn(headerCard, delay: 0.05f);

        // Stagger the country groups
        float groupDelay = 0.15f;
        foreach (Node child in scrollContent.GetChildren())
        {
            if (child is GridContainer gridChild)
            {
                Anim.StaggerChildren(gridChild, stagger: 0.04f);
            }
            else if (child is Control ctrl)
            {
                Anim.FadeIn(ctrl, delay: groupDelay);
            }
            groupDelay += 0.1f;
        }
    }

    private HoverCard CreateClubCard(Club club, Color accent)
    {
        var card = HoverCard.Create(accent);
        card.CustomMinimumSize = new Vector2(260, 140);
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        card.AddChild(vbox);

        // ─── Top row: OVR badge + club name ───────────────────────
        var topRow = new HBoxContainer();
        topRow.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        vbox.AddChild(topRow);

        float avgOverall = club.Team.Players.Average(p =>
            p.PrimaryPosition == Data.Enums.Position.GK
                ? p.Attributes.GoalkeeperOverall
                : p.Attributes.OutfieldOverall);

        var badge = UITheme.CreateBadge($"{avgOverall:F0}",
            UITheme.RatingColor(avgOverall), UITheme.TextDark,
            UITheme.FontSizeHeading, new Vector2(48, 48));
        topRow.AddChild(badge);

        var nameVbox = new VBoxContainer();
        nameVbox.AddThemeConstantOverride("separation", 2);
        topRow.AddChild(nameVbox);

        nameVbox.AddChild(UITheme.CreateLabel(club.Name,
            UITheme.FontSizeBody, UITheme.TextDark));
        nameVbox.AddChild(UITheme.CreateLabel(club.Country,
            UITheme.FontSizeCaption, UITheme.TextSecondary));

        // ─── Stats row ────────────────────────────────────────────
        var statsRow = new HBoxContainer();
        statsRow.AddThemeConstantOverride("separation", UITheme.Padding);
        vbox.AddChild(statsRow);

        statsRow.AddChild(CreateStatChip("Rep", $"{club.Reputation}", UITheme.Yellow));
        statsRow.AddChild(CreateStatChip("Squad", $"{club.Team.Players.Count}", UITheme.Blue));
        statsRow.AddChild(CreateStatChip("Budget", FormatMoney(club.Balance), UITheme.Green));

        // ─── Manage button ────────────────────────────────────────
        var manageBtn = UITheme.CreateFlatButton("Manage", accent);
        manageBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        manageBtn.CustomMinimumSize = new Vector2(0, 36);
        manageBtn.Pressed += () => SelectClub(club);
        vbox.AddChild(manageBtn);

        return card;
    }

    private static Control CreateStatChip(string label, string value, Color color)
    {
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 0);
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        vbox.AddChild(UITheme.CreateLabel(value,
            UITheme.FontSizeBody, color, HorizontalAlignment.Center));
        vbox.AddChild(UITheme.CreateLabel(label,
            UITheme.FontSizeCaption, UITheme.TextSecondary, HorizontalAlignment.Center));

        return vbox;
    }

    private static string FormatMoney(decimal amount)
    {
        return amount switch
        {
            >= 1_000_000 => $"{amount / 1_000_000:F1}M",
            >= 1_000 => $"{amount / 1_000:F0}K",
            _ => $"{amount:F0}"
        };
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
