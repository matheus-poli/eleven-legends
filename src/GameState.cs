using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Economy;
using ElevenLegends.Manager;
using ElevenLegends.Simulation;
using ElevenLegends.Transfers;

namespace ElevenLegends;

/// <summary>
/// Central game state. Tracks the current day, clubs, competitions, and manager.
/// Call AdvanceDay() for auto-mode, or use PrepareMatchDay/FinishDay for interactive mode.
/// </summary>
public sealed class GameState
{
    private readonly List<Club> _clubs;
    private readonly CompetitionManager _competition;
    private readonly List<SeasonDay> _calendar;
    private readonly int _baseSeed;

    private int _currentDayIndex;
    private int _nationalMatchDayCount;
    private int _mundialMatchDayCount;
    private int _daysSinceSalary;
    private int _nextPlayerId;
    private int _transferDayCount;

    public ManagerState Manager { get; }
    public IReadOnlyList<Club> Clubs => _clubs;
    public CompetitionManager Competition => _competition;
    public IReadOnlyList<SeasonDay> Calendar => _calendar;
    public List<TransferRecord> TransferHistory { get; } = [];
    public List<LoanRecord> ActiveLoans { get; } = [];

    public int CurrentDayIndex => _currentDayIndex;
    public SeasonDay CurrentDay => _calendar[_currentDayIndex];
    public bool IsSeasonOver => _currentDayIndex >= _calendar.Count;

    public GameState(List<Club> clubs, ManagerState manager, int seed)
    {
        _clubs = clubs;
        Manager = manager;
        _baseSeed = seed;
        _competition = new CompetitionManager(clubs, seed);
        _calendar = SeasonCalendar.BuildTemplate();
        _nextPlayerId = YouthAcademy.GetMaxPlayerId(clubs) + 1;
    }

    /// <summary>
    /// Returns the player's club.
    /// </summary>
    public Club PlayerClub => _clubs.First(c => c.Id == Manager.ClubId);

    /// <summary>
    /// Advances one day automatically (no interactive decisions).
    /// </summary>
    public DayResult AdvanceDay()
    {
        if (IsSeasonOver || CareerManager.IsGameOver(Manager) || CareerManager.IsVictory(Manager))
            return new DayResult { Day = CurrentDay, Finished = true };

        var day = CurrentDay;
        var result = new DayResult { Day = day };

        switch (day.Type)
        {
            case DayType.Training:
                ProcessTraining();
                break;

            case DayType.MatchDay:
                ProcessNationalMatchDay(result);
                break;

            case DayType.MundialMatchDay:
                ProcessMundialMatchDay(result);
                break;

            case DayType.TransferWindow:
                ProcessTransferDay(result);
                break;
        }

        FinishDayCommon(result);
        return result;
    }

    /// <summary>
    /// Prepares match day: generates fixtures, simulates all non-player matches.
    /// Returns the player's fixture (if any) and all fixtures for the day.
    /// Use this for interactive mode — call FinishDay() after resolving the player's match.
    /// </summary>
    public MatchDayContext PrepareMatchDay()
    {
        var day = CurrentDay;
        bool isMundial = day.Type == DayType.MundialMatchDay;

        List<MatchFixture> fixtures;
        int daySeed;

        if (isMundial)
        {
            _mundialMatchDayCount++;
            fixtures = _competition.GenerateMundialRound(day.Day);
            daySeed = _baseSeed + day.Day * 1000 + 500;
        }
        else
        {
            _nationalMatchDayCount++;
            fixtures = _competition.GenerateNationalRound(day.Day);
            daySeed = _baseSeed + day.Day * 1000;
        }

        // Find player's fixture
        MatchFixture? playerFixture = fixtures.FirstOrDefault(f =>
            f.HomeClubId == Manager.ClubId || f.AwayClubId == Manager.ClubId);

        // Simulate all non-player fixtures
        var otherFixtures = fixtures.Where(f => f != playerFixture).ToList();
        _competition.SimulateFixtures(otherFixtures, daySeed);

        // Compute the seed for the player's match
        int playerMatchSeed = playerFixture != null
            ? daySeed + fixtures.IndexOf(playerFixture)
            : 0;

        return new MatchDayContext
        {
            AllFixtures = fixtures,
            PlayerFixture = playerFixture,
            PlayerMatchSeed = playerMatchSeed,
            IsMundial = isMundial
        };
    }

    /// <summary>
    /// Finishes a match day after the player's match has been resolved.
    /// Records the player match result, processes economy, career, and advances the day.
    /// </summary>
    public DayResult FinishDay(MatchDayContext ctx, MatchResult? playerResult)
    {
        // Record player match result
        if (ctx.PlayerFixture != null && playerResult != null)
        {
            ctx.PlayerFixture.Result = (
                playerResult.FinalState.ScoreHome,
                playerResult.FinalState.ScoreAway);
        }

        var result = new DayResult { Day = CurrentDay, Fixtures = ctx.AllFixtures };

        // Economy for ALL fixtures
        foreach (var fixture in ctx.AllFixtures)
        {
            var home = _clubs.First(c => c.Id == fixture.HomeClubId);
            var away = _clubs.First(c => c.Id == fixture.AwayClubId);
            var phase = fixture.Phase;
            bool homeWon = fixture.WinnerClubId == home.Id;

            EconomyProcessor.ProcessMatchDay(home, phase, homeWon);
            EconomyProcessor.ProcessMatchDay(away, phase, !homeWon);
        }

        // Advance competition brackets
        if (ctx.IsMundial)
        {
            _competition.AdvanceMundialRound();

            // Update manager reputation
            if (ctx.PlayerFixture != null)
            {
                bool advanced = ctx.PlayerFixture.WinnerClubId == Manager.ClubId;
                CareerManager.UpdateReputation(Manager, ctx.PlayerFixture.Phase, advanced);
            }
        }
        else
        {
            _competition.AdvanceNationalRounds();

            if (_competition.AreNationalsFinished() && _competition.MundialBracket == null)
                _competition.CreateMundial();
        }

        FinishDayCommon(result);
        return result;
    }

    /// <summary>
    /// Returns the MatchConfig for the player's match based on tactical setup.
    /// </summary>
    public MatchConfig BuildPlayerMatchConfig(MatchDayContext ctx, TacticalSetup? tactics)
    {
        if (ctx.PlayerFixture == null)
            throw new InvalidOperationException("No player fixture on this day.");

        var homeClub = _clubs.First(c => c.Id == ctx.PlayerFixture.HomeClubId);
        var awayClub = _clubs.First(c => c.Id == ctx.PlayerFixture.AwayClubId);
        bool isHome = ctx.PlayerFixture.HomeClubId == Manager.ClubId;

        return new MatchConfig
        {
            HomeTeam = homeClub.Team,
            AwayTeam = awayClub.Team,
            Seed = ctx.PlayerMatchSeed,
            HomeTactics = isHome ? tactics : null,
            AwayTactics = isHome ? null : tactics
        };
    }

    private void ProcessTraining()
    {
        foreach (var club in _clubs)
        {
            var updatedPlayers = club.Team.Players.Select(p => p with
            {
                Morale = Math.Min(100, p.Morale + 2)
            }).ToList();

            club.Team = club.Team with { Players = updatedPlayers };
        }
    }

    private void ProcessTransferDay(DayResult result)
    {
        _transferDayCount++;
        int daySeed = _baseSeed + CurrentDay.Day * 2000;
        var rng = new SeededRng(daySeed);

        // AI clubs make transfers
        var aiRecords = AITransferAgent.ProcessDay(_clubs, Manager.ClubId, rng, ref _nextPlayerId);
        TransferHistory.AddRange(aiRecords);
        result.TransferRecords = aiRecords;
    }

    /// <summary>
    /// Returns the next available player ID and increments the counter.
    /// </summary>
    public int GetNextPlayerId(int count = 1)
    {
        int id = _nextPlayerId;
        _nextPlayerId += count;
        return id;
    }

    /// <summary>
    /// Records a player transfer in the history.
    /// </summary>
    public void RecordTransfer(TransferRecord record)
    {
        TransferHistory.Add(record);
    }

    /// <summary>
    /// Records an active loan.
    /// </summary>
    public void RecordLoan(LoanRecord loan)
    {
        ActiveLoans.Add(loan);
    }

    private void ProcessNationalMatchDay(DayResult result)
    {
        _nationalMatchDayCount++;
        var fixtures = _competition.GenerateNationalRound(CurrentDay.Day);
        int daySeed = _baseSeed + CurrentDay.Day * 1000;
        _competition.SimulateFixtures(fixtures, daySeed);
        result.Fixtures = fixtures;

        foreach (var fixture in fixtures)
        {
            var home = _clubs.First(c => c.Id == fixture.HomeClubId);
            var away = _clubs.First(c => c.Id == fixture.AwayClubId);
            var phase = fixture.Phase;
            bool homeWon = fixture.WinnerClubId == home.Id;

            EconomyProcessor.ProcessMatchDay(home, phase, homeWon);
            EconomyProcessor.ProcessMatchDay(away, phase, !homeWon);
        }

        _competition.AdvanceNationalRounds();

        if (_competition.AreNationalsFinished() && _competition.MundialBracket == null)
            _competition.CreateMundial();
    }

    private void ProcessMundialMatchDay(DayResult result)
    {
        _mundialMatchDayCount++;
        var fixtures = _competition.GenerateMundialRound(CurrentDay.Day);
        int daySeed = _baseSeed + CurrentDay.Day * 1000 + 500;
        _competition.SimulateFixtures(fixtures, daySeed);
        result.Fixtures = fixtures;

        foreach (var fixture in fixtures)
        {
            var home = _clubs.First(c => c.Id == fixture.HomeClubId);
            var away = _clubs.First(c => c.Id == fixture.AwayClubId);
            var phase = fixture.Phase;
            bool homeWon = fixture.WinnerClubId == home.Id;

            EconomyProcessor.ProcessMatchDay(home, phase, homeWon);
            EconomyProcessor.ProcessMatchDay(away, phase, !homeWon);
        }

        _competition.AdvanceMundialRound();

        var playerFixture = fixtures.FirstOrDefault(f =>
            f.HomeClubId == Manager.ClubId || f.AwayClubId == Manager.ClubId);
        if (playerFixture != null)
        {
            bool advanced = playerFixture.WinnerClubId == Manager.ClubId;
            CareerManager.UpdateReputation(Manager, playerFixture.Phase, advanced);
        }
    }

    private void FinishDayCommon(DayResult result)
    {
        // Weekly salary (every 7 days)
        _daysSinceSalary++;
        if (_daysSinceSalary >= 7)
        {
            _daysSinceSalary = 0;
            foreach (var club in _clubs)
                EconomyProcessor.ProcessWeeklySalary(club);
            CareerManager.PaySalary(Manager);
        }

        // Check game end conditions
        CareerManager.CheckDismissal(Manager, PlayerClub);
        if (_competition.IsSeasonComplete())
            CareerManager.CheckVictory(Manager, _competition.GetMundialChampion());

        _currentDayIndex++;

        if (CareerManager.IsGameOver(Manager))
            result.GameOver = true;
        if (CareerManager.IsVictory(Manager))
            result.Victory = true;
        if (_currentDayIndex >= _calendar.Count)
            result.Finished = true;
    }
}

/// <summary>
/// Summary of what happened during a single day advance.
/// </summary>
public sealed class DayResult
{
    public required SeasonDay Day { get; init; }
    public IReadOnlyList<MatchFixture> Fixtures { get; set; } = [];
    public IReadOnlyList<TransferRecord> TransferRecords { get; set; } = [];
    public bool GameOver { get; set; }
    public bool Victory { get; set; }
    public bool Finished { get; set; }
}

/// <summary>
/// Context for an interactive match day. Created by PrepareMatchDay(), consumed by FinishDay().
/// </summary>
public sealed class MatchDayContext
{
    public required List<MatchFixture> AllFixtures { get; init; }
    public MatchFixture? PlayerFixture { get; init; }
    public int PlayerMatchSeed { get; init; }
    public bool IsMundial { get; init; }
}
