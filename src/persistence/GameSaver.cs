using System.Text.Json;
using ElevenLegends.Competition;
using ElevenLegends.Data.Enums;
using ElevenLegends.Data.Models;
using Microsoft.Data.Sqlite;

namespace ElevenLegends.Persistence;

/// <summary>
/// Saves GameState to a SQLite database file.
/// </summary>
public static class GameSaver
{
    public static void Save(GameState gameState, string filePath)
    {
        // Delete existing file for clean save
        if (File.Exists(filePath))
            File.Delete(filePath);

        using var connection = new SqliteConnection($"Data Source={filePath}");
        connection.Open();

        DatabaseSchema.CreateTables(connection);

        using var transaction = connection.BeginTransaction();
        try
        {
            SaveMeta(connection, gameState);
            SaveGameStateScalars(connection, gameState);
            SaveManager(connection, gameState.Manager);
            SaveClubs(connection, gameState.Clubs);
            SavePlayers(connection, gameState.Clubs);
            SaveStartingLineups(connection, gameState.Clubs);
            SaveTransferHistory(connection, gameState.TransferHistory);
            SaveActiveLoans(connection, gameState.ActiveLoans);
            SaveBrackets(connection, gameState.Competition);
            SaveCalendar(connection, gameState.Calendar);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void SaveMeta(SqliteConnection conn, GameState gs)
    {
        ExecuteNonQuery(conn, """
            INSERT OR REPLACE INTO save_meta (key, value) VALUES
                ('save_timestamp', @ts),
                ('save_version', '1');
            """,
            ("@ts", DateTime.UtcNow.ToString("o")));
    }

    private static void SaveGameStateScalars(SqliteConnection conn, GameState gs)
    {
        ExecuteNonQuery(conn, """
            INSERT INTO game_state (id, base_seed, current_day_index,
                national_match_day_count, mundial_match_day_count,
                days_since_salary, next_player_id, transfer_day_count)
            VALUES (1, @seed, @dayIdx, @natCount, @munCount, @salDays, @nextPid, @tDayCount);
            """,
            ("@seed", gs.BaseSeed),
            ("@dayIdx", gs.CurrentDayIndex),
            ("@natCount", gs.NationalMatchDayCount),
            ("@munCount", gs.MundialMatchDayCount),
            ("@salDays", gs.DaysSinceSalary),
            ("@nextPid", gs.NextPlayerId),
            ("@tDayCount", gs.TransferDayCount));
    }

    private static void SaveManager(SqliteConnection conn, ManagerState mgr)
    {
        ExecuteNonQuery(conn, """
            INSERT INTO manager (id, name, status, club_id, reputation, personal_balance, salary)
            VALUES (1, @name, @status, @clubId, @rep, @balance, @salary);
            """,
            ("@name", mgr.Name),
            ("@status", mgr.Status.ToString()),
            ("@clubId", mgr.ClubId),
            ("@rep", mgr.Reputation),
            ("@balance", (double)mgr.PersonalBalance),
            ("@salary", (double)mgr.Salary));
    }

    private static void SaveClubs(SqliteConnection conn, IReadOnlyList<Club> clubs)
    {
        foreach (var club in clubs)
        {
            ExecuteNonQuery(conn, """
                INSERT INTO clubs (id, name, country, balance, reputation, team_id, team_name)
                VALUES (@id, @name, @country, @balance, @rep, @teamId, @teamName);
                """,
                ("@id", club.Id),
                ("@name", club.Name),
                ("@country", club.Country),
                ("@balance", (double)club.Balance),
                ("@rep", club.Reputation),
                ("@teamId", club.Team.Id),
                ("@teamName", club.Team.Name));
        }
    }

    private static void SavePlayers(SqliteConnection conn, IReadOnlyList<Club> clubs)
    {
        foreach (var club in clubs)
        {
            foreach (var p in club.Team.Players)
            {
                var a = p.Attributes;
                ExecuteNonQuery(conn, """
                    INSERT INTO players (id, club_id, name, primary_position, secondary_position,
                        age, morale, chemistry, traits_json,
                        finishing, passing, dribbling, first_touch, technique,
                        decisions, composure, positioning, anticipation, off_the_ball,
                        speed, acceleration, stamina, strength, agility,
                        consistency, leadership, flair, big_matches,
                        reflexes, handling, gk_positioning, aerial)
                    VALUES (@id, @clubId, @name, @pos, @secPos,
                        @age, @morale, @chem, @traits,
                        @fin, @pas, @dri, @ft, @tec,
                        @dec, @com, @positioning, @ant, @otb,
                        @spd, @acc, @sta, @str, @agi,
                        @con, @lea, @fla, @big,
                        @ref, @han, @gkp, @aer);
                    """,
                    ("@id", p.Id), ("@clubId", club.Id),
                    ("@name", p.Name),
                    ("@pos", p.PrimaryPosition.ToString()),
                    ("@secPos", p.SecondaryPosition?.ToString() ?? (object)DBNull.Value),
                    ("@age", p.Age), ("@morale", p.Morale), ("@chem", p.Chemistry),
                    ("@traits", JsonSerializer.Serialize(p.Traits)),
                    ("@fin", a.Finishing), ("@pas", a.Passing), ("@dri", a.Dribbling),
                    ("@ft", a.FirstTouch), ("@tec", a.Technique),
                    ("@dec", a.Decisions), ("@com", a.Composure), ("@positioning", a.Positioning),
                    ("@ant", a.Anticipation), ("@otb", a.OffTheBall),
                    ("@spd", a.Speed), ("@acc", a.Acceleration), ("@sta", a.Stamina),
                    ("@str", a.Strength), ("@agi", a.Agility),
                    ("@con", a.Consistency), ("@lea", a.Leadership), ("@fla", a.Flair),
                    ("@big", a.BigMatches),
                    ("@ref", a.Reflexes), ("@han", a.Handling), ("@gkp", a.GkPositioning),
                    ("@aer", a.Aerial));
            }
        }
    }

    private static void SaveStartingLineups(SqliteConnection conn, IReadOnlyList<Club> clubs)
    {
        foreach (var club in clubs)
        {
            for (int i = 0; i < club.Team.StartingLineup.Count; i++)
            {
                ExecuteNonQuery(conn, """
                    INSERT INTO starting_lineups (club_id, player_id, lineup_order)
                    VALUES (@clubId, @playerId, @order);
                    """,
                    ("@clubId", club.Id),
                    ("@playerId", club.Team.StartingLineup[i]),
                    ("@order", i));
            }
        }
    }

    private static void SaveTransferHistory(SqliteConnection conn, List<TransferRecord> history)
    {
        foreach (var t in history)
        {
            ExecuteNonQuery(conn, """
                INSERT INTO transfer_history (type, player_id, player_name, from_club_id, to_club_id, fee, day)
                VALUES (@type, @pid, @pname, @from, @to, @fee, @day);
                """,
                ("@type", t.Type.ToString()),
                ("@pid", t.PlayerId),
                ("@pname", t.PlayerName),
                ("@from", t.FromClubId.HasValue ? t.FromClubId.Value : DBNull.Value),
                ("@to", t.ToClubId.HasValue ? t.ToClubId.Value : DBNull.Value),
                ("@fee", (double)t.Fee),
                ("@day", t.Day));
        }
    }

    private static void SaveActiveLoans(SqliteConnection conn, List<LoanRecord> loans)
    {
        foreach (var loan in loans)
        {
            ExecuteNonQuery(conn, """
                INSERT INTO active_loans (player_id, player_name, origin_club_id, host_club_id)
                VALUES (@pid, @pname, @origin, @host);
                """,
                ("@pid", loan.PlayerId),
                ("@pname", loan.PlayerName),
                ("@origin", loan.OriginClubId),
                ("@host", loan.HostClubId));
        }
    }

    private static void SaveBrackets(SqliteConnection conn, CompetitionManager competition)
    {
        foreach (var kvp in competition.NationalBrackets)
        {
            SaveBracket(conn, $"national:{kvp.Key}", "national", kvp.Key, kvp.Value);
        }

        if (competition.MundialBracket != null)
        {
            SaveMundialBracket(conn, competition.MundialBracket);
        }
    }

    private static void SaveBracket(SqliteConnection conn, string key, string type,
        string? country, KnockoutBracket bracket)
    {
        var fixturesJson = JsonSerializer.Serialize(bracket.Fixtures.Select(f => new FixtureDto
        {
            Day = f.Day,
            HomeClubId = f.HomeClubId,
            AwayClubId = f.AwayClubId,
            Phase = f.Phase.ToString(),
            ResultHome = f.Result?.Home,
            ResultAway = f.Result?.Away
        }).ToList());

        // Get initial team IDs and advancing teams via bracket state
        var initialIds = GetInitialTeamIds(bracket);
        var advancingIds = GetAdvancingTeamIds(bracket);

        ExecuteNonQuery(conn, """
            INSERT INTO brackets (bracket_key, bracket_type, country, current_phase,
                champion_id, initial_team_ids_json, advancing_team_ids_json, fixtures_json)
            VALUES (@key, @type, @country, @phase, @champion, @initIds, @advIds, @fixtures);
            """,
            ("@key", key),
            ("@type", type),
            ("@country", country ?? (object)DBNull.Value),
            ("@phase", bracket.CurrentPhase.ToString()),
            ("@champion", bracket.ChampionId.HasValue ? bracket.ChampionId.Value : DBNull.Value),
            ("@initIds", JsonSerializer.Serialize(initialIds)),
            ("@advIds", JsonSerializer.Serialize(advancingIds)),
            ("@fixtures", fixturesJson));
    }

    private static void SaveMundialBracket(SqliteConnection conn, MundialBracket bracket)
    {
        var fixturesJson = JsonSerializer.Serialize(bracket.Fixtures.Select(f => new FixtureDto
        {
            Day = f.Day,
            HomeClubId = f.HomeClubId,
            AwayClubId = f.AwayClubId,
            Phase = f.Phase.ToString(),
            ResultHome = f.Result?.Home,
            ResultAway = f.Result?.Away
        }).ToList());

        var initialIds = GetMundialInitialTeamIds(bracket);
        var advancingIds = GetMundialAdvancingTeamIds(bracket);

        ExecuteNonQuery(conn, """
            INSERT INTO brackets (bracket_key, bracket_type, country, current_phase,
                champion_id, initial_team_ids_json, advancing_team_ids_json, fixtures_json)
            VALUES ('mundial', 'mundial', NULL, @phase, @champion, @initIds, @advIds, @fixtures);
            """,
            ("@phase", bracket.CurrentPhase.ToString()),
            ("@champion", bracket.ChampionId.HasValue ? bracket.ChampionId.Value : DBNull.Value),
            ("@initIds", JsonSerializer.Serialize(initialIds)),
            ("@advIds", JsonSerializer.Serialize(advancingIds)),
            ("@fixtures", fixturesJson));
    }

    private static void SaveCalendar(SqliteConnection conn, IReadOnlyList<SeasonDay> calendar)
    {
        for (int i = 0; i < calendar.Count; i++)
        {
            var day = calendar[i];
            var fixturesJson = JsonSerializer.Serialize(day.Fixtures.Select(f => new FixtureDto
            {
                Day = f.Day,
                HomeClubId = f.HomeClubId,
                AwayClubId = f.AwayClubId,
                Phase = f.Phase.ToString(),
                ResultHome = f.Result?.Home,
                ResultAway = f.Result?.Away
            }).ToList());

            ExecuteNonQuery(conn, """
                INSERT INTO season_calendar (day_index, day_number, day_type, fixtures_json)
                VALUES (@idx, @num, @type, @fixtures);
                """,
                ("@idx", i),
                ("@num", day.Day),
                ("@type", day.Type.ToString()),
                ("@fixtures", fixturesJson));
        }
    }

    #region Bracket State Extraction

    // We need to extract initial/advancing team IDs from brackets.
    // Since these are private fields, we derive them from fixtures.

    private static List<int> GetInitialTeamIds(KnockoutBracket bracket)
    {
        // Initial teams are the home+away of the quarterfinal fixtures
        var qfFixtures = bracket.Fixtures
            .Where(f => f.Phase == CompetitionPhase.Quarterfinals)
            .ToList();

        if (qfFixtures.Count > 0)
        {
            return qfFixtures.SelectMany(f => new[] { f.HomeClubId, f.AwayClubId }).ToList();
        }

        // Bracket hasn't started yet — no fixtures means we need the IDs from
        // somewhere else. They'll be regenerated from clubs on load.
        return [];
    }

    private static List<int> GetAdvancingTeamIds(KnockoutBracket bracket)
    {
        // Advancing teams are the winners of the previous round
        var prevPhase = bracket.CurrentPhase switch
        {
            CompetitionPhase.Semifinals => CompetitionPhase.Quarterfinals,
            CompetitionPhase.Final => CompetitionPhase.Semifinals,
            CompetitionPhase.Finished => CompetitionPhase.Final,
            _ => (CompetitionPhase?)null
        };

        if (prevPhase == null) return [];

        return bracket.Fixtures
            .Where(f => f.Phase == prevPhase && f.WinnerClubId.HasValue)
            .Select(f => f.WinnerClubId!.Value)
            .ToList();
    }

    private static List<int> GetMundialInitialTeamIds(MundialBracket bracket)
    {
        var sfFixtures = bracket.Fixtures
            .Where(f => f.Phase == CompetitionPhase.MundialSemifinals)
            .ToList();

        return sfFixtures.Count > 0
            ? sfFixtures.SelectMany(f => new[] { f.HomeClubId, f.AwayClubId }).ToList()
            : [];
    }

    private static List<int> GetMundialAdvancingTeamIds(MundialBracket bracket)
    {
        var prevPhase = bracket.CurrentPhase switch
        {
            CompetitionPhase.MundialFinal => CompetitionPhase.MundialSemifinals,
            CompetitionPhase.Finished => CompetitionPhase.MundialFinal,
            _ => (CompetitionPhase?)null
        };

        if (prevPhase == null) return [];

        return bracket.Fixtures
            .Where(f => f.Phase == prevPhase && f.WinnerClubId.HasValue)
            .Select(f => f.WinnerClubId!.Value)
            .ToList();
    }

    #endregion

    #region Helpers

    private static void ExecuteNonQuery(SqliteConnection conn, string sql,
        params (string name, object value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        cmd.ExecuteNonQuery();
    }

    #endregion
}

/// <summary>
/// DTO for serializing match fixtures to JSON.
/// </summary>
internal sealed class FixtureDto
{
    public int Day { get; set; }
    public int HomeClubId { get; set; }
    public int AwayClubId { get; set; }
    public string Phase { get; set; } = "";
    public int? ResultHome { get; set; }
    public int? ResultAway { get; set; }
}
