using System.Text.Json;
using DnDManager.Models;
using Microsoft.Data.Sqlite;

namespace DnDManager.Services;

public class SqliteCampaignRepository : ICampaignRepository {
    private string _connectionString = string.Empty;

    private static readonly string DefaultDbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DnDManager",
        "campaign.db");

    public async Task InitializeAsync(string dbPath) {
        if (string.IsNullOrWhiteSpace(dbPath)) {
            dbPath = DefaultDbPath;
        }

        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder {
            DataSource = dbPath
        }.ToString();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Characters (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Type TEXT NOT NULL,
                Name TEXT NOT NULL,
                PlayerName TEXT,
                Initiative INTEGER,
                PassivePerception INTEGER,
                PassiveInvestigation INTEGER,
                ArmorClass INTEGER NOT NULL,
                MaxHitPoints INTEGER,
                CurrentHitPoints INTEGER,
                Conditions TEXT NOT NULL DEFAULT '',
                Notes TEXT NOT NULL DEFAULT '',
                BestiaryEntryId INTEGER,
                SortOrder INTEGER NOT NULL,
                AttacksJson TEXT,
                MultiattackDescription TEXT NOT NULL DEFAULT '',
                InitiativeModifier INTEGER,
                SpecialAbilitiesJson TEXT,
                NonAttackActionsJson TEXT,
                LegendaryActionsJson TEXT,
                LegendaryDescription TEXT NOT NULL DEFAULT '',
                ReactionsJson TEXT,
                BonusActionsJson TEXT
            );

            CREATE TABLE IF NOT EXISTS EncounterState (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                IsActive INTEGER NOT NULL DEFAULT 0,
                RoundNumber INTEGER NOT NULL DEFAULT 0,
                ActiveCharacterIndex INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS DiceHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RawInput TEXT NOT NULL,
                IsValid INTEGER NOT NULL,
                ErrorReason TEXT,
                ResultData TEXT NOT NULL,
                Timestamp TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CampaignNotes (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                Content TEXT NOT NULL DEFAULT '',
                CaretPosition INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS AppSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            INSERT OR IGNORE INTO EncounterState (Id, IsActive, RoundNumber, ActiveCharacterIndex)
            VALUES (1, 0, 0, 0);

            INSERT OR IGNORE INTO CampaignNotes (Id, Content, CaretPosition)
            VALUES (1, '', 0);
            """;
        await command.ExecuteNonQueryAsync();

        // Migrations for existing databases
        string[] migrations = [
            "ALTER TABLE Characters ADD COLUMN InitiativeModifier INTEGER",
            "ALTER TABLE Characters ADD COLUMN SpecialAbilitiesJson TEXT",
            "ALTER TABLE Characters ADD COLUMN NonAttackActionsJson TEXT",
            "ALTER TABLE Characters ADD COLUMN LegendaryActionsJson TEXT",
            "ALTER TABLE Characters ADD COLUMN LegendaryDescription TEXT NOT NULL DEFAULT ''",
            "ALTER TABLE Characters ADD COLUMN ReactionsJson TEXT",
            "ALTER TABLE Characters ADD COLUMN BonusActionsJson TEXT"
        ];
        foreach (var migration in migrations) {
            try {
                await using var migrateCmd = connection.CreateCommand();
                migrateCmd.CommandText = migration;
                await migrateCmd.ExecuteNonQueryAsync();
            } catch (SqliteException) { /* column already exists */ }
        }
    }

    public async Task SaveCharactersAsync(List<Character> characters) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        try {
            await using (var deleteCmd = connection.CreateCommand()) {
                deleteCmd.CommandText = "DELETE FROM Characters";
                await deleteCmd.ExecuteNonQueryAsync();
            }

            foreach (var character in characters) {
                await using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = """
                    INSERT INTO Characters
                        (Type, Name, PlayerName, Initiative, PassivePerception, PassiveInvestigation,
                         ArmorClass, MaxHitPoints, CurrentHitPoints, Conditions, Notes,
                         BestiaryEntryId, SortOrder, AttacksJson, MultiattackDescription, InitiativeModifier,
                         SpecialAbilitiesJson, NonAttackActionsJson, LegendaryActionsJson,
                         LegendaryDescription, ReactionsJson, BonusActionsJson)
                    VALUES
                        (@Type, @Name, @PlayerName, @Initiative, @PassivePerception, @PassiveInvestigation,
                         @ArmorClass, @MaxHitPoints, @CurrentHitPoints, @Conditions, @Notes,
                         @BestiaryEntryId, @SortOrder, @AttacksJson, @MultiattackDescription, @InitiativeModifier,
                         @SpecialAbilitiesJson, @NonAttackActionsJson, @LegendaryActionsJson,
                         @LegendaryDescription, @ReactionsJson, @BonusActionsJson)
                    """;

                var type = character.CharacterType == CharacterType.PC ? "PC" : "NPC";
                insertCmd.Parameters.AddWithValue("@Type", type);
                insertCmd.Parameters.AddWithValue("@Name", character.Name);
                insertCmd.Parameters.AddWithValue("@ArmorClass", character.ArmorClass);
                insertCmd.Parameters.AddWithValue("@Initiative", (object?)character.Initiative ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Conditions", character.Conditions);
                insertCmd.Parameters.AddWithValue("@Notes", character.Notes);
                insertCmd.Parameters.AddWithValue("@SortOrder", character.SortOrder);

                if (character is PlayerCharacter pc) {
                    insertCmd.Parameters.AddWithValue("@PlayerName", pc.PlayerName);
                    insertCmd.Parameters.AddWithValue("@PassivePerception", pc.PassivePerception);
                    insertCmd.Parameters.AddWithValue("@PassiveInvestigation", pc.PassiveInvestigation);
                    insertCmd.Parameters.AddWithValue("@MaxHitPoints", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@CurrentHitPoints", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@BestiaryEntryId", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@AttacksJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@MultiattackDescription", string.Empty);
                    insertCmd.Parameters.AddWithValue("@InitiativeModifier", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@SpecialAbilitiesJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@NonAttackActionsJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@LegendaryActionsJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@LegendaryDescription", string.Empty);
                    insertCmd.Parameters.AddWithValue("@ReactionsJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@BonusActionsJson", DBNull.Value);
                } else if (character is NonPlayerCharacter npc) {
                    insertCmd.Parameters.AddWithValue("@PlayerName", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PassivePerception", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PassiveInvestigation", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@MaxHitPoints", npc.MaxHitPoints);
                    insertCmd.Parameters.AddWithValue("@CurrentHitPoints", npc.CurrentHitPoints);
                    insertCmd.Parameters.AddWithValue("@BestiaryEntryId", (object?)npc.BestiaryEntryId ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@AttacksJson", JsonSerializer.Serialize(npc.Attacks));
                    insertCmd.Parameters.AddWithValue("@MultiattackDescription", npc.MultiattackDescription);
                    insertCmd.Parameters.AddWithValue("@InitiativeModifier", (object?)npc.InitiativeModifier ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@SpecialAbilitiesJson",
                        npc.SpecialAbilities.Count > 0 ? JsonSerializer.Serialize(npc.SpecialAbilities) : DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@NonAttackActionsJson",
                        npc.NonAttackActions.Count > 0 ? JsonSerializer.Serialize(npc.NonAttackActions) : DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@LegendaryActionsJson",
                        npc.LegendaryActions.Count > 0 ? JsonSerializer.Serialize(npc.LegendaryActions) : DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@LegendaryDescription", npc.LegendaryDescription);
                    insertCmd.Parameters.AddWithValue("@ReactionsJson",
                        npc.Reactions.Count > 0 ? JsonSerializer.Serialize(npc.Reactions) : DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@BonusActionsJson",
                        npc.BonusActions.Count > 0 ? JsonSerializer.Serialize(npc.BonusActions) : DBNull.Value);
                } else {
                    insertCmd.Parameters.AddWithValue("@PlayerName", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PassivePerception", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PassiveInvestigation", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@MaxHitPoints", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@CurrentHitPoints", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@BestiaryEntryId", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@AttacksJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@MultiattackDescription", string.Empty);
                    insertCmd.Parameters.AddWithValue("@InitiativeModifier", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@SpecialAbilitiesJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@NonAttackActionsJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@LegendaryActionsJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@LegendaryDescription", string.Empty);
                    insertCmd.Parameters.AddWithValue("@ReactionsJson", DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@BonusActionsJson", DBNull.Value);
                }

                await insertCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        } catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Character>> LoadCharactersAsync() {
        var characters = new List<Character>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Type, Name, PlayerName, Initiative, PassivePerception, PassiveInvestigation,
                   ArmorClass, MaxHitPoints, CurrentHitPoints, Conditions, Notes,
                   BestiaryEntryId, SortOrder, AttacksJson, MultiattackDescription, InitiativeModifier,
                   SpecialAbilitiesJson, NonAttackActionsJson, LegendaryActionsJson,
                   LegendaryDescription, ReactionsJson, BonusActionsJson
            FROM Characters
            ORDER BY SortOrder
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var type = reader.GetString(0);
            Character character;

            if (type == "PC") {
                var pc = new PlayerCharacter {
                    Name = reader.GetString(1),
                    PlayerName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Initiative = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    PassivePerception = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                    PassiveInvestigation = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    ArmorClass = reader.GetInt32(6),
                    Conditions = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    Notes = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    SortOrder = reader.GetInt32(12)
                };
                character = pc;
            } else {
                var npc = new NonPlayerCharacter {
                    Name = reader.GetString(1),
                    Initiative = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    ArmorClass = reader.GetInt32(6),
                    MaxHitPoints = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                    CurrentHitPoints = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                    Conditions = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                    Notes = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    BestiaryEntryId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                    SortOrder = reader.GetInt32(12)
                };

                if (!reader.IsDBNull(13)) {
                    var attacksJson = reader.GetString(13);
                    npc.Attacks = JsonSerializer.Deserialize<List<Attack>>(attacksJson) ?? [];
                }

                if (!reader.IsDBNull(14))
                    npc.MultiattackDescription = reader.GetString(14);

                if (!reader.IsDBNull(15))
                    npc.InitiativeModifier = reader.GetInt32(15);

                if (!reader.IsDBNull(16))
                    npc.SpecialAbilities = JsonSerializer.Deserialize<List<NamedAbility>>(reader.GetString(16)) ?? [];
                if (!reader.IsDBNull(17))
                    npc.NonAttackActions = JsonSerializer.Deserialize<List<NamedAbility>>(reader.GetString(17)) ?? [];
                if (!reader.IsDBNull(18))
                    npc.LegendaryActions = JsonSerializer.Deserialize<List<NamedAbility>>(reader.GetString(18)) ?? [];
                if (!reader.IsDBNull(19))
                    npc.LegendaryDescription = reader.GetString(19);
                if (!reader.IsDBNull(20))
                    npc.Reactions = JsonSerializer.Deserialize<List<NamedAbility>>(reader.GetString(20)) ?? [];
                if (!reader.IsDBNull(21))
                    npc.BonusActions = JsonSerializer.Deserialize<List<NamedAbility>>(reader.GetString(21)) ?? [];

                npc.ParseLegendaryActionBudget();

                character = npc;
            }

            characters.Add(character);
        }

        return characters;
    }

    public async Task SaveEncounterStateAsync(EncounterState state) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE EncounterState
            SET IsActive = @IsActive, RoundNumber = @RoundNumber, ActiveCharacterIndex = @ActiveCharacterIndex
            WHERE Id = 1
            """;
        command.Parameters.AddWithValue("@IsActive", state.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@RoundNumber", state.RoundNumber);
        command.Parameters.AddWithValue("@ActiveCharacterIndex", state.ActiveCharacterIndex);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<EncounterState> LoadEncounterStateAsync() {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT IsActive, RoundNumber, ActiveCharacterIndex FROM EncounterState WHERE Id = 1";

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync()) {
            return new EncounterState {
                IsActive = reader.GetInt32(0) != 0,
                RoundNumber = reader.GetInt32(1),
                ActiveCharacterIndex = reader.GetInt32(2)
            };
        }

        return new EncounterState();
    }

    public async Task SaveDiceHistoryAsync(List<DiceRollResult> history) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        try {
            await using (var deleteCmd = connection.CreateCommand()) {
                deleteCmd.CommandText = "DELETE FROM DiceHistory";
                await deleteCmd.ExecuteNonQueryAsync();
            }

            foreach (var result in history) {
                await using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = """
                    INSERT INTO DiceHistory (RawInput, IsValid, ErrorReason, ResultData, Timestamp)
                    VALUES (@RawInput, @IsValid, @ErrorReason, @ResultData, @Timestamp)
                    """;
                insertCmd.Parameters.AddWithValue("@RawInput", result.RawInput);
                insertCmd.Parameters.AddWithValue("@IsValid", result.IsValid ? 1 : 0);
                insertCmd.Parameters.AddWithValue("@ErrorReason", (object?)result.ErrorReason ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ResultData", JsonSerializer.Serialize(result.PartResults));
                insertCmd.Parameters.AddWithValue("@Timestamp", result.Timestamp.ToString("o"));
                await insertCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        } catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<DiceRollResult>> LoadDiceHistoryAsync() {
        var history = new List<DiceRollResult>();

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT RawInput, IsValid, ErrorReason, ResultData, Timestamp FROM DiceHistory ORDER BY Id";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var result = new DiceRollResult {
                RawInput = reader.GetString(0),
                IsValid = reader.GetInt32(1) != 0,
                ErrorReason = reader.IsDBNull(2) ? null : reader.GetString(2),
                PartResults = JsonSerializer.Deserialize<List<DicePartResult>>(reader.GetString(3)) ?? [],
                Timestamp = DateTime.Parse(reader.GetString(4))
            };
            history.Add(result);
        }

        return history;
    }

    public async Task SaveCampaignNotesAsync(string content, int caretPosition) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE CampaignNotes
            SET Content = @Content, CaretPosition = @CaretPosition
            WHERE Id = 1
            """;
        command.Parameters.AddWithValue("@Content", content);
        command.Parameters.AddWithValue("@CaretPosition", caretPosition);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<(string content, int caretPosition)> LoadCampaignNotesAsync() {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Content, CaretPosition FROM CampaignNotes WHERE Id = 1";

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync()) {
            return (reader.GetString(0), reader.GetInt32(1));
        }

        return (string.Empty, 0);
    }

    public async Task SaveSettingAsync(string key, string value) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO AppSettings (Key, Value) VALUES (@Key, @Value)";
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<string?> LoadSettingAsync(string key) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM AppSettings WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);

        var result = await command.ExecuteScalarAsync();
        return result as string;
    }
}
