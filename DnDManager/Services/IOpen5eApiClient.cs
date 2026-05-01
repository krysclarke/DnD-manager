using DnDManager.Models;

namespace DnDManager.Services;

public interface IOpen5eApiClient {
    Task<Open5eSearchResult> SearchMonstersAsync(string query, int page = 1, int pageSize = 20);
    Task<Open5eSearchResult> SearchAllMonstersAsync(string query);
    Task<BestiaryEntry> GetMonsterAsync(string slug);
    BestiaryEntry MapToBestiaryEntry(System.Text.Json.JsonElement monster);
    Task<Spell?> GetSpellAsync(string slug);
    Task<Spell?> SearchSpellByNameAsync(string name);
    Task<IReadOnlyList<Open5eDocument>> GetDocumentsAsync();
}

public class Open5eSearchResult {
    public int TotalCount { get; set; }
    public List<Open5eMonsterPreview> Results { get; set; } = [];
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
