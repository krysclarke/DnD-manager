using System.Text.Json;
using DnDManager.Models;
using Microsoft.Data.Sqlite;

namespace DnDManager.Services;

public class BestiaryFileService : IBestiaryFileService {
    private string _masterDbPath = string.Empty;

    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS BestiaryEntries (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            ArmorClass INTEGER NOT NULL,
            ArmorDescription TEXT NOT NULL DEFAULT '',
            HitPoints INTEGER NOT NULL,
            HitDice TEXT NOT NULL DEFAULT '',
            Size TEXT NOT NULL DEFAULT '',
            Type TEXT NOT NULL DEFAULT '',
            Subtype TEXT NOT NULL DEFAULT '',
            Alignment TEXT NOT NULL DEFAULT '',
            ChallengeRating TEXT NOT NULL DEFAULT '',
            Speed TEXT NOT NULL DEFAULT '',
            Strength INTEGER NOT NULL DEFAULT 10,
            Dexterity INTEGER NOT NULL DEFAULT 10,
            Constitution INTEGER NOT NULL DEFAULT 10,
            Intelligence INTEGER NOT NULL DEFAULT 10,
            Wisdom INTEGER NOT NULL DEFAULT 10,
            Charisma INTEGER NOT NULL DEFAULT 10,
            Senses TEXT NOT NULL DEFAULT '',
            Languages TEXT NOT NULL DEFAULT '',
            MultiattackDescription TEXT NOT NULL DEFAULT '',
            SpecialAbilitiesJson TEXT,
            NonAttackActionsJson TEXT,
            AttacksJson TEXT,
            LegendaryActionsJson TEXT,
            LegendaryDescription TEXT NOT NULL DEFAULT '',
            ReactionsJson TEXT,
            BonusActionsJson TEXT,
            InitiativeModifier INTEGER,
            Source TEXT NOT NULL DEFAULT 'Manual',
            Open5eSlug TEXT
        );
        """;

    private const string UpsertSql = """
        INSERT INTO BestiaryEntries (
            Id, Name, ArmorClass, ArmorDescription, HitPoints, HitDice,
            Size, Type, Subtype, Alignment, ChallengeRating, Speed,
            Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma,
            Senses, Languages, MultiattackDescription, SpecialAbilitiesJson,
            NonAttackActionsJson, AttacksJson, LegendaryActionsJson,
            LegendaryDescription, ReactionsJson, BonusActionsJson,
            InitiativeModifier, Source, Open5eSlug
        ) VALUES (
            @Id, @Name, @ArmorClass, @ArmorDescription, @HitPoints, @HitDice,
            @Size, @Type, @Subtype, @Alignment, @ChallengeRating, @Speed,
            @Strength, @Dexterity, @Constitution, @Intelligence, @Wisdom, @Charisma,
            @Senses, @Languages, @MultiattackDescription, @SpecialAbilitiesJson,
            @NonAttackActionsJson, @AttacksJson, @LegendaryActionsJson,
            @LegendaryDescription, @ReactionsJson, @BonusActionsJson,
            @InitiativeModifier, @Source, @Open5eSlug
        )
        ON CONFLICT(Id) DO UPDATE SET
            Name=excluded.Name, ArmorClass=excluded.ArmorClass,
            ArmorDescription=excluded.ArmorDescription, HitPoints=excluded.HitPoints,
            HitDice=excluded.HitDice, Size=excluded.Size, Type=excluded.Type,
            Subtype=excluded.Subtype, Alignment=excluded.Alignment,
            ChallengeRating=excluded.ChallengeRating, Speed=excluded.Speed,
            Strength=excluded.Strength, Dexterity=excluded.Dexterity,
            Constitution=excluded.Constitution, Intelligence=excluded.Intelligence,
            Wisdom=excluded.Wisdom, Charisma=excluded.Charisma,
            Senses=excluded.Senses, Languages=excluded.Languages,
            MultiattackDescription=excluded.MultiattackDescription,
            SpecialAbilitiesJson=excluded.SpecialAbilitiesJson,
            NonAttackActionsJson=excluded.NonAttackActionsJson,
            AttacksJson=excluded.AttacksJson,
            LegendaryActionsJson=excluded.LegendaryActionsJson,
            LegendaryDescription=excluded.LegendaryDescription,
            ReactionsJson=excluded.ReactionsJson,
            BonusActionsJson=excluded.BonusActionsJson,
            InitiativeModifier=excluded.InitiativeModifier,
            Source=excluded.Source, Open5eSlug=excluded.Open5eSlug;
        """;

    private const string InsertSql = """
        INSERT INTO BestiaryEntries (
            Name, ArmorClass, ArmorDescription, HitPoints, HitDice,
            Size, Type, Subtype, Alignment, ChallengeRating, Speed,
            Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma,
            Senses, Languages, MultiattackDescription, SpecialAbilitiesJson,
            NonAttackActionsJson, AttacksJson, LegendaryActionsJson,
            LegendaryDescription, ReactionsJson, BonusActionsJson,
            InitiativeModifier, Source, Open5eSlug
        ) VALUES (
            @Name, @ArmorClass, @ArmorDescription, @HitPoints, @HitDice,
            @Size, @Type, @Subtype, @Alignment, @ChallengeRating, @Speed,
            @Strength, @Dexterity, @Constitution, @Intelligence, @Wisdom, @Charisma,
            @Senses, @Languages, @MultiattackDescription, @SpecialAbilitiesJson,
            @NonAttackActionsJson, @AttacksJson, @LegendaryActionsJson,
            @LegendaryDescription, @ReactionsJson, @BonusActionsJson,
            @InitiativeModifier, @Source, @Open5eSlug
        );
        """;

    private const string UpsertByNameSql = """
        INSERT INTO BestiaryEntries (
            Name, ArmorClass, ArmorDescription, HitPoints, HitDice,
            Size, Type, Subtype, Alignment, ChallengeRating, Speed,
            Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma,
            Senses, Languages, MultiattackDescription, SpecialAbilitiesJson,
            NonAttackActionsJson, AttacksJson, LegendaryActionsJson,
            LegendaryDescription, ReactionsJson, BonusActionsJson,
            InitiativeModifier, Source, Open5eSlug
        ) VALUES (
            @Name, @ArmorClass, @ArmorDescription, @HitPoints, @HitDice,
            @Size, @Type, @Subtype, @Alignment, @ChallengeRating, @Speed,
            @Strength, @Dexterity, @Constitution, @Intelligence, @Wisdom, @Charisma,
            @Senses, @Languages, @MultiattackDescription, @SpecialAbilitiesJson,
            @NonAttackActionsJson, @AttacksJson, @LegendaryActionsJson,
            @LegendaryDescription, @ReactionsJson, @BonusActionsJson,
            @InitiativeModifier, @Source, @Open5eSlug
        )
        ON CONFLICT(Name) DO UPDATE SET
            ArmorClass=excluded.ArmorClass,
            ArmorDescription=excluded.ArmorDescription, HitPoints=excluded.HitPoints,
            HitDice=excluded.HitDice, Size=excluded.Size, Type=excluded.Type,
            Subtype=excluded.Subtype, Alignment=excluded.Alignment,
            ChallengeRating=excluded.ChallengeRating, Speed=excluded.Speed,
            Strength=excluded.Strength, Dexterity=excluded.Dexterity,
            Constitution=excluded.Constitution, Intelligence=excluded.Intelligence,
            Wisdom=excluded.Wisdom, Charisma=excluded.Charisma,
            Senses=excluded.Senses, Languages=excluded.Languages,
            MultiattackDescription=excluded.MultiattackDescription,
            SpecialAbilitiesJson=excluded.SpecialAbilitiesJson,
            NonAttackActionsJson=excluded.NonAttackActionsJson,
            AttacksJson=excluded.AttacksJson,
            LegendaryActionsJson=excluded.LegendaryActionsJson,
            LegendaryDescription=excluded.LegendaryDescription,
            ReactionsJson=excluded.ReactionsJson,
            BonusActionsJson=excluded.BonusActionsJson,
            InitiativeModifier=excluded.InitiativeModifier,
            Source=excluded.Source, Open5eSlug=excluded.Open5eSlug;
        """;

    public async Task InitializeMasterAsync(string masterDbPath) {
        _masterDbPath = masterDbPath;
        var connectionString = BuildConnectionString(masterDbPath, SqliteOpenMode.ReadWriteCreate);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await EnsureTableAsync(connection);
        await RunMigrationsAsync(connection);

        // Add unique index on Name for upsert-by-name support
        try {
            await using var idxCmd = connection.CreateCommand();
            idxCmd.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS IX_BestiaryEntries_Name ON BestiaryEntries(Name);";
            await idxCmd.ExecuteNonQueryAsync();
        } catch (SqliteException) {
            // Index may conflict with existing duplicate names — ignore
        }
    }

    public async Task<List<BestiaryEntry>> LoadEntriesAsync() {
        return await QueryEntriesAsync(_masterDbPath, "SELECT * FROM BestiaryEntries ORDER BY Name;");
    }

    public async Task<List<BestiaryEntry>> SearchEntriesAsync(string searchTerm) {
        var sql = "SELECT * FROM BestiaryEntries WHERE Name LIKE @Search ORDER BY Name;";
        return await QueryEntriesAsync(_masterDbPath, sql, ("@Search", $"%{searchTerm}%"));
    }

    public async Task SaveEntryAsync(BestiaryEntry entry) {
        var connectionString = BuildConnectionString(_masterDbPath, SqliteOpenMode.ReadWrite);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await EnsureTableAsync(connection);

        var isNew = entry.Id == 0;
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = isNew ? InsertSql : UpsertSql;

        if (!isNew)
            cmd.Parameters.AddWithValue("@Id", entry.Id);

        AddEntryParameters(cmd, entry);
        await cmd.ExecuteNonQueryAsync();

        if (isNew) {
            await using var idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid();";
            entry.Id = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
        }
    }

    public async Task DeleteEntryAsync(int entryId) {
        var connectionString = BuildConnectionString(_masterDbPath, SqliteOpenMode.ReadWrite);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM BestiaryEntries WHERE Id = @Id;";
        cmd.Parameters.AddWithValue("@Id", entryId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetEntryCountAsync() {
        var connectionString = BuildConnectionString(_masterDbPath, SqliteOpenMode.ReadOnly);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM BestiaryEntries;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task ImportEntriesAsync(List<BestiaryEntry> entries, ImportDuplicateMode mode) {
        var connectionString = BuildConnectionString(_masterDbPath, SqliteOpenMode.ReadWrite);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await EnsureTableAsync(connection);

        await using var transaction = await connection.BeginTransactionAsync();

        if (mode == ImportDuplicateMode.Overwrite) {
            foreach (var entry in entries) {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = UpsertByNameSql;
                AddEntryParameters(cmd, entry);
                await cmd.ExecuteNonQueryAsync();
            }
        } else {
            // Skip mode: only insert entries whose Name doesn't already exist
            var existingNames = await GetExistingNamesAsync(connection);
            foreach (var entry in entries) {
                if (existingNames.Contains(entry.Name)) continue;
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = InsertSql;
                AddEntryParameters(cmd, entry);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        await transaction.CommitAsync();
    }

    public async Task<List<BestiaryEntry>> LoadEntriesFromFileAsync(string filePath) {
        return await QueryEntriesAsync(filePath, "SELECT * FROM BestiaryEntries ORDER BY Name;");
    }

    public async Task<List<string>> FindDuplicateNamesAsync(List<BestiaryEntry> entries) {
        var connectionString = BuildConnectionString(_masterDbPath, SqliteOpenMode.ReadOnly);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var existingNames = await GetExistingNamesAsync(connection);
        return entries.Select(e => e.Name).Where(n => existingNames.Contains(n)).ToList();
    }

    private static async Task<HashSet<string>> GetExistingNamesAsync(SqliteConnection connection) {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Name FROM BestiaryEntries;";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));
        return names;
    }

    private async Task<List<BestiaryEntry>> QueryEntriesAsync(
        string filePath, string sql, params (string name, object value)[] parameters) {
        var entries = new List<BestiaryEntry>();
        var connectionString = BuildConnectionString(filePath, SqliteOpenMode.ReadOnly);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            entries.Add(ReadEntry(reader));
        }
        return entries;
    }

    private static BestiaryEntry ReadEntry(SqliteDataReader reader) {
        var entry = new BestiaryEntry {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            ArmorClass = reader.GetInt32(reader.GetOrdinal("ArmorClass")),
            ArmorDescription = reader.GetString(reader.GetOrdinal("ArmorDescription")),
            HitPoints = reader.GetInt32(reader.GetOrdinal("HitPoints")),
            HitDice = reader.GetString(reader.GetOrdinal("HitDice")),
            Size = Enum.TryParse<CreatureSize>(reader.GetString(reader.GetOrdinal("Size")), true, out var size)
                ? size : CreatureSize.Medium,
            Type = reader.GetString(reader.GetOrdinal("Type")),
            Subtype = reader.GetString(reader.GetOrdinal("Subtype")),
            Alignment = reader.GetString(reader.GetOrdinal("Alignment")),
            ChallengeRating = reader.GetString(reader.GetOrdinal("ChallengeRating")),
            Speed = reader.GetString(reader.GetOrdinal("Speed")),
            Strength = reader.GetInt32(reader.GetOrdinal("Strength")),
            Dexterity = reader.GetInt32(reader.GetOrdinal("Dexterity")),
            Constitution = reader.GetInt32(reader.GetOrdinal("Constitution")),
            Intelligence = reader.GetInt32(reader.GetOrdinal("Intelligence")),
            Wisdom = reader.GetInt32(reader.GetOrdinal("Wisdom")),
            Charisma = reader.GetInt32(reader.GetOrdinal("Charisma")),
            Senses = reader.GetString(reader.GetOrdinal("Senses")),
            Languages = reader.GetString(reader.GetOrdinal("Languages")),
            MultiattackDescription = reader.GetString(reader.GetOrdinal("MultiattackDescription")),
            Source = reader.GetString(reader.GetOrdinal("Source"))
        };

        var initOrdinal = reader.GetOrdinal("InitiativeModifier");
        if (!reader.IsDBNull(initOrdinal))
            entry.InitiativeModifier = reader.GetInt32(initOrdinal);

        var specialOrdinal = reader.GetOrdinal("SpecialAbilitiesJson");
        if (!reader.IsDBNull(specialOrdinal)) {
            entry.SpecialAbilitiesJson = reader.GetString(specialOrdinal);
            entry.SpecialAbilities = DeserializeNamedAbilities(entry.SpecialAbilitiesJson);
        }

        entry.NonAttackActions = DeserializeNamedAbilitiesColumn(reader, "NonAttackActionsJson");

        var attacksOrdinal = reader.GetOrdinal("AttacksJson");
        if (!reader.IsDBNull(attacksOrdinal)) {
            var json = reader.GetString(attacksOrdinal);
            entry.Attacks = JsonSerializer.Deserialize<List<Attack>>(json) ?? [];
        }

        entry.LegendaryActions = DeserializeNamedAbilitiesColumn(reader, "LegendaryActionsJson");

        var legDescOrdinal = reader.GetOrdinal("LegendaryDescription");
        if (!reader.IsDBNull(legDescOrdinal))
            entry.LegendaryDescription = reader.GetString(legDescOrdinal);

        entry.Reactions = DeserializeNamedAbilitiesColumn(reader, "ReactionsJson");
        entry.BonusActions = DeserializeNamedAbilitiesColumn(reader, "BonusActionsJson");

        var slugOrdinal = reader.GetOrdinal("Open5eSlug");
        if (!reader.IsDBNull(slugOrdinal))
            entry.Open5eSlug = reader.GetString(slugOrdinal);

        return entry;
    }

    private static void AddEntryParameters(SqliteCommand cmd, BestiaryEntry entry) {
        cmd.Parameters.AddWithValue("@Name", entry.Name);
        cmd.Parameters.AddWithValue("@ArmorClass", entry.ArmorClass);
        cmd.Parameters.AddWithValue("@ArmorDescription", entry.ArmorDescription);
        cmd.Parameters.AddWithValue("@HitPoints", entry.HitPoints);
        cmd.Parameters.AddWithValue("@HitDice", entry.HitDice);
        cmd.Parameters.AddWithValue("@Size", entry.Size.ToString());
        cmd.Parameters.AddWithValue("@Type", entry.Type);
        cmd.Parameters.AddWithValue("@Subtype", entry.Subtype);
        cmd.Parameters.AddWithValue("@Alignment", entry.Alignment);
        cmd.Parameters.AddWithValue("@ChallengeRating", entry.ChallengeRating);
        cmd.Parameters.AddWithValue("@Speed", entry.Speed);
        cmd.Parameters.AddWithValue("@Strength", entry.Strength);
        cmd.Parameters.AddWithValue("@Dexterity", entry.Dexterity);
        cmd.Parameters.AddWithValue("@Constitution", entry.Constitution);
        cmd.Parameters.AddWithValue("@Intelligence", entry.Intelligence);
        cmd.Parameters.AddWithValue("@Wisdom", entry.Wisdom);
        cmd.Parameters.AddWithValue("@Charisma", entry.Charisma);
        cmd.Parameters.AddWithValue("@Senses", entry.Senses);
        cmd.Parameters.AddWithValue("@Languages", entry.Languages);
        cmd.Parameters.AddWithValue("@MultiattackDescription", entry.MultiattackDescription);
        cmd.Parameters.AddWithValue("@SpecialAbilitiesJson",
            entry.SpecialAbilities.Count > 0 ? JsonSerializer.Serialize(entry.SpecialAbilities) :
            string.IsNullOrEmpty(entry.SpecialAbilitiesJson) ? DBNull.Value : entry.SpecialAbilitiesJson);
        cmd.Parameters.AddWithValue("@NonAttackActionsJson",
            entry.NonAttackActions.Count > 0 ? JsonSerializer.Serialize(entry.NonAttackActions) : DBNull.Value);
        cmd.Parameters.AddWithValue("@AttacksJson",
            entry.Attacks.Count > 0 ? JsonSerializer.Serialize(entry.Attacks) : DBNull.Value);
        cmd.Parameters.AddWithValue("@LegendaryActionsJson",
            entry.LegendaryActions.Count > 0 ? JsonSerializer.Serialize(entry.LegendaryActions) : DBNull.Value);
        cmd.Parameters.AddWithValue("@LegendaryDescription", entry.LegendaryDescription);
        cmd.Parameters.AddWithValue("@ReactionsJson",
            entry.Reactions.Count > 0 ? JsonSerializer.Serialize(entry.Reactions) : DBNull.Value);
        cmd.Parameters.AddWithValue("@BonusActionsJson",
            entry.BonusActions.Count > 0 ? JsonSerializer.Serialize(entry.BonusActions) : DBNull.Value);
        cmd.Parameters.AddWithValue("@InitiativeModifier",
            (object?)entry.InitiativeModifier ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Source", entry.Source);
        cmd.Parameters.AddWithValue("@Open5eSlug", (object?)entry.Open5eSlug ?? DBNull.Value);
    }

    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private static List<NamedAbility> DeserializeNamedAbilities(string json) {
        if (string.IsNullOrEmpty(json)) return [];
        try {
            // Try standard deserialization first (new format: Name/Description)
            var result = JsonSerializer.Deserialize<List<NamedAbility>>(json, CaseInsensitiveOptions) ?? [];
            // If entries have empty descriptions, try Open5e raw format (name/desc)
            if (result.Count > 0 && result.All(a => string.IsNullOrEmpty(a.Description))) {
                var doc = JsonDocument.Parse(json);
                var parsed = new List<NamedAbility>();
                foreach (var item in doc.RootElement.EnumerateArray()) {
                    parsed.Add(new NamedAbility {
                        Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty,
                        Description = item.TryGetProperty("desc", out var d) ? d.GetString() ?? string.Empty : string.Empty
                    });
                }
                return parsed;
            }
            return result;
        } catch (JsonException) {
            return [];
        }
    }

    private static List<NamedAbility> DeserializeNamedAbilitiesColumn(SqliteDataReader reader, string columnName) {
        try {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return [];
            return DeserializeNamedAbilities(reader.GetString(ordinal));
        } catch (ArgumentOutOfRangeException) {
            // Column doesn't exist yet (pre-migration DB)
            return [];
        }
    }

    private static async Task EnsureTableAsync(SqliteConnection connection) {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = CreateTableSql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task RunMigrationsAsync(SqliteConnection connection) {
        string[] newColumns = [
            "NonAttackActionsJson TEXT",
            "LegendaryActionsJson TEXT",
            "LegendaryDescription TEXT NOT NULL DEFAULT ''",
            "ReactionsJson TEXT",
            "BonusActionsJson TEXT"
        ];
        foreach (var colDef in newColumns) {
            try {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = $"ALTER TABLE BestiaryEntries ADD COLUMN {colDef}";
                await cmd.ExecuteNonQueryAsync();
            } catch (SqliteException) {
                // Column already exists
            }
        }
    }

    private static string BuildConnectionString(string filePath, SqliteOpenMode mode) {
        return new SqliteConnectionStringBuilder {
            DataSource = filePath,
            Mode = mode
        }.ToString();
    }
}
