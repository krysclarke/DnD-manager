using DnDManager.Models;

namespace DnDManager.Services;

public interface ICampaignRepository {
    Task InitializeAsync(string dbPath);
    Task SaveCharactersAsync(List<Character> characters);
    Task<List<Character>> LoadCharactersAsync();
    Task SaveEncounterStateAsync(EncounterState state);
    Task<EncounterState> LoadEncounterStateAsync();
    Task SaveDiceHistoryAsync(List<DiceRollResult> history);
    Task<List<DiceRollResult>> LoadDiceHistoryAsync();
    Task SaveCampaignNotesAsync(string content, int caretPosition);
    Task<(string content, int caretPosition)> LoadCampaignNotesAsync();
    Task SaveSettingAsync(string key, string value);
    Task<string?> LoadSettingAsync(string key);
}
