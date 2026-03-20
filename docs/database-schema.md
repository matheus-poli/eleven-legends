# Database Schema — Save/Load System

Schema version: **1**  
Package: `Microsoft.Data.Sqlite 10.0.5`  
Each save slot is a separate `.db` file in the `saves/` directory.

---

## Tables

### `save_meta`
Key-value metadata about the save file.

| Column | Type | Description |
|--------|------|-------------|
| `key` | TEXT PK | Metadata key |
| `value` | TEXT | Metadata value |

Keys: `schema_version`, `save_timestamp`, `save_version`

---

### `game_state`
Single-row table with core game progression scalars.

| Column | Type | Description |
|--------|------|-------------|
| `id` | INTEGER PK | Always 1 |
| `base_seed` | INTEGER | Deterministic RNG seed for the season |
| `current_day_index` | INTEGER | Current position in calendar (0-based) |
| `national_match_day_count` | INTEGER | National rounds played |
| `mundial_match_day_count` | INTEGER | Mundial rounds played |
| `days_since_salary` | INTEGER | Days since last salary cycle (0-7) |
| `next_player_id` | INTEGER | Next available player ID |
| `transfer_day_count` | INTEGER | Transfer window days processed |

---

### `manager`
Single-row table with manager career state.

| Column | Type | Description |
|--------|------|-------------|
| `id` | INTEGER PK | Always 1 |
| `name` | TEXT | Manager name |
| `status` | TEXT | Enum: Employed, Dismissed, Winner, GameOver |
| `club_id` | INTEGER | FK → clubs.id |
| `reputation` | INTEGER | 0–100 |
| `personal_balance` | REAL | Manager's personal money |
| `salary` | REAL | Per-cycle salary |

---

### `clubs`
All clubs in the game world.

| Column | Type | Description |
|--------|------|-------------|
| `id` | INTEGER PK | Club ID |
| `name` | TEXT | Club name |
| `country` | TEXT | Country name |
| `balance` | REAL | Club's financial balance |
| `reputation` | INTEGER | 0–100 |
| `team_id` | INTEGER | Team ID (1:1 with club) |
| `team_name` | TEXT | Team display name |

---

### `players`
All players with attributes inline (no separate attributes table).

| Column | Type | Description |
|--------|------|-------------|
| `id` | INTEGER PK | Player ID (globally unique) |
| `club_id` | INTEGER | FK → clubs.id |
| `name` | TEXT | Player name |
| `primary_position` | TEXT | Enum: GK, CB, LB, RB, etc. |
| `secondary_position` | TEXT | Nullable. Same enum. |
| `age` | INTEGER | Player age |
| `morale` | INTEGER | 0–100 |
| `chemistry` | INTEGER | 0–100 |
| `traits_json` | TEXT | JSON array of trait strings |
| **Technical** | | |
| `finishing` | INTEGER | 0–100 |
| `passing` | INTEGER | 0–100 |
| `dribbling` | INTEGER | 0–100 |
| `first_touch` | INTEGER | 0–100 |
| `technique` | INTEGER | 0–100 |
| **Mental** | | |
| `decisions` | INTEGER | 0–100 |
| `composure` | INTEGER | 0–100 |
| `positioning` | INTEGER | 0–100 |
| `anticipation` | INTEGER | 0–100 |
| `off_the_ball` | INTEGER | 0–100 |
| **Physical** | | |
| `speed` | INTEGER | 0–100 |
| `acceleration` | INTEGER | 0–100 |
| `stamina` | INTEGER | 0–100 |
| `strength` | INTEGER | 0–100 |
| `agility` | INTEGER | 0–100 |
| **Special** | | |
| `consistency` | INTEGER | 0–100 |
| `leadership` | INTEGER | 0–100 |
| `flair` | INTEGER | 0–100 |
| `big_matches` | INTEGER | 0–100 |
| **Goalkeeper** | | |
| `reflexes` | INTEGER | 0–100 |
| `handling` | INTEGER | 0–100 |
| `gk_positioning` | INTEGER | 0–100 |
| `aerial` | INTEGER | 0–100 |

---

### `starting_lineups`
Ordered list of 11 starting player IDs per club.

| Column | Type | Description |
|--------|------|-------------|
| `club_id` | INTEGER | FK → clubs.id |
| `player_id` | INTEGER | FK → players.id |
| `lineup_order` | INTEGER | Position in lineup (0-10) |

PK: `(club_id, player_id)`

---

### `transfer_history`
All completed transfers during the season.

| Column | Type | Description |
|--------|------|-------------|
| `id` | INTEGER PK | Auto-increment |
| `type` | TEXT | Enum: Buy, Sell, LoanIn, LoanOut, YouthRecruit, ScoutRecruit |
| `player_id` | INTEGER | Transferred player ID |
| `player_name` | TEXT | Player name at time of transfer |
| `from_club_id` | INTEGER | Nullable. Source club ID |
| `to_club_id` | INTEGER | Nullable. Destination club ID |
| `fee` | REAL | Transfer fee |
| `day` | INTEGER | Season day of transfer |

---

### `active_loans`
Currently active loan agreements.

| Column | Type | Description |
|--------|------|-------------|
| `player_id` | INTEGER PK | Loaned player ID |
| `player_name` | TEXT | Player name |
| `origin_club_id` | INTEGER | Club that owns the player |
| `host_club_id` | INTEGER | Club borrowing the player |

---

### `brackets`
Competition bracket state (national knockouts + mundial). Uses JSON for fixture data.

| Column | Type | Description |
|--------|------|-------------|
| `bracket_key` | TEXT PK | e.g., `national:Brasilândia` or `mundial` |
| `bracket_type` | TEXT | `national` or `mundial` |
| `country` | TEXT | Nullable. Country name for nationals |
| `current_phase` | TEXT | Enum: Quarterfinals, Semifinals, Final, MundialSemifinals, MundialFinal, Finished |
| `champion_id` | INTEGER | Nullable. Winner club ID |
| `initial_team_ids_json` | TEXT | JSON array of team IDs (8 for national, 4 for mundial) |
| `advancing_team_ids_json` | TEXT | JSON array of teams advancing from current round |
| `fixtures_json` | TEXT | JSON array of FixtureDto objects |

**FixtureDto JSON format:**
```json
{
  "Day": 4,
  "HomeClubId": 1,
  "AwayClubId": 2,
  "Phase": "Quarterfinals",
  "ResultHome": 2,
  "ResultAway": 1
}
```

---

### `season_calendar`
Full season schedule with fixture data.

| Column | Type | Description |
|--------|------|-------------|
| `day_index` | INTEGER PK | 0-based calendar position |
| `day_number` | INTEGER | Display day number (1-based) |
| `day_type` | TEXT | Enum: Training, Rest, MatchDay, MundialMatchDay, TransferWindow |
| `fixtures_json` | TEXT | JSON array of FixtureDto objects |

---

## Notes

- **File-per-save**: Each save slot is an independent SQLite `.db` file
- **Autosave**: Automatically overwrites `saves/autosave.db` after each day
- **Schema version**: Stored in `save_meta` for future migration support
- **Decimal precision**: Club/manager balances stored as REAL (SQLite has no decimal type). Precision is sufficient for game economy.
- **Enum storage**: All enums stored as their string name (not integer) for readability
