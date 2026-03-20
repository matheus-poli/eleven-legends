using Microsoft.Data.Sqlite;

namespace ElevenLegends.Persistence;

/// <summary>
/// Defines and creates the SQLite schema for save files.
/// </summary>
public static class DatabaseSchema
{
    public const int SchemaVersion = 1;

    public static void CreateTables(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            -- Save file metadata
            CREATE TABLE IF NOT EXISTS save_meta (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            -- Core game state (single-row table)
            CREATE TABLE IF NOT EXISTS game_state (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                base_seed INTEGER NOT NULL,
                current_day_index INTEGER NOT NULL,
                national_match_day_count INTEGER NOT NULL,
                mundial_match_day_count INTEGER NOT NULL,
                days_since_salary INTEGER NOT NULL,
                next_player_id INTEGER NOT NULL,
                transfer_day_count INTEGER NOT NULL
            );

            -- Manager career state (single-row table)
            CREATE TABLE IF NOT EXISTS manager (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                name TEXT NOT NULL,
                status TEXT NOT NULL,
                club_id INTEGER NOT NULL,
                reputation INTEGER NOT NULL,
                personal_balance REAL NOT NULL,
                salary REAL NOT NULL
            );

            -- Clubs
            CREATE TABLE IF NOT EXISTS clubs (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                country TEXT NOT NULL,
                balance REAL NOT NULL,
                reputation INTEGER NOT NULL,
                team_id INTEGER NOT NULL,
                team_name TEXT NOT NULL
            );

            -- Players (attributes inline to avoid joins)
            CREATE TABLE IF NOT EXISTS players (
                id INTEGER PRIMARY KEY,
                club_id INTEGER NOT NULL REFERENCES clubs(id),
                name TEXT NOT NULL,
                primary_position TEXT NOT NULL,
                secondary_position TEXT,
                age INTEGER NOT NULL,
                morale INTEGER NOT NULL,
                chemistry INTEGER NOT NULL,
                traits_json TEXT NOT NULL DEFAULT '[]',
                -- Technical
                finishing INTEGER NOT NULL,
                passing INTEGER NOT NULL,
                dribbling INTEGER NOT NULL,
                first_touch INTEGER NOT NULL,
                technique INTEGER NOT NULL,
                -- Mental
                decisions INTEGER NOT NULL,
                composure INTEGER NOT NULL,
                positioning INTEGER NOT NULL,
                anticipation INTEGER NOT NULL,
                off_the_ball INTEGER NOT NULL,
                -- Physical
                speed INTEGER NOT NULL,
                acceleration INTEGER NOT NULL,
                stamina INTEGER NOT NULL,
                strength INTEGER NOT NULL,
                agility INTEGER NOT NULL,
                -- Special
                consistency INTEGER NOT NULL,
                leadership INTEGER NOT NULL,
                flair INTEGER NOT NULL,
                big_matches INTEGER NOT NULL,
                -- Goalkeeper
                reflexes INTEGER NOT NULL,
                handling INTEGER NOT NULL,
                gk_positioning INTEGER NOT NULL,
                aerial INTEGER NOT NULL
            );

            -- Starting lineup (11 players per team, ordered)
            CREATE TABLE IF NOT EXISTS starting_lineups (
                club_id INTEGER NOT NULL REFERENCES clubs(id),
                player_id INTEGER NOT NULL REFERENCES players(id),
                lineup_order INTEGER NOT NULL,
                PRIMARY KEY (club_id, player_id)
            );

            -- Transfer history
            CREATE TABLE IF NOT EXISTS transfer_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                type TEXT NOT NULL,
                player_id INTEGER NOT NULL,
                player_name TEXT NOT NULL,
                from_club_id INTEGER,
                to_club_id INTEGER,
                fee REAL NOT NULL,
                day INTEGER NOT NULL
            );

            -- Active loans
            CREATE TABLE IF NOT EXISTS active_loans (
                player_id INTEGER PRIMARY KEY,
                player_name TEXT NOT NULL,
                origin_club_id INTEGER NOT NULL,
                host_club_id INTEGER NOT NULL
            );

            -- Competition brackets (JSON for complex nested state)
            CREATE TABLE IF NOT EXISTS brackets (
                bracket_key TEXT PRIMARY KEY,
                bracket_type TEXT NOT NULL,
                country TEXT,
                current_phase TEXT NOT NULL,
                champion_id INTEGER,
                initial_team_ids_json TEXT NOT NULL,
                advancing_team_ids_json TEXT NOT NULL,
                fixtures_json TEXT NOT NULL
            );

            -- Season calendar
            CREATE TABLE IF NOT EXISTS season_calendar (
                day_index INTEGER PRIMARY KEY,
                day_number INTEGER NOT NULL,
                day_type TEXT NOT NULL,
                fixtures_json TEXT NOT NULL DEFAULT '[]'
            );
            """;
        cmd.ExecuteNonQuery();

        // Insert schema version
        using var metaCmd = connection.CreateCommand();
        metaCmd.CommandText = """
            INSERT OR REPLACE INTO save_meta (key, value) VALUES ('schema_version', @version);
            """;
        metaCmd.Parameters.AddWithValue("@version", SchemaVersion.ToString());
        metaCmd.ExecuteNonQuery();
    }

    public static int GetSchemaVersion(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM save_meta WHERE key = 'schema_version'";
        var result = cmd.ExecuteScalar();
        return result != null ? int.Parse((string)result) : 0;
    }
}
