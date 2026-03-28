using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using ElevenLegends.Simulation;

namespace ElevenLegends.Competition;

/// <summary>
/// Orchestrates the entire season: national knockouts per country + mundial.
/// </summary>
public sealed class CompetitionManager
{
    private readonly Dictionary<string, KnockoutBracket> _nationalBrackets;
    private MundialBracket? _mundialBracket;
    private readonly List<Club> _clubs;
    private readonly int _baseSeed;

    public IReadOnlyDictionary<string, KnockoutBracket> NationalBrackets =>
        _nationalBrackets;
    public MundialBracket? MundialBracket => _mundialBracket;

    public CompetitionManager(List<Club> clubs, int baseSeed)
    {
        _clubs = clubs;
        _baseSeed = baseSeed;
        _nationalBrackets = new Dictionary<string, KnockoutBracket>();

        // Group clubs by country and create national brackets
        var byCountry = clubs.GroupBy(c => c.Country);
        foreach (var group in byCountry)
        {
            var teamIds = group.Select(c => c.Id).ToList();
            _nationalBrackets[group.Key] = new KnockoutBracket(teamIds);
        }
    }

    /// <summary>
    /// Restores a competition manager from saved state. Used by the persistence layer.
    /// </summary>
    internal static CompetitionManager Restore(
        List<Club> clubs,
        int baseSeed,
        Dictionary<string, KnockoutBracket> nationalBrackets,
        MundialBracket? mundialBracket)
    {
        var cm = new CompetitionManager(clubs, baseSeed);
        cm._nationalBrackets.Clear();
        foreach (var kvp in nationalBrackets)
            cm._nationalBrackets[kvp.Key] = kvp.Value;
        cm._mundialBracket = mundialBracket;
        return cm;
    }

    /// <summary>
    /// Generates fixtures for the current national round across all countries.
    /// </summary>
    public List<MatchFixture> GenerateNationalRound(int matchDay)
    {
        var allFixtures = new List<MatchFixture>();
        foreach (var bracket in _nationalBrackets.Values)
        {
            if (!bracket.IsFinished)
                allFixtures.AddRange(bracket.GenerateNextRound(matchDay));
        }
        return allFixtures;
    }

    /// <summary>
    /// Simulates all fixtures for a given day and records results.
    /// </summary>
    public void SimulateFixtures(IReadOnlyList<MatchFixture> fixtures, int daySeed)
    {
        for (int i = 0; i < fixtures.Count; i++)
        {
            var fixture = fixtures[i];
            var home = _clubs.First(c => c.Id == fixture.HomeClubId);
            var away = _clubs.First(c => c.Id == fixture.AwayClubId);

            var config = new MatchConfig
            {
                HomeTeam = home.Team,
                AwayTeam = away.Team,
                Seed = daySeed + i
            };

            var result = MatchSimulator.Simulate(config);
            fixture.Result = (result.FinalState.ScoreHome, result.FinalState.ScoreAway);
        }
    }

    /// <summary>
    /// Advances all national brackets after a round is played.
    /// </summary>
    public void AdvanceNationalRounds()
    {
        foreach (var bracket in _nationalBrackets.Values)
        {
            if (!bracket.IsFinished)
                bracket.AdvanceRound();
        }
    }

    /// <summary>
    /// Returns true if all national brackets are finished.
    /// </summary>
    public bool AreNationalsFinished() =>
        _nationalBrackets.Values.All(b => b.IsFinished);

    /// <summary>
    /// Creates the mundial bracket from national champions.
    /// Call only after all nationals are finished.
    /// </summary>
    public void CreateMundial()
    {
        if (!AreNationalsFinished())
            throw new InvalidOperationException("Cannot create mundial before nationals finish.");

        var champions = _nationalBrackets.Values
            .Select(b => b.ChampionId!.Value)
            .ToList();

        _mundialBracket = new MundialBracket(champions);
    }

    /// <summary>
    /// Generates fixtures for the current mundial round.
    /// </summary>
    public List<MatchFixture> GenerateMundialRound(int matchDay)
    {
        if (_mundialBracket == null)
            throw new InvalidOperationException("Mundial not yet created.");

        return [.. _mundialBracket.GenerateNextRound(matchDay)];
    }

    /// <summary>
    /// Advances the mundial bracket after a round.
    /// </summary>
    public void AdvanceMundialRound()
    {
        _mundialBracket?.AdvanceRound();
    }

    /// <summary>
    /// Gets the nacional champion of the country where the given club plays.
    /// </summary>
    public int? GetNationalChampion(string country) =>
        _nationalBrackets.TryGetValue(country, out var bracket)
            ? bracket.ChampionId
            : null;

    /// <summary>
    /// Gets the mundial champion, or null if not yet decided.
    /// </summary>
    public int? GetMundialChampion() => _mundialBracket?.ChampionId;

    /// <summary>
    /// Returns true if the entire season (nationals + mundial) is complete.
    /// </summary>
    public bool IsSeasonComplete() =>
        AreNationalsFinished() && (_mundialBracket?.IsFinished ?? false);
}
