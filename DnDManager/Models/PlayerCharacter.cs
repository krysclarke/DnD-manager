namespace DnDManager.Models;

public class PlayerCharacter : Character {
    public string PlayerName { get; set; } = string.Empty;
    public int PassivePerception { get; set; }
    public int PassiveInvestigation { get; set; }

    public PlayerCharacter() {
        CharacterType = CharacterType.PC;
    }
}
