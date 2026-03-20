using Godot;
using ElevenLegends.Data.Models;
using ElevenLegends.Manager;
using ElevenLegends.Persistence;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

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

        bool isGameOver = CareerManager.IsGameOver(_gameState.Manager);
        bool isChampion = CareerManager.IsVictory(_gameState.Manager);

        if (isChampion)
        {
            root.AddChild(Theme.CreateLabel("🏆 CHAMPION!",
                Theme.FontSizeTitle, Theme.Yellow, HorizontalAlignment.Center));
            root.AddChild(Theme.CreateLabel($"{_playerClub.Name} wins the Mundial!",
                Theme.FontSizeHeading, Theme.Green, HorizontalAlignment.Center));
        }
        else if (isGameOver)
        {
            root.AddChild(Theme.CreateLabel("💼 Game Over",
                Theme.FontSizeTitle, Theme.Pink, HorizontalAlignment.Center));
            root.AddChild(Theme.CreateLabel("You have been sacked.",
                Theme.FontSizeHeading, Theme.TextSecondary, HorizontalAlignment.Center));
        }
        else
        {
            root.AddChild(Theme.CreateLabel("📊 Season Complete",
                Theme.FontSizeTitle, Theme.Blue, HorizontalAlignment.Center));
        }

        // Season stats card
        var statsCard = Theme.CreateCard();
        root.AddChild(statsCard);

        var statsVbox = new VBoxContainer();
        statsVbox.AddThemeConstantOverride("separation", 8);
        statsCard.AddChild(statsVbox);

        statsVbox.AddChild(Theme.CreateLabel("📋 Season Summary",
            Theme.FontSizeHeading, Theme.Blue, HorizontalAlignment.Center));

        statsVbox.AddChild(Theme.CreateLabel(
            $"Club: {_playerClub.Name}",
            Theme.FontSizeBody, Theme.TextPrimary));
        statsVbox.AddChild(Theme.CreateLabel(
            $"Manager Reputation: {_gameState.Manager.Reputation}",
            Theme.FontSizeBody, Theme.TextSecondary));
        statsVbox.AddChild(Theme.CreateLabel(
            $"Club Balance: {_playerClub.Balance:C0}",
            Theme.FontSizeBody, Theme.TextSecondary));
        statsVbox.AddChild(Theme.CreateLabel(
            $"Squad Size: {_playerClub.Team.Players.Count}",
            Theme.FontSizeBody, Theme.TextSecondary));
        statsVbox.AddChild(Theme.CreateLabel(
            $"Manager Status: {_gameState.Manager.Status}",
            Theme.FontSizeBody, Theme.TextSecondary));

        // Transfer history
        if (_gameState.TransferHistory.Count > 0)
        {
            var transferCard = Theme.CreateCard();
            root.AddChild(transferCard);

            var transferVbox = new VBoxContainer();
            transferVbox.AddThemeConstantOverride("separation", 4);
            transferCard.AddChild(transferVbox);

            transferVbox.AddChild(Theme.CreateLabel("🔄 Transfer Activity",
                Theme.FontSizeBody, Theme.Orange));
            foreach (var t in _gameState.TransferHistory.TakeLast(8))
            {
                transferVbox.AddChild(Theme.CreateLabel(
                    $"  {t.Type}: {t.PlayerName} — {t.Fee:C0}",
                    Theme.FontSizeSmall, Theme.TextSecondary));
            }
        }

        // Actions
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", Theme.Padding);
        root.AddChild(actions);

        var menuBtn = Theme.CreateButton("🏠 Main Menu", Theme.Blue);
        menuBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        menuBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/MainMenu.tscn");
        actions.AddChild(menuBtn);
    }
}
