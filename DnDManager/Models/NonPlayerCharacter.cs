using System.Text.Json;

namespace DnDManager.Models;

public class NonPlayerCharacter : Character {
    public int MaxHitPoints { get; set; }
    public int CurrentHitPoints { get; set; }
    public int? BestiaryEntryId { get; set; }
    public int? InitiativeModifier { get; set; }
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Constitution { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    public int Wisdom { get; set; } = 10;
    public int Charisma { get; set; } = 10;
    public int ProficiencyBonus { get; set; } = 2;
    /// <summary>Proficient/overridden saving throws: ability code ("STR"…) → total save bonus.</summary>
    public Dictionary<string, int> SavingThrows { get; set; } = [];
    /// <summary>Skill proficiencies: skill display name → total skill bonus.</summary>
    public Dictionary<string, int> SkillProficiencies { get; set; } = [];

    /// <summary>Ability score for a three-letter ability code ("STR"…).</summary>
    public int AbilityScore(string code) => code switch {
        "STR" => Strength,
        "DEX" => Dexterity,
        "CON" => Constitution,
        "INT" => Intelligence,
        "WIS" => Wisdom,
        "CHA" => Charisma,
        _ => 10
    };

    /// <summary>Serializes the stat block (scores, PB, saves, skills) to a compact JSON blob.</summary>
    public string SerializeStats() => JsonSerializer.Serialize(new NpcStatsSnapshot {
        Str = Strength, Dex = Dexterity, Con = Constitution,
        Int = Intelligence, Wis = Wisdom, Cha = Charisma,
        Pb = ProficiencyBonus, Saves = SavingThrows, Skills = SkillProficiencies
    });

    /// <summary>Applies a stat block previously produced by <see cref="SerializeStats"/>.</summary>
    public void ApplyStats(string? json) {
        if (string.IsNullOrWhiteSpace(json)) return;
        NpcStatsSnapshot? snapshot;
        try {
            snapshot = JsonSerializer.Deserialize<NpcStatsSnapshot>(json);
        } catch (JsonException) {
            return;
        }
        if (snapshot is null) return;
        Strength = snapshot.Str;
        Dexterity = snapshot.Dex;
        Constitution = snapshot.Con;
        Intelligence = snapshot.Int;
        Wisdom = snapshot.Wis;
        Charisma = snapshot.Cha;
        ProficiencyBonus = snapshot.Pb;
        SavingThrows = snapshot.Saves ?? [];
        SkillProficiencies = snapshot.Skills ?? [];
    }
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