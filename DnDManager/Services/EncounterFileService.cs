using System.Text.Json;
using DnDManager.Models;
using Microsoft.Data.Sqlite;

namespace DnDManager.Services;

public class EncounterFileService : IEncounterFileService {
    private const string CreateCharactersTableSql = """
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
            InitiativeModifier INTEGER
        );
        """;

    private const string CreateDiceHistoryTableSql = """
        CREATE TABLE IF NOT EXISTS DiceHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RawInput TEXT NOT NULL,
            IsValid INTEGER NOT NULL,
            ErrorReason TEXT,
            ResultData TEXT NOT NULL,
            Timestamp TEXT NOT NULL
        );
        """;

    private const string CreateCampaignNotesTableSql = """
        CREATE TABLE IF NOT EXISTS CampaignNotes (
            Id INTEGER PRIMARY KEY CHECK (Id = 1),
            Content TEXT NOT NULL DEFAULT '',
            CaretPosition INTEGER NOT NULL DEFAULT 0
        );
        """;

    private const string InsertCharacterSql = """
        INSERT INTO Characters (
            Type, Name, PlayerName, Initiative,
            PassivePerception, PassiveInvestigation, ArmorClass,
            MaxHitPoints, CurrentHitPoints, Conditions, Notes,
            BestiaryEntryId, SortOrder, AttacksJson, MultiattackDescription, InitiativeModifier
        ) VALUES (
            @Type, @Name, @PlayerName, @Initiative,
            @PassivePerception, @PassiveInvestigation, @ArmorClass,
            @MaxHitPoints, @CurrentHitPoints, @Conditions, @Notes,
            @BestiaryEntryId, @SortOrder, @AttacksJson, @MultiattackDescription, @InitiativeModifier
        );
        """;

    public async Task SaveCharactersToFileAsync(string filePath, List<Character> characters,
        CharacterType? filterType = null) {
        var toSave = filterType.HasValue
            ? characters.Where(c => c.CharacterType == filterType.Value).ToList()
            : characters;

        var connectionString = new SqliteConnectionStringBuilder {
            DataSource = filePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var createCmd = connection.CreateCommand();
        createCmd.CommandText = CreateCharactersTableSql;
        await createCmd.ExecuteNonQueryAsync();

        await using var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM Characters;";
        await deleteCmd.ExecuteNonQueryAsync();

        await WriteCharactersAsync(connection, toSave);
    }

    public async Task<List<Character>> LoadCharactersFromFileAsync(string filePath) {
        var connectionString = new SqliteConnectionStringBuilder {
            DataSource = filePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        return await ReadCharactersAsync(connection);
    }

    public async Task SaveEncounterToFileAsync(string filePath, List<Character> characters,
        List<DiceRollResult> diceHistory, string campaignNotes, int caretPosition) {
        var connectionString = new SqliteConnectionStringBuilder {
            DataSource = filePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Create all tables
        await using (var cmd = connection.CreateCommand()) {
            cmd.CommandText = CreateCharactersTableSql;
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var cmd = connection.CreateCommand()) {
            cmd.CommandText = CreateDiceHistoryTableSql;
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var cmd = connection.CreateCommand()) {
            cmd.CommandText = CreateCampaignNotesTableSql;
            await cmd.ExecuteNonQueryAsync();
        }

        // Clear existing data before writing
        await using (var cmd = connection.CreateCommand()) {
            cmd.CommandText = "DELETE FROM Characters; DELETE FROM DiceHistory;";
            await cmd.ExecuteNonQueryAsync();
        }

        // Write all data
        await WriteCharactersAsync(connection, characters);
        await WriteDiceHistoryAsync(connection, diceHistory);
        await WriteCampaignNotesAsync(connection, campaignNotes, caretPosition);
    }

    public async Task<EncounterFileData> LoadEncounterFromFileAsync(string filePath) {
        var connectionString = new SqliteConnectionStringBuilder {
            DataSource = filePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var characters = await ReadCharactersAsync(connection);
        var diceHistory = await ReadDiceHistoryAsync(connection);
        var (notes, caret) = await ReadCampaignNotesAsync(connection);

        return new EncounterFileData(characters, diceHistory, notes, caret);
    }

    private async Task WriteCharactersAsync(SqliteConnection connection, List<Character> characters) {
        foreach (var character in characters) {
            await using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = InsertCharacterSql;

            insertCmd.Parameters.AddWithValue("@Type", character.CharacterType.ToString());
            insertCmd.Parameters.AddWithValue("@Name", character.Name);
            insertCmd.Parameters.AddWithValue("@Initiative", (object?)character.Initiative ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@ArmorClass", character.ArmorClass);
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
            } else if (character is NonPlayerCharacter npc) {
                insertCmd.Parameters.AddWithValue("@PlayerName", DBNull.Value);
                insertCmd.Parameters.AddWithValue("@PassivePerception", DBNull.Value);
                insertCmd.Parameters.AddWithValue("@PassiveInvestigation", DBNull.Value);
                insertCmd.Parameters.AddWithValue("@MaxHitPoints", npc.MaxHitPoints);
                insertCmd.Parameters.AddWithValue("@CurrentHitPoints", npc.CurrentHitPoints);
                insertCmd.Parameters.AddWithValue("@BestiaryEntryId",
                    (object?)npc.BestiaryEntryId ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@AttacksJson",
                    npc.Attacks.Count > 0
                        ? JsonSerializer.Serialize(npc.Attacks)
                        : DBNull.Value);
                insertCmd.Parameters.AddWithValue("@MultiattackDescription", npc.MultiattackDescription);
                insertCmd.Parameters.AddWithValue("@InitiativeModifier", (object?)npc.InitiativeModifier ?? DBNull.Value);
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
            }

            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task WriteDiceHistoryAsync(SqliteConnection connection, List<DiceRollResult> history) {
        foreach (var result in history) {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO DiceHistory (RawInput, IsValid, ErrorReason, ResultData, Timestamp)
                VALUES (@RawInput, @IsValid, @ErrorReason, @ResultData, @Timestamp)
                """;
            cmd.Parameters.AddWithValue("@RawInput", result.RawInput);
            cmd.Parameters.AddWithValue("@IsValid", result.IsValid ? 1 : 0);
            cmd.Parameters.AddWithValue("@ErrorReason", (object?)result.ErrorReason ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ResultData", JsonSerializer.Serialize(result.PartResults));
            cmd.Parameters.AddWithValue("@Timestamp", result.Timestamp.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task WriteCampaignNotesAsync(SqliteConnection connection, string content, int caretPosition) {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO CampaignNotes (Id, Content, CaretPosition)
            VALUES (1, @Content, @CaretPosition)
            """;
        cmd.Parameters.AddWithValue("@Content", content);
        cmd.Parameters.AddWithValue("@CaretPosition", caretPosition);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<Character>> ReadCharactersAsync(SqliteConnection connection) {
        var characters = new List<Character>();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Characters ORDER BY SortOrder;";

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var type = reader.GetString(reader.GetOrdinal("Type"));

            Character character;
            if (type == CharacterType.PC.ToString()) {
                var pc = new PlayerCharacter {
                    PlayerName = reader.IsDBNull(reader.GetOrdinal("PlayerName"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("PlayerName")),
                    PassivePerception = reader.IsDBNull(reader.GetOrdinal("PassivePerception"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("PassivePerception")),
                    PassiveInvestigation = reader.IsDBNull(reader.GetOrdinal("PassiveInvestigation"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("PassiveInvestigation"))
                };
                character = pc;
            } else {
                var npc = new NonPlayerCharacter {
                    MaxHitPoints = reader.IsDBNull(reader.GetOrdinal("MaxHitPoints"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("MaxHitPoints")),
                    CurrentHitPoints = reader.IsDBNull(reader.GetOrdinal("CurrentHitPoints"))
                        ? 0
                        : reader.GetInt32(reader.GetOrdinal("CurrentHitPoints")),
                    BestiaryEntryId = reader.IsDBNull(reader.GetOrdinal("BestiaryEntryId"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("BestiaryEntryId"))
                };

                var attacksOrdinal = reader.GetOrdinal("AttacksJson");
                if (!reader.IsDBNull(attacksOrdinal)) {
                    var attacksJson = reader.GetString(attacksOrdinal);
                    npc.Attacks = JsonSerializer.Deserialize<List<Attack>>(attacksJson) ?? [];
                }

                var multiattackOrdinal = reader.GetOrdinal("MultiattackDescription");
                if (!reader.IsDBNull(multiattackOrdinal))
                    npc.MultiattackDescription = reader.GetString(multiattackOrdinal);

                try {
                    var initModOrdinal = reader.GetOrdinal("InitiativeModifier");
                    if (!reader.IsDBNull(initModOrdinal))
                        npc.InitiativeModifier = reader.GetInt32(initModOrdinal);
                } catch (IndexOutOfRangeException) { /* old file without column */ }

                character = npc;
            }

            character.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            character.Name = reader.GetString(reader.GetOrdinal("Name"));
            character.Initiative = reader.IsDBNull(reader.GetOrdinal("Initiative"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("Initiative"));
            character.ArmorClass = reader.GetInt32(reader.GetOrdinal("ArmorClass"));
            character.Conditions = reader.GetString(reader.GetOrdinal("Conditions"));
            character.Notes = reader.GetString(reader.GetOrdinal("Notes"));
            character.SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder"));

            characters.Add(character);
        }

        return characters;
    }

    private static async Task<List<DiceRollResult>> ReadDiceHistoryAsync(SqliteConnection connection) {
        var history = new List<DiceRollResult>();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT RawInput, IsValid, ErrorReason, ResultData, Timestamp FROM DiceHistory ORDER BY Id";

        await using var reader = await cmd.ExecuteReaderAsync();
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

    private static async Task<(string content, int caretPosition)> ReadCampaignNotesAsync(SqliteConnection connection) {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Content, CaretPosition FROM CampaignNotes WHERE Id = 1";

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync()) {
            return (reader.GetString(0), reader.GetInt32(1));
        }

        return (string.Empty, 0);
    }
}
