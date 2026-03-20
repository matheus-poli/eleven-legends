using Godot;
using ElevenLegends.Data.Models;
using ElevenLegends.Manager;
using ElevenLegends.Persistence;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Season end screen — final standings, game over, or next season.
/// </summary>
public partial class SeasonEndScreen : Control
{
    private GameState _gameState = null!;
    private Club _playerClub = null!;

    public override void _Ready()
    {
        _gameState = SceneManager.Instance.CurrentGameState!;
        _playerClub = _gameState.PlayerClub;
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

        bool isGameOver = CareerManager.IsGameOver(_gameState.Manager);
        bool isChampion = CareerManager.IsVictory(_gameState.Manager);

        if (isChampion)
        {
            root.AddChild(UITheme.CreateLabel("🏆 CHAMPION!",
                UITheme.FontSizeTitle, UITheme.Yellow, HorizontalAlignment.Center));
            root.AddChild(UITheme.CreateLabel($"{_playerClub.Name} wins the Mundial!",
                UITheme.FontSizeHeading, UITheme.Green, HorizontalAlignment.Center));
        }
        else if (isGameOver)
        {
            root.AddChild(UITheme.CreateLabel("💼 Game Over",
                UITheme.FontSizeTitle, UITheme.Pink, HorizontalAlignment.Center));
            root.AddChild(UITheme.CreateLabel("You have been sacked.",
                UITheme.FontSizeHeading, UITheme.TextSecondary, HorizontalAlignment.Center));
        }
        else
        {
            root.AddChild(UITheme.CreateLabel("📊 Season Complete",
                UITheme.FontSizeTitle, UITheme.Blue, HorizontalAlignment.Center));
        }

        // Season stats card
        var statsCard = UITheme.CreateCard();
        root.AddChild(statsCard);

        var statsVbox = new VBoxContainer();
        statsVbox.AddThemeConstantOverride("separation", 8);
        statsCard.AddChild(statsVbox);

        statsVbox.AddChild(UITheme.CreateLabel("📋 Season Summary",
            UITheme.FontSizeHeading, UITheme.Blue, HorizontalAlignment.Center));

        statsVbox.AddChild(UITheme.CreateLabel(
            $"Club: {_playerClub.Name}",
            UITheme.FontSizeBody, UITheme.TextPrimary));
        statsVbox.AddChild(UITheme.CreateLabel(
            $"Manager Reputation: {_gameState.Manager.Reputation}",
            UITheme.FontSizeBody, UITheme.TextSecondary));
        statsVbox.AddChild(UITheme.CreateLabel(
            $"Club Balance: {_playerClub.Balance:C0}",
            UITheme.FontSizeBody, UITheme.TextSecondary));
        statsVbox.AddChild(UITheme.CreateLabel(
            $"Squad Size: {_playerClub.Team.Players.Count}",
            UITheme.FontSizeBody, UITheme.TextSecondary));
        statsVbox.AddChild(UITheme.CreateLabel(
            $"Manager Status: {_gameState.Manager.Status}",
            UITheme.FontSizeBody, UITheme.TextSecondary));

        // Transfer history
        if (_gameState.TransferHistory.Count > 0)
        {
            var transferCard = UITheme.CreateCard();
            root.AddChild(transferCard);

            var transferVbox = new VBoxContainer();
            transferVbox.AddThemeConstantOverride("separation", 4);
            transferCard.AddChild(transferVbox);

            transferVbox.AddChild(UITheme.CreateLabel("🔄 Transfer Activity",
                UITheme.FontSizeBody, UITheme.Orange));
            foreach (var t in _gameState.TransferHistory.TakeLast(8))
            {
                transferVbox.AddChild(UITheme.CreateLabel(
                    $"  {t.Type}: {t.PlayerName} — {t.Fee:C0}",
                    UITheme.FontSizeSmall, UITheme.TextSecondary));
            }
        }

        // Actions
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", UITheme.Padding);
        root.AddChild(actions);

        var menuBtn = UITheme.CreateButton("🏠 Main Menu", UITheme.Blue);
        menuBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        menuBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/MainMenu.tscn");
        actions.AddChild(menuBtn);
    }
}
