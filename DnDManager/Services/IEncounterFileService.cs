using DnDManager.Models;

namespace DnDManager.Services;

public interface IEncounterFileService {
    Task SaveCharactersToFileAsync(string filePath, List<Character> characters, CharacterType? filterType = null);
    Task<List<Character>> LoadCharactersFromFileAsync(string filePath);
    Task SaveEncounterToFileAsync(string filePath, List<Character> characters,
        List<DiceRollResult> diceHistory, string campaignNotes, int caretPosition);
    Task<EncounterFileData> LoadEncounterFromFileAsync(string filePath);
}
