namespace DnDManager.Models;

public class Character {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CharacterType CharacterType { get; set; }
    public int? Initiative { get; set; }
    public int ArmorClass { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
