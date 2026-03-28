using Godot;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Training day scene — choose a training type, see per-player events.
/// Uma Musume / Inazuma Eleven inspired.
/// </summary>
public partial class Training : Control
{
    private GameState _gameState = null!;
    private Club _playerClub = null!;
    private IReadOnlyList<TrainingChoice> _choices = [];
    private TrainingResult? _result;

    public override void _Ready()
    {
        _gameState = SceneManager.Instance.CurrentGameState!;
        _playerClub = _gameState.PlayerClub;

        int seed = _gameState.CurrentDayIndex * 1000 + _playerClub.Id;
        var rng = new SeededRng(seed);
        _choices = TrainingProcessor.GenerateChoices(rng);

        BuildChoiceUI();
    }

    private void BuildChoiceUI()
    {
        foreach (Node child in GetChildren())
            child.QueueFree();

        var bg = UITheme.CreateGradientBackground(UITheme.Blue, UITheme.BlueDark);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, margin: UITheme.PaddingLarge);
        root.AddThemeConstantOverride("separation", UITheme.Padding);
        AddChild(root);

        // Header
        var header = UITheme.CreateIconLabel("dumbbell", $"Training Day — Day {_gameState.CurrentDay.Day}",
            UITheme.FontSizeTitle, UITheme.TextLight, new Vector2(36, 36));
        header.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        root.AddChild(header);

        root.AddChild(UITheme.CreateLabel("Choose your training focus:",
            UITheme.FontSizeBody, new Color(1, 1, 1, 0.7f), HorizontalAlignment.Center));

        root.AddChild(new Control { CustomMinimumSize = new Vector2(0, 16) });

        // 3 choice cards
        var cardRow = new HBoxContainer();
        cardRow.AddThemeConstantOverride("separation", UITheme.Padding);
        cardRow.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(cardRow);

        foreach (TrainingChoice choice in _choices)
        {
            var card = CreateChoiceCard(choice);
            cardRow.AddChild(card);
        }

        // Skip button
        var skipBtn = UITheme.CreateFlatButton("Skip (auto-train)", UITheme.Border, new Color(1, 1, 1, 0.5f));
        skipBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        skipBtn.Pressed += () =>
        {
            _gameState.AdvanceDay();
            AutoSave();
            SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        };
        root.AddChild(skipBtn);

        Anim.StaggerChildren(cardRow, stagger: 0.1f);
    }

    private HoverCard CreateChoiceCard(TrainingChoice choice)
    {
        Color accent = choice.Type switch
        {
            Data.Enums.TrainingType.IntenseDrills => UITheme.Red,
            Data.Enums.TrainingType.TacticalSession => UITheme.Green,
            Data.Enums.TrainingType.LightTraining => UITheme.Blue,
            Data.Enums.TrainingType.RestDay => UITheme.Yellow,
            Data.Enums.TrainingType.YouthFocus => UITheme.Purple,
            _ => UITheme.Blue,
        };

        string iconName = choice.Type switch
        {
            Data.Enums.TrainingType.IntenseDrills => "dumbbell",
            Data.Enums.TrainingType.TacticalSession => "scale",
            Data.Enums.TrainingType.LightTraining => "football",
            Data.Enums.TrainingType.RestDay => "moon",
            Data.Enums.TrainingType.YouthFocus => "sparkle",
            _ => "dumbbell",
        };

        var card = HoverCard.Create(accent);
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        card.SizeFlagsVertical = SizeFlags.ExpandFill;

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        card.AddChild(vbox);

        // Icon
        var icon = UITheme.CreateIcon(iconName, new Vector2(48, 48), accent);
        icon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        vbox.AddChild(icon);

        // Name
        vbox.AddChild(UITheme.CreateLabel(choice.Name,
            UITheme.FontSizeHeading, accent, HorizontalAlignment.Center));

        // Description
        var desc = UITheme.CreateLabel(choice.Description,
            UITheme.FontSizeSmall, UITheme.TextSecondary, HorizontalAlignment.Center);
        desc.AutowrapMode = TextServer.AutowrapMode.Word;
        vbox.AddChild(desc);

        vbox.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

        // Select button
        var selectBtn = UITheme.CreateButton("Choose", accent);
        selectBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        TrainingChoice captured = choice;
        selectBtn.Pressed += () => OnChoiceSelected(captured);
        vbox.AddChild(selectBtn);

        return card;
    }

    private void OnChoiceSelected(TrainingChoice choice)
    {
        int seed = _gameState.CurrentDayIndex * 1000 + _playerClub.Id + 500;
        var rng = new SeededRng(seed);

        _result = TrainingProcessor.ProcessTraining(choice, _playerClub, rng);

        // Advance the day (applies AI training for other clubs too)
        _gameState.AdvanceDay();
        AutoSave();

        BuildResultUI();
    }

    private void BuildResultUI()
    {
        foreach (Node child in GetChildren())
            child.QueueFree();

        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, margin: UITheme.PaddingLarge);
        root.AddThemeConstantOverride("separation", UITheme.Padding);
        AddChild(root);

        // Header
        Color accent = _result!.Choice.Type switch
        {
            Data.Enums.TrainingType.IntenseDrills => UITheme.Red,
            Data.Enums.TrainingType.TacticalSession => UITheme.Green,
            Data.Enums.TrainingType.RestDay => UITheme.Yellow,
            Data.Enums.TrainingType.YouthFocus => UITheme.Purple,
            _ => UITheme.Blue,
        };

        root.AddChild(UITheme.CreateLabel($"Training: {_result.Choice.Name}",
            UITheme.FontSizeTitle, accent, HorizontalAlignment.Center));

        // Event feed
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        root.AddChild(scroll);

        var eventList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        eventList.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(eventList);

        foreach (TrainingPlayerEvent evt in _result.Events)
        {
            var card = UITheme.CreateCard(evt.IsPositive ? UITheme.Green : UITheme.Red);
            card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            eventList.AddChild(card);

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
            card.AddChild(row);

            string iconName = evt.IsPositive ? "arrow-up" : "arrow-down";
            Color iconColor = evt.IsPositive ? UITheme.Green : UITheme.Red;
            row.AddChild(UITheme.CreateIcon(iconName, new Vector2(16, 16), iconColor));

            var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            info.AddThemeConstantOverride("separation", 1);
            info.AddChild(UITheme.CreateLabel(evt.Description,
                UITheme.FontSizeSmall, UITheme.TextDark));

            var deltaText = "";
            if (evt.MoraleDelta != 0)
            {
                string sign = evt.MoraleDelta > 0 ? "+" : "";
                deltaText += $"Morale {sign}{evt.MoraleDelta}  ";
            }
            if (evt.ChemistryDelta != 0)
            {
                string sign = evt.ChemistryDelta > 0 ? "+" : "";
                deltaText += $"Chemistry {sign}{evt.ChemistryDelta}";
            }

            if (deltaText.Length > 0)
            {
                info.AddChild(UITheme.CreateLabel(deltaText.Trim(),
                    UITheme.FontSizeCaption, evt.IsPositive ? UITheme.Green : UITheme.Red));
            }

            row.AddChild(info);
        }

        if (_result.Events.Count == 0)
        {
            eventList.AddChild(UITheme.CreateLabel("A quiet training day. Nothing notable happened.",
                UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center));
        }

        // Continue button
        var continueBtn = UITheme.CreateButton("Continue", UITheme.Green);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        continueBtn.CustomMinimumSize = new Vector2(0, 48);
        continueBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        root.AddChild(continueBtn);

        Anim.StaggerChildren(eventList, stagger: 0.05f, useScale: false);
    }

    private void AutoSave()
    {
        try
        {
            var sm = new SaveManager(System.IO.Path.Combine(OS.GetUserDataDir(), "saves"));
            sm.AutoSave(_gameState);
        }
        catch { /* silent */ }
    }
}
