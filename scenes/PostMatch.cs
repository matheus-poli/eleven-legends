using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Post-match screen — result, MVP, ratings with celebration animations.
/// </summary>
public partial class PostMatchScreen : Control
{
    public static MatchResult? PendingResult;
    public static MatchDayContext? PendingContext;

    private MatchResult _result = null!;
    private MatchDayContext _ctx = null!;
    private GameState _gameState = null!;
    private Club _playerClub = null!;

    public override void _Ready()
    {
        _result = PendingResult!;
        _ctx = PendingContext!;
        _gameState = SceneManager.Instance.CurrentGameState!;
        _playerClub = _gameState.PlayerClub;

        PendingResult = null;
        PendingContext = null;

        DayResult dayResult = _gameState.FinishDay(_ctx, _result);

        try
        {
            var sm = new SaveManager(System.IO.Path.Combine(OS.GetUserDataDir(), "saves"));
            sm.AutoSave(_gameState);
        }
        catch { /* silent */ }

        BuildUI(dayResult);
    }

    private void BuildUI(DayResult dayResult)
    {
        bool isHome = _ctx.PlayerFixture!.HomeClubId == _playerClub.Id;
        int playerGoals = isHome ? _result.ScoreHome : _result.ScoreAway;
        int opponentGoals = isHome ? _result.ScoreAway : _result.ScoreHome;
        bool won = playerGoals > opponentGoals;
        bool draw = playerGoals == opponentGoals;

        // ─── Background based on result ───────────────────────────
        if (won)
        {
            AddChild(UITheme.CreateGradientBackground(UITheme.Green, UITheme.GreenDark));
        }
        else if (draw)
        {
            AddChild(UITheme.CreateGradientBackground(UITheme.Yellow, UITheme.YellowDark));
        }
        else
        {
            AddChild(UITheme.CreateGradientBackground(
                new Color("4B4B4B"), new Color("2A2A2A")));
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

        // ─── Result header ────────────────────────────────────────
        string resultText = won ? "VICTORY!" : (draw ? "DRAW" : "DEFEAT");
        string resultEmoji = won ? "🎉" : (draw ? "🤝" : "😞");

        var resultLabel = UITheme.CreateLabel(resultEmoji,
            UITheme.FontSizeDisplay + 16, UITheme.TextLight, HorizontalAlignment.Center);
        root.AddChild(resultLabel);

        root.AddChild(UITheme.CreateLabel(resultText,
            UITheme.FontSizeDisplay, UITheme.TextLight, HorizontalAlignment.Center));

        // Score
        root.AddChild(UITheme.CreateLabel(
            $"{_result.FinalState.ScoreHome} - {_result.FinalState.ScoreAway}",
            UITheme.FontSizeDisplay, UITheme.TextLight, HorizontalAlignment.Center));

        Club homeClub = _gameState.Clubs.First(c => c.Id == _ctx.PlayerFixture.HomeClubId);
        Club awayClub = _gameState.Clubs.First(c => c.Id == _ctx.PlayerFixture.AwayClubId);

        root.AddChild(UITheme.CreateLabel(
            $"{homeClub.Name} vs {awayClub.Name}",
            UITheme.FontSizeBody, new Color(1, 1, 1, 0.7f), HorizontalAlignment.Center));

        // ─── MVP card ─────────────────────────────────────────────
        var allPlayers = _playerClub.Team.Players
            .Concat(_gameState.Clubs
                .Where(c => c.Id != _playerClub.Id &&
                    (c.Id == _ctx.PlayerFixture.HomeClubId || c.Id == _ctx.PlayerFixture.AwayClubId))
                .SelectMany(c => c.Team.Players));

        Player? mvp = allPlayers.FirstOrDefault(p => p.Id == _result.MvpPlayerId);
        if (mvp != null)
        {
            var mvpCard = UITheme.CreateCard(UITheme.Yellow);
            root.AddChild(mvpCard);

            var mvpVbox = new VBoxContainer();
            mvpVbox.AddThemeConstantOverride("separation", 4);
            mvpCard.AddChild(mvpVbox);

            mvpVbox.AddChild(UITheme.CreateLabel("⭐ Man of the Match",
                UITheme.FontSizeSmall, UITheme.Yellow, HorizontalAlignment.Center));
            mvpVbox.AddChild(UITheme.CreateLabel(mvp.Name,
                UITheme.FontSizeHeading, UITheme.TextDark, HorizontalAlignment.Center));

            if (_result.FinalState.PlayerRatings.TryGetValue(mvp.Id, out float rating))
            {
                mvpVbox.AddChild(UITheme.CreateLabel($"Rating: {rating:F1}",
                    UITheme.FontSizeBody, UITheme.Green, HorizontalAlignment.Center));
            }
        }

        // ─── Goals summary ────────────────────────────────────────
        var goals = _result.Events.Where(e => e.Type == EventType.Goal).ToList();
        if (goals.Count > 0)
        {
            var goalCard = UITheme.CreateCard(UITheme.Green);
            root.AddChild(goalCard);

            var goalVbox = new VBoxContainer();
            goalVbox.AddThemeConstantOverride("separation", 4);
            goalCard.AddChild(goalVbox);

            goalVbox.AddChild(UITheme.CreateLabel("Goals",
                UITheme.FontSizeBody, UITheme.Green));
            foreach (MatchEvent goal in goals)
            {
                goalVbox.AddChild(UITheme.CreateLabel(
                    $"  {goal.Tick}' — {goal.Description}",
                    UITheme.FontSizeSmall, UITheme.TextSecondary));
            }
        }

        // ─── Other results ────────────────────────────────────────
        if (dayResult.Fixtures.Count > 1)
        {
            var otherCard = UITheme.CreateCard();
            root.AddChild(otherCard);

            var otherVbox = new VBoxContainer();
            otherVbox.AddThemeConstantOverride("separation", 4);
            otherCard.AddChild(otherVbox);

            otherVbox.AddChild(UITheme.CreateLabel("Other Results",
                UITheme.FontSizeBody, UITheme.TextSecondary));
            foreach (MatchFixture fix in dayResult.Fixtures)
            {
                if (fix == _ctx.PlayerFixture || fix.Result == null) continue;
                Club? home = _gameState.Clubs.FirstOrDefault(c => c.Id == fix.HomeClubId);
                Club? away = _gameState.Clubs.FirstOrDefault(c => c.Id == fix.AwayClubId);
                otherVbox.AddChild(UITheme.CreateLabel(
                    $"  {home?.Name ?? "?"} {fix.Result.Value.Home} - {fix.Result.Value.Away} {away?.Name ?? "?"}",
                    UITheme.FontSizeSmall, UITheme.TextSecondary));
            }
        }

        // ─── Continue button ──────────────────────────────────────
        var continueBtn = UITheme.CreateButton("Continue",
            won ? UITheme.Yellow : UITheme.Blue,
            won ? UITheme.TextDark : UITheme.TextLight);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueBtn.CustomMinimumSize = new Vector2(280, 56);
        continueBtn.Pressed += () =>
        {
            if (dayResult.GameOver || dayResult.Victory || dayResult.Finished)
                SceneManager.Instance.ChangeScene("res://scenes/SeasonEnd.tscn");
            else
                SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        };
        root.AddChild(continueBtn);

        // ─── Entrance animations ──────────────────────────────────
        Anim.BounceIn(resultLabel, delay: 0.1f, duration: 0.6f);
        Anim.StaggerChildren(root, stagger: 0.08f, useScale: false);
    }
}
