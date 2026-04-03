namespace DnDManager.Models;

public class NonPlayerCharacter : Character {
    public int MaxHitPoints { get; set; }
    public int CurrentHitPoints { get; set; }
    public int? BestiaryEntryId { get; set; }
    public int? InitiativeModifier { get; set; }
    public List<NamedAbility> SpecialAbilities { get; set; } = [];
    public List<NamedAbility> NonAttackActions { get; set; } = [];
    public string MultiattackDescription { get; set; } = string.Empty;
    public List<Attack> Attacks { get; set; } = [];
    public List<NamedAbility> LegendaryActions { get; set; } = [];
    public string LegendaryDescription { get; set; } = string.Empty;
    public List<NamedAbility> Reactions { get; set; } = [];
    public List<NamedAbility> BonusActions { get; set; } = [];
    public List<MonsterSpellInfo> Spells { get; set; } = [];
    public List<SpellSlotLevel> SpellSlots { get; set; } = [];
    public int SpellSaveDc { get; set; }
    public int SpellAttackBonus { get; set; }
    public int CasterLevel { get; set; }
    public int LegendaryActionBudget { get; set; }
    public int LegendaryActionsUsed { get; set; }
    public bool ReactionUsed { get; set; }

    public NonPlayerCharacter() {
        CharacterType = CharacterType.NPC;
    }

    public void ParseLegendaryActionBudget() {
        if (string.IsNullOrEmpty(LegendaryDescription)) {
            LegendaryActionBudget = LegendaryActions.Count > 0 ? 3 : 0;
            return;
        }
        var match = System.Text.RegularExpressions.Regex.Match(
            LegendaryDescription, @"(\d+)\s+legendary\s+action",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        LegendaryActionBudget = match.Success && int.TryParse(match.Groups[1].Value, out var budget)
            ? budget : 3;
    }
}