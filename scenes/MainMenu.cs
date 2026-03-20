using Godot;
using ElevenLegends.Data.Generators;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

namespace ElevenLegends.Scenes;

/// <summary>
/// Main menu screen — New Game, Continue, Settings.
/// </summary>
public partial class MainMenu : Control
{
    private SaveManager _saveManager = null!;

    public override void _Ready()
    {
        _saveManager = new SaveManager(
            System.IO.Path.Combine(OS.GetUserDataDir(), "saves"));

        BuildUI();
    }

    private void BuildUI()
    {
        // Background
        var bg = Theme.CreateBackground(Theme.Background);
        AddChild(bg);

        // Center container
        var center = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.Center,
            GrowHorizontal = GrowDirection.Both,
            GrowVertical = GrowDirection.Both,
        };
        center.AddThemeConstantOverride("separation", 20);
        AddChild(center);

        // Title
        var title = Theme.CreateLabel("⚽ Eleven Legends", Theme.FontSizeTitle,
            Theme.Green, HorizontalAlignment.Center);
        center.AddChild(title);

        var subtitle = Theme.CreateLabel("Football Manager + Gacha",
            Theme.FontSizeBody, Theme.TextSecondary, HorizontalAlignment.Center);
        center.AddChild(subtitle);

        // Spacer
        center.AddChild(new Control { CustomMinimumSize = new Vector2(0, 40) });

        // New Game button
        var newGameBtn = Theme.CreateButton("🆕  New Game", Theme.Green);
        newGameBtn.Pressed += OnNewGame;
        center.AddChild(newGameBtn);

        // Continue button (only if save exists)
        if (_saveManager.HasAutoSave())
        {
            var continueBtn = Theme.CreateButton("▶️  Continue", Theme.Blue);
            continueBtn.Pressed += OnContinue;
            center.AddChild(continueBtn);
        }

        // Center alignment
        center.SetAnchorsPreset(LayoutPreset.Center);
        foreach (var child in center.GetChildren())
        {
            if (child is Control ctrl)
                ctrl.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        }
    }

    private void OnNewGame()
    {
        SceneManager.Instance.ChangeScene("res://scenes/ClubSelection.tscn");
    }

    private void OnContinue()
    {
        try
        {
            var gameState = _saveManager.LoadGame("autosave");
            SceneManager.Instance.CurrentGameState = gameState;
            SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Failed to load save: {ex.Message}");
        }
    }
}
