using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;

namespace ElevenLegends.Competition;

/// <summary>
/// Manages a knockout bracket for 8 teams: Quarters → Semis → Final.
/// </summary>
public sealed class KnockoutBracket
{
    private readonly List<int> _initialTeamIds;
    private readonly List<MatchFixture> _fixtures = [];
    private CompetitionPhase _currentPhase = CompetitionPhase.Quarterfinals;
    private List<int> _advancingTeams = [];
    private int? _championId;

    public IReadOnlyList<MatchFixture> Fixtures => _fixtures;
    public CompetitionPhase CurrentPhase => _currentPhase;
    public int? ChampionId => _championId;
    public bool IsFinished => _currentPhase == CompetitionPhase.Finished;

    public KnockoutBracket(IReadOnlyList<int> seededTeamIds)
    {
        if (seededTeamIds.Count != 8)
            throw new ArgumentException("Knockout bracket requires exactly 8 teams.");

        _initialTeamIds = [.. seededTeamIds];
    }

    /// <summary>
    /// Restores a bracket from saved state. Used by the persistence layer.
    /// </summary>
    internal static KnockoutBracket Restore(
        IReadOnlyList<int> initialTeamIds,
        List<MatchFixture> fixtures,
        CompetitionPhase currentPhase,
        List<int> advancingTeams,
        int? championId)
    {
        var bracket = new KnockoutBracket(initialTeamIds);
        bracket._fixtures.AddRange(fixtures);
        bracket._currentPhase = currentPhase;
        bracket._advancingTeams = advancingTeams;
        bracket._championId = championId;
        return bracket;
    }

    /// <summary>
    /// Generates fixtures for the current phase. Returns them for scheduling.
    /// </summary>
    public IReadOnlyList<MatchFixture> GenerateNextRound(int startDay)
    {
        if (IsFinished)
            return [];

        var teams = _currentPhase == CompetitionPhase.Quarterfinals
            ? _initialTeamIds
            : _advancingTeams;

        var roundFixtures = GeneratePairings(teams, startDay);
        _fixtures.AddRange(roundFixtures);
        return roundFixtures;
    }

    /// <summary>
    /// Records results and advances the bracket. Call after all fixtures in the round are played.
    /// </summary>
    public void AdvanceRound()
    {
        _advancingTeams = _fixtures
            .Where(f => f.Phase == _currentPhase && f.WinnerClubId.HasValue)
            .Select(f => f.WinnerClubId!.Value)
            .ToList();

        _currentPhase = _currentPhase switch
        {
            CompetitionPhase.Quarterfinals => CompetitionPhase.Semifinals,
            CompetitionPhase.Semifinals => CompetitionPhase.Final,
            CompetitionPhase.Final => CompetitionPhase.Finished,
            _ => _currentPhase
        };

        if (_currentPhase == CompetitionPhase.Finished && _advancingTeams.Count == 1)
        {
            _championId = _advancingTeams[0];
        }
    }

    private List<MatchFixture> GeneratePairings(List<int> teamIds, int startDay)
    {
        var fixtures = new List<MatchFixture>();
        for (int i = 0; i < teamIds.Count; i += 2)
        {
            fixtures.Add(new MatchFixture
            {
                Day = startDay,
                HomeClubId = teamIds[i],
                AwayClubId = teamIds[i + 1],
                Phase = _currentPhase
            });
        }
        return fixtures;
    }
}
