using Godot;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Persistence;
using ElevenLegends.Scenes.Components;
using ElevenLegends.Simulation;
using ElevenLegends.UI;

namespace ElevenLegends.Scenes;

/// <summary>
/// Post-match screen — pitch view with final ratings + results sidebar.
/// </summary>
public partial class PostMatch : Control
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

        // Snapshot balance before processing
        decimal balanceBefore = _playerClub.Balance;

        DayResult dayResult = _gameState.FinishDay(_ctx, _result);

        decimal moneyGained = _playerClub.Balance - balanceBefore;

        try
        {
            var sm = new SaveManager(System.IO.Path.Combine(OS.GetUserDataDir(), "saves"));
            sm.AutoSave(_gameState);
        }
        catch { /* silent */ }

        BuildUI(dayResult, moneyGained);
    }

    private void BuildUI(DayResult dayResult, decimal moneyGained)
    {
        bool isHome = _ctx.PlayerFixture!.HomeClubId == _playerClub.Id;
        int playerGoals = isHome ? _result.ScoreHome : _result.ScoreAway;
        int opponentGoals = isHome ? _result.ScoreAway : _result.ScoreHome;
        bool won = playerGoals > opponentGoals;
        bool draw = playerGoals == opponentGoals;

        // Background
        if (won)
            AddChild(UITheme.CreateGradientBackground(UITheme.Green, UITheme.GreenDark));
        else if (draw)
            AddChild(UITheme.CreateGradientBackground(UITheme.Yellow, UITheme.YellowDark));
        else
            AddChild(UITheme.CreateGradientBackground(new Color("4B4B4B"), new Color("2A2A2A")));

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = UITheme.PaddingSmall;
        root.OffsetRight = -UITheme.PaddingSmall;
        root.OffsetTop = UITheme.PaddingSmall;
        root.OffsetBottom = -UITheme.PaddingSmall;
        root.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        AddChild(root);

        // ─── Score header ─────────────────────────────────────────
        string resultText = won ? "VICTORY!" : (draw ? "DRAW" : "DEFEAT");
        string resultIcon = won ? "confetti" : (draw ? "handshake" : "sad-face");

        var headerRow = new HBoxContainer();
        headerRow.AddThemeConstantOverride("separation", UITheme.Padding);
        headerRow.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        root.AddChild(headerRow);

        headerRow.AddChild(UITheme.CreateIcon(resultIcon, new Vector2(36, 36)));
        headerRow.AddChild(UITheme.CreateLabel(resultText, UITheme.FontSizeTitle, UITheme.TextLight));
        headerRow.AddChild(UITheme.CreateLabel(
            $"{_result.FinalState.ScoreHome} - {_result.FinalState.ScoreAway}",
            UITheme.FontSizeTitle, UITheme.TextLight));

        Club homeClub = _gameState.Clubs.First(c => c.Id == _ctx.PlayerFixture.HomeClubId);
        Club awayClub = _gameState.Clubs.First(c => c.Id == _ctx.PlayerFixture.AwayClubId);

        root.AddChild(UITheme.CreateLabel(
            $"{homeClub.Name} vs {awayClub.Name}",
            UITheme.FontSizeSmall, new Color(1, 1, 1, 0.6f), HorizontalAlignment.Center));

        // ─── Main content: pitch + sidebar ────────────────────────
        var content = new HBoxContainer();
        content.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        content.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(content);

        // Left: pitch with final ratings
        var pitchView = new MatchPitchView
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.6f,
        };
        content.AddChild(pitchView);

        // Build config for pitch view from result data
        MatchConfig matchConfig = _gameState.BuildPlayerMatchConfig(_ctx, null);
        Formation homeForm = _result.FinalState.Phase != MatchPhase.Finished
            ? Formation.F442
            : (matchConfig.HomeTactics?.Formation ?? Formation.F442);
        Formation awayForm = matchConfig.AwayTactics?.Formation ?? Formation.F442;

        pitchView.Setup(matchConfig, homeForm, awayForm);
        pitchView.UpdateRatings(_result.FinalState.PlayerRatings);

        // Right: results sidebar
        var sidebar = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1.0f,
        };
        sidebar.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        content.AddChild(sidebar);

        var sideScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            ClipContents = true,
        };
        sidebar.AddChild(sideScroll);

        var sideContent = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        sideContent.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        sideScroll.AddChild(sideContent);

        // MVP
        var allPlayers = homeClub.Team.Players.Concat(awayClub.Team.Players);
        Player? mvp = allPlayers.FirstOrDefault(p => p.Id == _result.MvpPlayerId);
        if (mvp != null)
        {
            float mvpRating = _result.FinalState.PlayerRatings.GetValueOrDefault(mvp.Id, 6f);
            var mvpCard = UITheme.CreateCard(UITheme.Yellow);
            sideContent.AddChild(mvpCard);

            var mvpVbox = new VBoxContainer();
            mvpVbox.AddThemeConstantOverride("separation", 2);
            mvpCard.AddChild(mvpVbox);

            var mvpTitle = UITheme.CreateIconLabel("sparkle", "Man of the Match",
                UITheme.FontSizeCaption, UITheme.Yellow);
            mvpTitle.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            mvpVbox.AddChild(mvpTitle);
            mvpVbox.AddChild(UITheme.CreateLabel(mvp.Name,
                UITheme.FontSizeBody, UITheme.TextDark, HorizontalAlignment.Center));
            mvpVbox.AddChild(UITheme.CreateLabel($"Rating: {mvpRating:F1}",
                UITheme.FontSizeSmall, UITheme.Green, HorizontalAlignment.Center));
        }

        // SVP
        Player? svp = allPlayers.FirstOrDefault(p => p.Id == _result.SvpPlayerId);
        if (svp != null && svp.Id != (mvp?.Id ?? 0))
        {
            float svpRating = _result.FinalState.PlayerRatings.GetValueOrDefault(svp.Id, 6f);
            var svpCard = UITheme.CreateCard(UITheme.Blue);
            sideContent.AddChild(svpCard);
            var svpVbox = new VBoxContainer();
            svpVbox.AddThemeConstantOverride("separation", 2);
            svpCard.AddChild(svpVbox);
            svpVbox.AddChild(UITheme.CreateLabel("2nd Best",
                UITheme.FontSizeCaption, UITheme.Blue, HorizontalAlignment.Center));
            svpVbox.AddChild(UITheme.CreateLabel(svp.Name,
                UITheme.FontSizeSmall, UITheme.TextDark, HorizontalAlignment.Center));
            svpVbox.AddChild(UITheme.CreateLabel($"Rating: {svpRating:F1}",
                UITheme.FontSizeCaption, UITheme.Blue, HorizontalAlignment.Center));
        }

        // Money and morale
        if (moneyGained != 0)
        {
            Color moneyColor = moneyGained > 0 ? UITheme.Green : UITheme.Red;
            string sign = moneyGained > 0 ? "+" : "";
            sideContent.AddChild(UITheme.CreateIconLabel("coin",
                $"{sign}{FormatMoney(moneyGained)}", UITheme.FontSizeSmall, moneyColor));
        }

        // Goals
        var goals = _result.Events.Where(e => e.Type == EventType.Goal).ToList();
        if (goals.Count > 0)
        {
            var goalCard = UITheme.CreateCard(UITheme.Green);
            sideContent.AddChild(goalCard);
            var goalVbox = new VBoxContainer();
            goalVbox.AddThemeConstantOverride("separation", 2);
            goalCard.AddChild(goalVbox);
            goalVbox.AddChild(UITheme.CreateLabel("Goals", UITheme.FontSizeSmall, UITheme.Green));
            foreach (MatchEvent goal in goals)
                goalVbox.AddChild(UITheme.CreateLabel(
                    $"  {goal.Tick}' {goal.Description}", 11, UITheme.TextSecondary));
        }

        // Other results
        if (dayResult.Fixtures.Count > 1)
        {
            var otherCard = UITheme.CreateCard();
            sideContent.AddChild(otherCard);
            var otherVbox = new VBoxContainer();
            otherVbox.AddThemeConstantOverride("separation", 2);
            otherCard.AddChild(otherVbox);
            otherVbox.AddChild(UITheme.CreateLabel("Other Results", UITheme.FontSizeSmall, UITheme.TextSecondary));
            foreach (MatchFixture fix in dayResult.Fixtures)
            {
                if (fix == _ctx.PlayerFixture || fix.Result == null) continue;
                Club? h = _gameState.Clubs.FirstOrDefault(c => c.Id == fix.HomeClubId);
                Club? a = _gameState.Clubs.FirstOrDefault(c => c.Id == fix.AwayClubId);
                otherVbox.AddChild(UITheme.CreateLabel(
                    $"  {h?.Name ?? "?"} {fix.Result.Value.Home}-{fix.Result.Value.Away} {a?.Name ?? "?"}",
                    10, UITheme.TextSecondary));
            }
        }

        // ─── Continue button ──────────────────────────────────────
        var continueBtn = UITheme.CreateButton("Continue",
            won ? UITheme.Yellow : UITheme.Blue,
            won ? UITheme.TextDark : UITheme.TextLight);
        continueBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        continueBtn.CustomMinimumSize = new Vector2(0, 48);
        continueBtn.Pressed += () =>
        {
            if (dayResult.GameOver || dayResult.Victory || dayResult.Finished)
                SceneManager.Instance.ChangeScene("res://scenes/SeasonEnd.tscn");
            else
                SceneManager.Instance.ChangeScene("res://scenes/DayHub.tscn");
        };
        root.AddChild(continueBtn);

        Anim.StaggerChildren(root, stagger: 0.06f, useScale: false);
    }

    private static string FormatMoney(decimal amount)
    {
        return amount switch
        {
            >= 1_000_000 or <= -1_000_000 => $"{amount / 1_000_000:F1}M",
            >= 1_000 or <= -1_000 => $"{amount / 1_000:F0}K",
            _ => $"{amount:F0}",
        };
    }
}
