namespace DnDManager.Models;

public class BestiaryEntry {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ArmorClass { get; set; }
    public string ArmorDescription { get; set; } = string.Empty;
    public int HitPoints { get; set; }
    public string HitDice { get; set; } = string.Empty;
    public CreatureSize Size { get; set; } = CreatureSize.Medium;
    public string Type { get; set; } = string.Empty;
    public string Subtype { get; set; } = string.Empty;
    public string Alignment { get; set; } = string.Empty;
    public string ChallengeRating { get; set; } = string.Empty;
    public string Speed { get; set; } = string.Empty;
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;
    public string Senses { get; set; } = string.Empty;
    public string Languages { get; set; } = string.Empty;
    public string SpecialAbilitiesJson { get; set; } = string.Empty;
    public List<NamedAbility> SpecialAbilities { get; set; } = [];
    public List<NamedAbility> NonAttackActions { get; set; } = [];
    public string MultiattackDescription { get; set; } = string.Empty;
    public List<Attack> Attacks { get; set; } = [];
    public List<NamedAbility> LegendaryActions { get; set; } = [];
    public string LegendaryDescription { get; set; } = string.Empty;
    public List<NamedAbility> Reactions { get; set; } = [];
    public List<NamedAbility> BonusActions { get; set; } = [];
    public int? InitiativeModifier { get; set; }
    public int EffectiveInitiativeModifier => InitiativeModifier ?? (Dexterity - 10) / 2;
    public string Source { get; set; } = "Manual";
    public string? Open5eSlug { get; set; }
}
