using Godot;
using ElevenLegends.Data.Models;
using ElevenLegends.Manager;
using ElevenLegends.Persistence;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Season end screen — trophy celebration, game over, or season summary.
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
        bool isGameOver = CareerManager.IsGameOver(_gameState.Manager);
        bool isChampion = CareerManager.IsVictory(_gameState.Manager);

        // ─── Background based on outcome ──────────────────────────
        if (isChampion)
        {
            AddChild(UITheme.CreateGradientBackground(UITheme.Yellow, UITheme.YellowDark));
        }
        else if (isGameOver)
        {
            AddChild(UITheme.CreateGradientBackground(
                new Color("4B4B4B"), new Color("1A1A1A")));
        }
        else
        {
            AddChild(UITheme.CreateGradientBackground(UITheme.Blue, UITheme.BlueDark));
        }

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

        // ─── Outcome header ──────────────────────────────────────
        if (isChampion)
        {
            var trophy = UITheme.CreateLabel("🏆",
                UITheme.FontSizeDisplay + 24, UITheme.TextLight, HorizontalAlignment.Center);
            root.AddChild(trophy);

            root.AddChild(UITheme.CreateLabel("CHAMPION!",
                UITheme.FontSizeDisplay, UITheme.TextLight, HorizontalAlignment.Center));

            root.AddChild(UITheme.CreateLabel($"{_playerClub.Name} wins the Mundial!",
                UITheme.FontSizeHeading, new Color(1, 1, 1, 0.8f), HorizontalAlignment.Center));

            // Animate trophy
            GetTree().CreateTimer(0.1f).Timeout += () =>
            {
                if (IsInstanceValid(trophy))
                    Anim.BounceIn(trophy, delay: 0f, duration: 0.8f);
            };
        }
        else if (isGameOver)
        {
            root.AddChild(UITheme.CreateLabel("💼",
                UITheme.FontSizeDisplay, UITheme.TextLight, HorizontalAlignment.Center));
            root.AddChild(UITheme.CreateLabel("Game Over",
                UITheme.FontSizeDisplay, UITheme.Red, HorizontalAlignment.Center));
            root.AddChild(UITheme.CreateLabel("You have been sacked.",
                UITheme.FontSizeBody, new Color(1, 1, 1, 0.6f), HorizontalAlignment.Center));
        }
        else
        {
            root.AddChild(UITheme.CreateLabel("📊",
                UITheme.FontSizeDisplay, UITheme.TextLight, HorizontalAlignment.Center));
            root.AddChild(UITheme.CreateLabel("Season Complete",
                UITheme.FontSizeTitle, UITheme.TextLight, HorizontalAlignment.Center));
        }

        // ─── Stats card ──────────────────────────────────────────
        var statsCard = UITheme.CreateCard();
        root.AddChild(statsCard);

        var statsVbox = new VBoxContainer();
        statsVbox.AddThemeConstantOverride("separation", 8);
        statsCard.AddChild(statsVbox);

        statsVbox.AddChild(UITheme.CreateLabel("Season Summary",
            UITheme.FontSizeHeading, UITheme.Blue, HorizontalAlignment.Center));

        AddStatRow(statsVbox, "Club", _playerClub.Name, UITheme.TextDark);
        AddStatRow(statsVbox, "Reputation", $"{_gameState.Manager.Reputation}", UITheme.Yellow);
        AddStatRow(statsVbox, "Balance", FormatMoney(_playerClub.Balance), UITheme.Green);
        AddStatRow(statsVbox, "Squad Size", $"{_playerClub.Team.Players.Count}", UITheme.Blue);
        AddStatRow(statsVbox, "Status", $"{_gameState.Manager.Status}", UITheme.TextSecondary);

        // ─── Transfer history ────────────────────────────────────
        if (_gameState.TransferHistory.Count > 0)
        {
            var transferCard = UITheme.CreateCard(UITheme.Orange);
            root.AddChild(transferCard);

            var transferVbox = new VBoxContainer();
            transferVbox.AddThemeConstantOverride("separation", 4);
            transferCard.AddChild(transferVbox);

            transferVbox.AddChild(UITheme.CreateLabel("Transfer Activity",
                UITheme.FontSizeBody, UITheme.Orange));
            foreach (TransferRecord t in _gameState.TransferHistory.TakeLast(8))
            {
                transferVbox.AddChild(UITheme.CreateLabel(
                    $"  {t.Type}: {t.PlayerName} — {t.Fee:C0}",
                    UITheme.FontSizeSmall, UITheme.TextSecondary));
            }
        }

        // ─── Main menu button ─────────────────────────────────────
        var menuBtn = UITheme.CreateButton("Main Menu",
            isChampion ? UITheme.Yellow : UITheme.Blue,
            isChampion ? UITheme.TextDark : UITheme.TextLight);
        menuBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        menuBtn.CustomMinimumSize = new Vector2(280, 56);
        menuBtn.Pressed += () =>
            SceneManager.Instance.ChangeScene("res://scenes/MainMenu.tscn");
        root.AddChild(menuBtn);

        // Entrance
        Anim.StaggerChildren(root, stagger: 0.08f, useScale: false);
    }

    private static void AddStatRow(VBoxContainer parent, string label, string value, Color valueColor)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);

        var lbl = UITheme.CreateLabel(label, UITheme.FontSizeBody, UITheme.TextSecondary);
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(lbl);

        row.AddChild(UITheme.CreateLabel(value, UITheme.FontSizeBody, valueColor));
    }

    private static string FormatMoney(decimal amount)
    {
        return amount switch
        {
            >= 1_000_000 => $"{amount / 1_000_000:F1}M",
            >= 1_000 => $"{amount / 1_000:F0}K",
            _ => $"{amount:F0}",
        };
    }
}
