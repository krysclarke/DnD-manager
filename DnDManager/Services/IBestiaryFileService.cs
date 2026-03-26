using DnDManager.Models;

namespace DnDManager.Services;

public interface IBestiaryFileService {
    Task InitializeMasterAsync(string masterDbPath);
    Task<List<BestiaryEntry>> LoadEntriesAsync();
    Task<List<BestiaryEntry>> SearchEntriesAsync(string searchTerm);
    Task SaveEntryAsync(BestiaryEntry entry);
    Task DeleteEntryAsync(int entryId);
    Task<int> GetEntryCountAsync();
    Task ImportEntriesAsync(List<BestiaryEntry> entries, ImportDuplicateMode mode);
    Task<List<BestiaryEntry>> LoadEntriesFromFileAsync(string filePath);
    Task<List<string>> FindDuplicateNamesAsync(List<BestiaryEntry> entries);
}
