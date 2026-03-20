using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Post-match screen — result, MVP, ratings.
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

        // Record the result and finish the day
        var dayResult = _gameState.FinishDay(_ctx, _result);

        // Autosave
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
        var bg = UITheme.CreateBackground(UITheme.Background);
        AddChild(bg);

        var root = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = UITheme.PaddingLarge,
            OffsetRight = -UITheme.PaddingLarge,
            OffsetTop = UITheme.Padding,
            OffsetBottom = -UITheme.Padding,
        };
        root.AddThemeConstantOverride("separation", UITheme.Padding);
        AddChild(root);

        bool isHome = _ctx.PlayerFixture!.HomeClubId == _playerClub.Id;
        int playerGoals = isHome ? _result.ScoreHome : _result.ScoreAway;
        int opponentGoals = isHome ? _result.ScoreAway : _result.ScoreHome;
        bool won = playerGoals > opponentGoals;

        // Result header
        string resultText = won ? "🎉 VICTORY!" : (playerGoals == opponentGoals ? "🤝 DRAW" : "😞 DEFEAT");
        Color resultColor = won ? UITheme.Green : (playerGoals == opponentGoals ? UITheme.Yellow : UITheme.Pink);

        var header = UITheme.CreateLabel(resultText, UITheme.FontSizeTitle,
            resultColor, HorizontalAlignment.Center);
        root.AddChild(header);

        // Score
        var scoreLabel = UITheme.CreateLabel(
            $"{_result.FinalState.ScoreHome} - {_result.FinalState.ScoreAway}",
            48, UITheme.TextPrimary, HorizontalAlignment.Center);
        root.AddChild(scoreLabel);

        var homeClub = _gameState.Clubs.First(c => c.Id == _ctx.PlayerFixture.HomeClubId);
        var awayClub = _gameState.Clubs.First(c => c.Id == _ctx.PlayerFixture.AwayClubId);

        var teams = UITheme.CreateLabel(
            $"{homeClub.Name} vs {awayClub.Name}",
            UITheme.FontSizeBody, UITheme.TextSecondary, HorizontalAlignment.Center);
        root.AddChild(teams);

        // MVP card
        var allPlayers = _playerClub.Team.Players
            .Concat(_gameState.Clubs
                .Where(c => c.Id != _playerClub.Id &&
                    (c.Id == _ctx.PlayerFixture.HomeClubId || c.Id == _ctx.PlayerFixture.AwayClubId))
                .SelectMany(c => c.Team.Players));

        var mvpPlayer = allPlayers.FirstOrDefault(p => p.Id == _result.MvpPlayerId);
        if (mvpPlayer != null)
        {
            var mvpCard = UITheme.CreateCard();
            root.AddChild(mvpCard);

            var mvpVbox = new VBoxContainer();
            mvpVbox.AddThemeConstantOverride("separation", 4);
            mvpCard.AddChild(mvpVbox);

            mvpVbox.AddChild(UITheme.CreateLabel("⭐ Man of the Match",
                UITheme.FontSizeBody, UITheme.Yellow, HorizontalAlignment.Center));
            mvpVbox.AddChild(UITheme.CreateLabel(mvpPlayer.Name,
                UITheme.FontSizeHeading, UITheme.TextPrimary, HorizontalAlignment.Center));

            if (_result.FinalState.PlayerRatings.TryGetValue(mvpPlayer.Id, out float rating))
            {
                mvpVbox.AddChild(UITheme.CreateLabel($"Rating: {rating:F1}",
                    UITheme.FontSizeBody, UITheme.Green, HorizontalAlignment.Center));
            }
        }

        // Goals summary
        var goals = _result.Events.Where(e => e.Type == EventType.Goal).ToList();
        if (goals.Count > 0)
        {
            root.AddChild(UITheme.CreateLabel("⚽ Goals", UITheme.FontSizeHeading, UITheme.Blue));
            foreach (var goal in goals)
            {
                root.AddChild(UITheme.CreateLabel(
                    $"  {goal.Tick}' — {goal.Description}",
                    UITheme.FontSizeSmall, UITheme.TextSecondary));
            }
        }

        // Other match results
        if (dayResult.Fixtures.Count > 1)
        {
            root.AddChild(UITheme.CreateLabel("📋 Other Results",
                UITheme.FontSizeHeading, UITheme.TextSecondary));
            foreach (var fix in dayResult.Fixtures)
            {
                if (fix == _ctx.PlayerFixture) continue;
                if (fix.Result == null) continue;

                var home = _gameState.Clubs.FirstOrDefault(c => c.Id == fix.HomeClubId);
                var away = _gameState.Clubs.FirstOrDefault(c => c.Id == fix.AwayClubId);
                root.AddChild(UITheme.CreateLabel(
                    $"  {home?.Name ?? "?"} {fix.Result.Value.Home} - {fix.Result.Value.Away} {away?.Name ?? "?"}",
                    UITheme.FontSizeSmall, UITheme.TextSecondary));
            }
        }

        // Continue button
        var continueBtn = UITheme.CreateButton("➡️  Continue", UITheme.Blue);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        continueBtn.Pressed += () =>
        {
            if (dayResult.GameOver || dayResult.Victory || dayResult.Finished)
                SceneManager.Instance.ChangeScene("res://scenes/SeasonEnd.tscn");
            else
                SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        };
        root.AddChild(continueBtn);
    }
}
