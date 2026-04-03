using DnDManager.Models;

namespace DnDManager.Services;

public interface IOpen5eApiClient {
    Task<Open5eSearchResult> SearchMonstersAsync(string query, int page = 1, int pageSize = 20);
    Task<BestiaryEntry> GetMonsterAsync(string slug);
    BestiaryEntry MapToBestiaryEntry(System.Text.Json.JsonElement monster);
    Task<Spell?> GetSpellAsync(string slug);
    Task<Spell?> SearchSpellByNameAsync(string name);
}

public class Open5eSearchResult {
    public int TotalCount { get; set; }
    public List<Open5eMonsterPreview> Results { get; set; } = [];
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
