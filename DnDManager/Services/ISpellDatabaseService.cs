using DnDManager.Models;

namespace DnDManager.Services;

public interface ISpellDatabaseService {
    Task InitializeAsync(string dbPath);
    Task<Spell?> GetSpellBySlugAsync(string slug);
    Task SaveSpellAsync(Spell spell);
    Task SaveSpellsAsync(List<Spell> spells);
    Task LinkSpellToMonsterAsync(string monsterSlug, string spellSlug, int slotLevel, bool isPreCast, SpellUsageType usageType, int? usesPerDay);
    Task ClearMonsterSpellsAsync(string monsterSlug);
    Task<List<MonsterSpellInfo>> GetSpellsForMonsterAsync(string monsterSlug);
    Task<int> GetSpellCountAsync();
}
