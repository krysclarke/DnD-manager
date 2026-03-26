namespace DnDManager.Models;

public record EncounterFileData(
    List<Character> Characters,
    List<DiceRollResult> DiceHistory,
    string CampaignNotes,
    int CaretPosition);
