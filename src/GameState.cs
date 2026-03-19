using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Economy;
using ElevenLegends.Manager;

namespace ElevenLegends;

/// <summary>
/// Central game state. Tracks the current day, clubs, competitions, and manager.
/// Call AdvanceDay() to progress the simulation one day at a time.
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

    public ManagerState Manager { get; }
    public IReadOnlyList<Club> Clubs => _clubs;
    public CompetitionManager Competition => _competition;
    public IReadOnlyList<SeasonDay> Calendar => _calendar;

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
    }

    /// <summary>
    /// Returns the player's club.
    /// </summary>
    public Club PlayerClub => _clubs.First(c => c.Id == Manager.ClubId);

    /// <summary>
    /// Advances one day. Returns a summary of what happened.
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
        }

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

        return result;
    }

    private void ProcessTraining()
    {
        // In the demo, training recovers morale slightly
        foreach (var club in _clubs)
        {
            var updatedPlayers = club.Team.Players.Select(p => p with
            {
                Morale = Math.Min(100, p.Morale + 2)
            }).ToList();

            club.Team = club.Team with { Players = updatedPlayers };
        }
    }

    private void ProcessNationalMatchDay(DayResult result)
    {
        _nationalMatchDayCount++;
        var fixtures = _competition.GenerateNationalRound(CurrentDay.Day);
        int daySeed = _baseSeed + CurrentDay.Day * 1000;
        _competition.SimulateFixtures(fixtures, daySeed);
        result.Fixtures = fixtures;

        // Economy: match revenue + prizes
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

        // If nationals done, create mundial
        if (_competition.AreNationalsFinished() && _competition.MundialBracket == null)
        {
            _competition.CreateMundial();
        }
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

        // Update manager reputation based on player's club performance
        var playerFixture = fixtures.FirstOrDefault(f =>
            f.HomeClubId == Manager.ClubId || f.AwayClubId == Manager.ClubId);
        if (playerFixture != null)
        {
            bool advanced = playerFixture.WinnerClubId == Manager.ClubId;
            CareerManager.UpdateReputation(Manager, playerFixture.Phase, advanced);
        }
    }


}

/// <summary>
/// Summary of what happened during a single day advance.
/// </summary>
public sealed class DayResult
{
    public required SeasonDay Day { get; init; }
    public IReadOnlyList<MatchFixture> Fixtures { get; set; } = [];
    public bool GameOver { get; set; }
    public bool Victory { get; set; }
    public bool Finished { get; set; }
}
