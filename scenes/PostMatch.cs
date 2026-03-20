using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.Simulation;
using ElevenLegends.UI;
using Theme = ElevenLegends.UI.Theme;

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
        var bg = Theme.CreateBackground(Theme.Background);
        AddChild(bg);

        var root = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            OffsetLeft = Theme.PaddingLarge,
            OffsetRight = -Theme.PaddingLarge,
            OffsetTop = Theme.Padding,
            OffsetBottom = -Theme.Padding,
        };
        root.AddThemeConstantOverride("separation", Theme.Padding);
        AddChild(root);

        bool isHome = _ctx.PlayerFixture!.HomeClubId == _playerClub.Id;
        int playerGoals = isHome ? _result.ScoreHome : _result.ScoreAway;
        int opponentGoals = isHome ? _result.ScoreAway : _result.ScoreHome;
        bool won = playerGoals > opponentGoals;

        // Result header
        string resultText = won ? "🎉 VICTORY!" : (playerGoals == opponentGoals ? "🤝 DRAW" : "😞 DEFEAT");
        Color resultColor = won ? Theme.Green : (playerGoals == opponentGoals ? Theme.Yellow : Theme.Pink);

        var header = Theme.CreateLabel(resultText, Theme.FontSizeTitle,
            resultColor, HorizontalAlignment.Center);
        root.AddChild(header);

        // Score
        var scoreLabel = Theme.CreateLabel(
            $"{_result.FinalState.ScoreHome} - {_result.FinalState.ScoreAway}",
            48, Theme.TextPrimary, HorizontalAlignment.Center);
        root.AddChild(scoreLabel);

        var homeClub = _gameState.Clubs.First(c => c.Id == _ctx.PlayerFixture.HomeClubId);
        var awayClub = _gameState.Clubs.First(c => c.Id == _ctx.PlayerFixture.AwayClubId);

        var teams = Theme.CreateLabel(
            $"{homeClub.Name} vs {awayClub.Name}",
            Theme.FontSizeBody, Theme.TextSecondary, HorizontalAlignment.Center);
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
            var mvpCard = Theme.CreateCard();
            root.AddChild(mvpCard);

            var mvpVbox = new VBoxContainer();
            mvpVbox.AddThemeConstantOverride("separation", 4);
            mvpCard.AddChild(mvpVbox);

            mvpVbox.AddChild(Theme.CreateLabel("⭐ Man of the Match",
                Theme.FontSizeBody, Theme.Yellow, HorizontalAlignment.Center));
            mvpVbox.AddChild(Theme.CreateLabel(mvpPlayer.Name,
                Theme.FontSizeHeading, Theme.TextPrimary, HorizontalAlignment.Center));

            if (_result.FinalState.PlayerRatings.TryGetValue(mvpPlayer.Id, out float rating))
            {
                mvpVbox.AddChild(Theme.CreateLabel($"Rating: {rating:F1}",
                    Theme.FontSizeBody, Theme.Green, HorizontalAlignment.Center));
            }
        }

        // Goals summary
        var goals = _result.Events.Where(e => e.Type == EventType.Goal).ToList();
        if (goals.Count > 0)
        {
            root.AddChild(Theme.CreateLabel("⚽ Goals", Theme.FontSizeHeading, Theme.Blue));
            foreach (var goal in goals)
            {
                root.AddChild(Theme.CreateLabel(
                    $"  {goal.Tick}' — {goal.Description}",
                    Theme.FontSizeSmall, Theme.TextSecondary));
            }
        }

        // Other match results
        if (dayResult.Fixtures.Count > 1)
        {
            root.AddChild(Theme.CreateLabel("📋 Other Results",
                Theme.FontSizeHeading, Theme.TextSecondary));
            foreach (var fix in dayResult.Fixtures)
            {
                if (fix == _ctx.PlayerFixture) continue;
                if (fix.Result == null) continue;

                var home = _gameState.Clubs.FirstOrDefault(c => c.Id == fix.HomeClubId);
                var away = _gameState.Clubs.FirstOrDefault(c => c.Id == fix.AwayClubId);
                root.AddChild(Theme.CreateLabel(
                    $"  {home?.Name ?? "?"} {fix.Result.Value.Home} - {fix.Result.Value.Away} {away?.Name ?? "?"}",
                    Theme.FontSizeSmall, Theme.TextSecondary));
            }
        }

        // Continue button
        var continueBtn = Theme.CreateButton("➡️  Continue", Theme.Blue);
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
