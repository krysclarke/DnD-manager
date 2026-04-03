using DnDManager.Models;
using Microsoft.Data.Sqlite;

namespace DnDManager.Services;

public class SpellDatabaseService : ISpellDatabaseService {
    private string _dbPath = string.Empty;

    private const string CreateSpellsTableSql = """
        CREATE TABLE IF NOT EXISTS Spells (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Slug TEXT NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            Level INTEGER NOT NULL DEFAULT 0,
            School TEXT NOT NULL DEFAULT '',
            CastingTime TEXT NOT NULL DEFAULT '',
            Range TEXT NOT NULL DEFAULT '',
            Components TEXT NOT NULL DEFAULT '',
            Material TEXT NOT NULL DEFAULT '',
            Duration TEXT NOT NULL DEFAULT '',
            Concentration INTEGER NOT NULL DEFAULT 0,
            Ritual INTEGER NOT NULL DEFAULT 0,
            Description TEXT NOT NULL DEFAULT '',
            HigherLevel TEXT NOT NULL DEFAULT '',
            Classes TEXT NOT NULL DEFAULT '',
            Source TEXT NOT NULL DEFAULT 'Open5e'
        );
        """;

    private const string CreateMonsterSpellsTableSql = """
        CREATE TABLE IF NOT EXISTS MonsterSpells (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            MonsterSlug TEXT NOT NULL,
            SpellSlug TEXT NOT NULL,
            SlotLevel INTEGER NOT NULL DEFAULT 0,
            IsPreCast INTEGER NOT NULL DEFAULT 0,
            UsageType INTEGER NOT NULL DEFAULT 0,
            UsesPerDay INTEGER,
            UNIQUE(MonsterSlug, SpellSlug)
        );
        CREATE INDEX IF NOT EXISTS IX_MonsterSpells_MonsterSlug ON MonsterSpells(MonsterSlug);
        """;

    private const string UpsertSpellSql = """
        INSERT INTO Spells (Slug, Name, Level, School, CastingTime, Range, Components, Material, Duration, Concentration, Ritual, Description, HigherLevel, Classes, Source)
        VALUES (@Slug, @Name, @Level, @School, @CastingTime, @Range, @Components, @Material, @Duration, @Concentration, @Ritual, @Description, @HigherLevel, @Classes, @Source)
        ON CONFLICT(Slug) DO UPDATE SET
            Name=excluded.Name, Level=excluded.Level, School=excluded.School,
            CastingTime=excluded.CastingTime, Range=excluded.Range,
            Components=excluded.Components, Material=excluded.Material,
            Duration=excluded.Duration, Concentration=excluded.Concentration,
            Ritual=excluded.Ritual, Description=excluded.Description,
            HigherLevel=excluded.HigherLevel, Classes=excluded.Classes,
            Source=excluded.Source;
        """;

    public async Task InitializeAsync(string dbPath) {
        _dbPath = dbPath;
        var connectionString = BuildConnectionString(dbPath, SqliteOpenMode.ReadWriteCreate);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd1 = connection.CreateCommand();
        cmd1.CommandText = CreateSpellsTableSql;
        await cmd1.ExecuteNonQueryAsync();

        await using var cmd2 = connection.CreateCommand();
        cmd2.CommandText = CreateMonsterSpellsTableSql;
        await cmd2.ExecuteNonQueryAsync();

        // Migration: add UsageType column if not present
        try {
            await using var migCmd = connection.CreateCommand();
            migCmd.CommandText = "ALTER TABLE MonsterSpells ADD COLUMN UsageType INTEGER NOT NULL DEFAULT 0";
            await migCmd.ExecuteNonQueryAsync();
        } catch (SqliteException) {
            // Column already exists
        }
    }

    public async Task<Spell?> GetSpellBySlugAsync(string slug) {
        var connectionString = BuildConnectionString(_dbPath, SqliteOpenMode.ReadOnly);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Spells WHERE Slug = @Slug;";
        cmd.Parameters.AddWithValue("@Slug", slug);

        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadSpell(reader) : null;
    }

    public async Task SaveSpellAsync(Spell spell) {
        var connectionString = BuildConnectionString(_dbPath, SqliteOpenMode.ReadWrite);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = UpsertSpellSql;
        AddSpellParameters(cmd, spell);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SaveSpellsAsync(List<Spell> spells) {
        if (spells.Count == 0) return;

        var connectionString = BuildConnectionString(_dbPath, SqliteOpenMode.ReadWrite);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();
        foreach (var spell in spells) {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = UpsertSpellSql;
            AddSpellParameters(cmd, spell);
            await cmd.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
    }

    public async Task LinkSpellToMonsterAsync(string monsterSlug, string spellSlug, int slotLevel, bool isPreCast, SpellUsageType usageType, int? usesPerDay) {
        var connectionString = BuildConnectionString(_dbPath, SqliteOpenMode.ReadWrite);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO MonsterSpells (MonsterSlug, SpellSlug, SlotLevel, IsPreCast, UsageType, UsesPerDay)
            VALUES (@MonsterSlug, @SpellSlug, @SlotLevel, @IsPreCast, @UsageType, @UsesPerDay)
            ON CONFLICT(MonsterSlug, SpellSlug) DO UPDATE SET
                SlotLevel=excluded.SlotLevel, IsPreCast=excluded.IsPreCast,
                UsageType=excluded.UsageType, UsesPerDay=excluded.UsesPerDay;
            """;
        cmd.Parameters.AddWithValue("@MonsterSlug", monsterSlug);
        cmd.Parameters.AddWithValue("@SpellSlug", spellSlug);
        cmd.Parameters.AddWithValue("@SlotLevel", slotLevel);
        cmd.Parameters.AddWithValue("@IsPreCast", isPreCast ? 1 : 0);
        cmd.Parameters.AddWithValue("@UsageType", (int)usageType);
        cmd.Parameters.AddWithValue("@UsesPerDay", (object?)usesPerDay ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ClearMonsterSpellsAsync(string monsterSlug) {
        var connectionString = BuildConnectionString(_dbPath, SqliteOpenMode.ReadWrite);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM MonsterSpells WHERE MonsterSlug = @MonsterSlug;";
        cmd.Parameters.AddWithValue("@MonsterSlug", monsterSlug);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<MonsterSpellInfo>> GetSpellsForMonsterAsync(string monsterSlug) {
        var results = new List<MonsterSpellInfo>();
        var connectionString = BuildConnectionString(_dbPath, SqliteOpenMode.ReadOnly);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT s.*, ms.SlotLevel, ms.IsPreCast, ms.UsageType, ms.UsesPerDay
            FROM MonsterSpells ms
            INNER JOIN Spells s ON s.Slug = ms.SpellSlug
            WHERE ms.MonsterSlug = @MonsterSlug
            ORDER BY ms.SlotLevel, s.Name;
            """;
        cmd.Parameters.AddWithValue("@MonsterSlug", monsterSlug);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var spell = ReadSpell(reader);
            spell.ParseCombatInfo();
            var slotLevel = reader.GetInt32(reader.GetOrdinal("SlotLevel"));
            var isPreCast = reader.GetInt32(reader.GetOrdinal("IsPreCast")) != 0;
            var usageType = (SpellUsageType)reader.GetInt32(reader.GetOrdinal("UsageType"));
            var usesOrdinal = reader.GetOrdinal("UsesPerDay");
            int? usesPerDay = reader.IsDBNull(usesOrdinal) ? null : reader.GetInt32(usesOrdinal);

            results.Add(new MonsterSpellInfo {
                Spell = spell,
                SlotLevel = slotLevel,
                IsPreCast = isPreCast,
                UsageType = usageType,
                UsesPerDay = usesPerDay
            });
        }

        return results;
    }

    public async Task<int> GetSpellCountAsync() {
        var connectionString = BuildConnectionString(_dbPath, SqliteOpenMode.ReadOnly);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Spells;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static Spell ReadSpell(SqliteDataReader reader) {
        return new Spell {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Slug = reader.GetString(reader.GetOrdinal("Slug")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Level = reader.GetInt32(reader.GetOrdinal("Level")),
            School = reader.GetString(reader.GetOrdinal("School")),
            CastingTime = reader.GetString(reader.GetOrdinal("CastingTime")),
            Range = reader.GetString(reader.GetOrdinal("Range")),
            Components = reader.GetString(reader.GetOrdinal("Components")),
            Material = reader.GetString(reader.GetOrdinal("Material")),
            Duration = reader.GetString(reader.GetOrdinal("Duration")),
            Concentration = reader.GetInt32(reader.GetOrdinal("Concentration")) != 0,
            Ritual = reader.GetInt32(reader.GetOrdinal("Ritual")) != 0,
            Description = reader.GetString(reader.GetOrdinal("Description")),
            HigherLevel = reader.GetString(reader.GetOrdinal("HigherLevel")),
            Classes = reader.GetString(reader.GetOrdinal("Classes")),
            Source = reader.GetString(reader.GetOrdinal("Source"))
        };
    }

    private static void AddSpellParameters(SqliteCommand cmd, Spell spell) {
        cmd.Parameters.AddWithValue("@Slug", spell.Slug);
        cmd.Parameters.AddWithValue("@Name", spell.Name);
        cmd.Parameters.AddWithValue("@Level", spell.Level);
        cmd.Parameters.AddWithValue("@School", spell.School);
        cmd.Parameters.AddWithValue("@CastingTime", spell.CastingTime);
        cmd.Parameters.AddWithValue("@Range", spell.Range);
        cmd.Parameters.AddWithValue("@Components", spell.Components);
        cmd.Parameters.AddWithValue("@Material", spell.Material);
        cmd.Parameters.AddWithValue("@Duration", spell.Duration);
        cmd.Parameters.AddWithValue("@Concentration", spell.Concentration ? 1 : 0);
        cmd.Parameters.AddWithValue("@Ritual", spell.Ritual ? 1 : 0);
        cmd.Parameters.AddWithValue("@Description", spell.Description);
        cmd.Parameters.AddWithValue("@HigherLevel", spell.HigherLevel);
        cmd.Parameters.AddWithValue("@Classes", spell.Classes);
        cmd.Parameters.AddWithValue("@Source", spell.Source);
    }

    private static string BuildConnectionString(string filePath, SqliteOpenMode mode) {
        return new SqliteConnectionStringBuilder {
            DataSource = filePath,
            Mode = mode
        }.ToString();
    }
}
