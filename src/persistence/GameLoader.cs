using System.Text.Json;
using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using Microsoft.Data.Sqlite;

namespace ElevenLegends.Persistence;

/// <summary>
/// Loads GameState from a SQLite save file.
/// </summary>
public static class GameLoader
{
    public static GameState Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file not found: {filePath}");

        using var connection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly");
        connection.Open();

        int version = DatabaseSchema.GetSchemaVersion(connection);
        if (version != DatabaseSchema.SchemaVersion)
            throw new InvalidOperationException(
                $"Incompatible save version {version}, expected {DatabaseSchema.SchemaVersion}.");

        // Load all data
        var (baseSeed, dayIndex, natCount, munCount, salDays, nextPid, tDayCount) =
            LoadGameStateScalars(connection);
        var manager = LoadManager(connection);
        var clubs = LoadClubs(connection);
        var transferHistory = LoadTransferHistory(connection);
        var activeLoans = LoadActiveLoans(connection);
        var calendar = LoadCalendar(connection);

        // Restore competition from brackets
        var competition = LoadCompetition(connection, clubs, baseSeed);

        // Build GameState
        var gameState = GameState.Restore(
            clubs, manager, baseSeed, competition, calendar,
            dayIndex, natCount, munCount, salDays, nextPid, tDayCount,
            transferHistory, activeLoans);

        gameState.ReplaceCompetition(competition);
        gameState.ReplaceCalendar(calendar);

        return gameState;
    }

    private static (int baseSeed, int dayIndex, int natCount, int munCount,
        int salDays, int nextPid, int tDayCount) LoadGameStateScalars(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM game_state WHERE id = 1";
        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            throw new InvalidOperationException("No game_state row found.");

        return (
            reader.GetInt32(reader.GetOrdinal("base_seed")),
            reader.GetInt32(reader.GetOrdinal("current_day_index")),
            reader.GetInt32(reader.GetOrdinal("national_match_day_count")),
            reader.GetInt32(reader.GetOrdinal("mundial_match_day_count")),
            reader.GetInt32(reader.GetOrdinal("days_since_salary")),
            reader.GetInt32(reader.GetOrdinal("next_player_id")),
            reader.GetInt32(reader.GetOrdinal("transfer_day_count"))
        );
    }

    private static ManagerState LoadManager(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM manager WHERE id = 1";
        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            throw new InvalidOperationException("No manager row found.");

        return new ManagerState
        {
            Name = reader.GetString(reader.GetOrdinal("name")),
            Status = Enum.Parse<ManagerStatus>(reader.GetString(reader.GetOrdinal("status"))),
            ClubId = reader.GetInt32(reader.GetOrdinal("club_id")),
            Reputation = reader.GetInt32(reader.GetOrdinal("reputation")),
            PersonalBalance = (decimal)reader.GetDouble(reader.GetOrdinal("personal_balance")),
            Salary = (decimal)reader.GetDouble(reader.GetOrdinal("salary"))
        };
    }

    private static List<Club> LoadClubs(SqliteConnection conn)
    {
        var clubs = new List<Club>();

        // Load all clubs
        using var clubCmd = conn.CreateCommand();
        clubCmd.CommandText = "SELECT * FROM clubs ORDER BY id";
        using var clubReader = clubCmd.ExecuteReader();

        var clubData = new List<(int id, string name, string country, decimal balance,
            int reputation, int teamId, string teamName)>();

        while (clubReader.Read())
        {
            clubData.Add((
                clubReader.GetInt32(clubReader.GetOrdinal("id")),
                clubReader.GetString(clubReader.GetOrdinal("name")),
                clubReader.GetString(clubReader.GetOrdinal("country")),
                (decimal)clubReader.GetDouble(clubReader.GetOrdinal("balance")),
                clubReader.GetInt32(clubReader.GetOrdinal("reputation")),
                clubReader.GetInt32(clubReader.GetOrdinal("team_id")),
                clubReader.GetString(clubReader.GetOrdinal("team_name"))
            ));
        }

        // Load all players grouped by club
        var playersByClub = LoadAllPlayers(conn);

        // Load all starting lineups
        var lineupsByClub = LoadAllLineups(conn);

        foreach (var cd in clubData)
        {
            var players = playersByClub.GetValueOrDefault(cd.id, []);
            var lineup = lineupsByClub.GetValueOrDefault(cd.id, []);

            clubs.Add(new Club
            {
                Id = cd.id,
                Name = cd.name,
                Country = cd.country,
                Balance = cd.balance,
                Reputation = cd.reputation,
                Team = new Team
                {
                    Id = cd.teamId,
                    Name = cd.teamName,
                    Players = players,
                    StartingLineup = lineup
                }
            });
        }

        return clubs;
    }

    private static Dictionary<int, List<Player>> LoadAllPlayers(SqliteConnection conn)
    {
        var result = new Dictionary<int, List<Player>>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM players ORDER BY club_id, id";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            int clubId = r.GetInt32(r.GetOrdinal("club_id"));
            string? secPosStr = r.IsDBNull(r.GetOrdinal("secondary_position"))
                ? null : r.GetString(r.GetOrdinal("secondary_position"));

            var traitsJson = r.GetString(r.GetOrdinal("traits_json"));
            var traits = JsonSerializer.Deserialize<List<string>>(traitsJson) ?? [];

            var player = new Player
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                Name = r.GetString(r.GetOrdinal("name")),
                PrimaryPosition = Enum.Parse<Position>(r.GetString(r.GetOrdinal("primary_position"))),
                SecondaryPosition = secPosStr != null ? Enum.Parse<Position>(secPosStr) : null,
                Age = r.GetInt32(r.GetOrdinal("age")),
                Morale = r.GetInt32(r.GetOrdinal("morale")),
                Chemistry = r.GetInt32(r.GetOrdinal("chemistry")),
                Traits = traits,
                Attributes = new PlayerAttributes
                {
                    Finishing = r.GetInt32(r.GetOrdinal("finishing")),
                    Passing = r.GetInt32(r.GetOrdinal("passing")),
                    Dribbling = r.GetInt32(r.GetOrdinal("dribbling")),
                    FirstTouch = r.GetInt32(r.GetOrdinal("first_touch")),
                    Technique = r.GetInt32(r.GetOrdinal("technique")),
                    Decisions = r.GetInt32(r.GetOrdinal("decisions")),
                    Composure = r.GetInt32(r.GetOrdinal("composure")),
                    Positioning = r.GetInt32(r.GetOrdinal("positioning")),
                    Anticipation = r.GetInt32(r.GetOrdinal("anticipation")),
                    OffTheBall = r.GetInt32(r.GetOrdinal("off_the_ball")),
                    Speed = r.GetInt32(r.GetOrdinal("speed")),
                    Acceleration = r.GetInt32(r.GetOrdinal("acceleration")),
                    Stamina = r.GetInt32(r.GetOrdinal("stamina")),
                    Strength = r.GetInt32(r.GetOrdinal("strength")),
                    Agility = r.GetInt32(r.GetOrdinal("agility")),
                    Consistency = r.GetInt32(r.GetOrdinal("consistency")),
                    Leadership = r.GetInt32(r.GetOrdinal("leadership")),
                    Flair = r.GetInt32(r.GetOrdinal("flair")),
                    BigMatches = r.GetInt32(r.GetOrdinal("big_matches")),
                    Reflexes = r.GetInt32(r.GetOrdinal("reflexes")),
                    Handling = r.GetInt32(r.GetOrdinal("handling")),
                    GkPositioning = r.GetInt32(r.GetOrdinal("gk_positioning")),
                    Aerial = r.GetInt32(r.GetOrdinal("aerial"))
                }
            };

            if (!result.ContainsKey(clubId))
                result[clubId] = [];
            result[clubId].Add(player);
        }

        return result;
    }

    private static Dictionary<int, List<int>> LoadAllLineups(SqliteConnection conn)
    {
        var result = new Dictionary<int, List<int>>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM starting_lineups ORDER BY club_id, lineup_order";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            int clubId = r.GetInt32(r.GetOrdinal("club_id"));
            int playerId = r.GetInt32(r.GetOrdinal("player_id"));

            if (!result.ContainsKey(clubId))
                result[clubId] = [];
            result[clubId].Add(playerId);
        }

        return result;
    }

    private static List<TransferRecord> LoadTransferHistory(SqliteConnection conn)
    {
        var records = new List<TransferRecord>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM transfer_history ORDER BY id";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            records.Add(new TransferRecord
            {
                Type = Enum.Parse<TransferType>(r.GetString(r.GetOrdinal("type"))),
                PlayerId = r.GetInt32(r.GetOrdinal("player_id")),
                PlayerName = r.GetString(r.GetOrdinal("player_name")),
                FromClubId = r.IsDBNull(r.GetOrdinal("from_club_id"))
                    ? null : r.GetInt32(r.GetOrdinal("from_club_id")),
                ToClubId = r.IsDBNull(r.GetOrdinal("to_club_id"))
                    ? null : r.GetInt32(r.GetOrdinal("to_club_id")),
                Fee = (decimal)r.GetDouble(r.GetOrdinal("fee")),
                Day = r.GetInt32(r.GetOrdinal("day"))
            });
        }

        return records;
    }

    private static List<LoanRecord> LoadActiveLoans(SqliteConnection conn)
    {
        var loans = new List<LoanRecord>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM active_loans";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            loans.Add(new LoanRecord
            {
                PlayerId = r.GetInt32(r.GetOrdinal("player_id")),
                PlayerName = r.GetString(r.GetOrdinal("player_name")),
                OriginClubId = r.GetInt32(r.GetOrdinal("origin_club_id")),
                HostClubId = r.GetInt32(r.GetOrdinal("host_club_id"))
            });
        }

        return loans;
    }

    private static List<SeasonDay> LoadCalendar(SqliteConnection conn)
    {
        var calendar = new List<SeasonDay>();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM season_calendar ORDER BY day_index";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            var fixturesJson = r.GetString(r.GetOrdinal("fixtures_json"));
            var fixtureDtos = JsonSerializer.Deserialize<List<FixtureDto>>(fixturesJson) ?? [];

            var fixtures = fixtureDtos.Select(dto =>
            {
                var fixture = new MatchFixture
                {
                    Day = dto.Day,
                    HomeClubId = dto.HomeClubId,
                    AwayClubId = dto.AwayClubId,
                    Phase = Enum.Parse<CompetitionPhase>(dto.Phase)
                };
                if (dto.ResultHome.HasValue && dto.ResultAway.HasValue)
                    fixture.Result = (dto.ResultHome.Value, dto.ResultAway.Value);
                return fixture;
            }).ToList();

            calendar.Add(new SeasonDay
            {
                Day = r.GetInt32(r.GetOrdinal("day_number")),
                Type = Enum.Parse<DayType>(r.GetString(r.GetOrdinal("day_type"))),
                Fixtures = fixtures
            });
        }

        return calendar;
    }

    private static CompetitionManager LoadCompetition(SqliteConnection conn,
        List<Club> clubs, int baseSeed)
    {
        var nationalBrackets = new Dictionary<string, KnockoutBracket>();
        MundialBracket? mundialBracket = null;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM brackets";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            string bracketType = r.GetString(r.GetOrdinal("bracket_type"));
            string? country = r.IsDBNull(r.GetOrdinal("country"))
                ? null : r.GetString(r.GetOrdinal("country"));
            var currentPhase = Enum.Parse<CompetitionPhase>(
                r.GetString(r.GetOrdinal("current_phase")));
            int? championId = r.IsDBNull(r.GetOrdinal("champion_id"))
                ? null : r.GetInt32(r.GetOrdinal("champion_id"));

            var initialIds = JsonSerializer.Deserialize<List<int>>(
                r.GetString(r.GetOrdinal("initial_team_ids_json"))) ?? [];
            var advancingIds = JsonSerializer.Deserialize<List<int>>(
                r.GetString(r.GetOrdinal("advancing_team_ids_json"))) ?? [];
            var fixtureDtos = JsonSerializer.Deserialize<List<FixtureDto>>(
                r.GetString(r.GetOrdinal("fixtures_json"))) ?? [];

            var fixtures = fixtureDtos.Select(dto =>
            {
                var f = new MatchFixture
                {
                    Day = dto.Day,
                    HomeClubId = dto.HomeClubId,
                    AwayClubId = dto.AwayClubId,
                    Phase = Enum.Parse<CompetitionPhase>(dto.Phase)
                };
                if (dto.ResultHome.HasValue && dto.ResultAway.HasValue)
                    f.Result = (dto.ResultHome.Value, dto.ResultAway.Value);
                return f;
            }).ToList();

            if (bracketType == "nacional" || bracketType == "national")
            {
                if (country != null && initialIds.Count == 8)
                {
                    nationalBrackets[country] = KnockoutBracket.Restore(
                        initialIds, fixtures, currentPhase, advancingIds, championId);
                }
            }
            else if (bracketType == "mundial")
            {
                if (initialIds.Count == 4)
                {
                    mundialBracket = MundialBracket.Restore(
                        initialIds, fixtures, currentPhase, advancingIds, championId);
                }
            }
        }

        // If no brackets were loaded (e.g., save before any matches), fall back to initial state
        if (nationalBrackets.Count == 0)
        {
            return new CompetitionManager(clubs, baseSeed);
        }

        return CompetitionManager.Restore(clubs, baseSeed, nationalBrackets, mundialBracket);
    }
}
