using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Competition;

/// <summary>
/// Manages a mundial bracket for 4 national champions: Semis → Final.
/// </summary>
public sealed class MundialBracket
{
    private readonly List<int> _initialTeamIds;
    private readonly List<MatchFixture> _fixtures = [];
    private CompetitionPhase _currentPhase = CompetitionPhase.MundialSemifinals;
    private List<int> _advancingTeams = [];
    private int? _championId;

    public IReadOnlyList<MatchFixture> Fixtures => _fixtures;
    public CompetitionPhase CurrentPhase => _currentPhase;
    public int? ChampionId => _championId;
    public bool IsFinished => _currentPhase == CompetitionPhase.Finished;

    public MundialBracket(IReadOnlyList<int> nationalChampionIds)
    {
        if (nationalChampionIds.Count != 4)
            throw new ArgumentException("Mundial bracket requires exactly 4 teams.");

        _initialTeamIds = [.. nationalChampionIds];
    }

    /// <summary>
    /// Generates fixtures for the current phase.
    /// </summary>
    public IReadOnlyList<MatchFixture> GenerateNextRound(int startDay)
    {
        if (IsFinished)
            return [];

        var teams = _currentPhase == CompetitionPhase.MundialSemifinals
            ? _initialTeamIds
            : _advancingTeams;

        var roundFixtures = new List<MatchFixture>();
        for (int i = 0; i < teams.Count; i += 2)
        {
            roundFixtures.Add(new MatchFixture
            {
                Day = startDay,
                HomeClubId = teams[i],
                AwayClubId = teams[i + 1],
                Phase = _currentPhase
            });
        }

        _fixtures.AddRange(roundFixtures);
        return roundFixtures;
    }

    /// <summary>
    /// Records results and advances the bracket.
    /// </summary>
    public void AdvanceRound()
    {
        _advancingTeams = _fixtures
            .Where(f => f.Phase == _currentPhase && f.WinnerClubId.HasValue)
            .Select(f => f.WinnerClubId!.Value)
            .ToList();

        _currentPhase = _currentPhase switch
        {
            CompetitionPhase.MundialSemifinals => CompetitionPhase.MundialFinal,
            CompetitionPhase.MundialFinal => CompetitionPhase.Finished,
            _ => _currentPhase
        };

        if (_currentPhase == CompetitionPhase.Finished && _advancingTeams.Count == 1)
        {
            _championId = _advancingTeams[0];
        }
    }
}
