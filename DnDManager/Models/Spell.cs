using System.Text.RegularExpressions;

namespace DnDManager.Models;

public enum SpellDelivery {
    None,
    MeleeSpellAttack,
    RangedSpellAttack,
    SavingThrow
}

public class Spell {
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string School { get; set; } = string.Empty;
    public string CastingTime { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public string Components { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public bool Concentration { get; set; }
    public bool Ritual { get; set; }
    public string Description { get; set; } = string.Empty;
    public string HigherLevel { get; set; } = string.Empty;
    public string Classes { get; set; } = string.Empty;
    public string Source { get; set; } = "Open5e";

    // Parsed combat info (derived from Description/HigherLevel, not stored in DB)
    public SpellDelivery Delivery { get; set; }
    public string SaveAbility { get; set; } = string.Empty;
    public string DamageDice { get; set; } = string.Empty;
    public string SpellDamageType { get; set; } = string.Empty;

    // Upcast scaling (parsed from HigherLevel)
    public string ScalingDice { get; set; } = string.Empty;
    public int ScalingInterval { get; set; } = 1;
    public bool HasDamageScaling => !string.IsNullOrEmpty(ScalingDice);
    public bool HasDamage => !string.IsNullOrEmpty(DamageDice);
    public bool IsCantrip => Level == 0;

    public string LevelDisplay => Level == 0 ? "Cantrip" : $"Level {Level}";

    public string CombatSummary {
        get {
            var parts = new List<string>();
            switch (Delivery) {
                case SpellDelivery.MeleeSpellAttack:
                    parts.Add("Melee spell attack");
                    break;
                case SpellDelivery.RangedSpellAttack:
                    parts.Add("Ranged spell attack");
                    break;
                case SpellDelivery.SavingThrow when !string.IsNullOrEmpty(SaveAbility):
                    parts.Add($"{SaveAbility} save");
                    break;
            }
            if (!string.IsNullOrEmpty(DamageDice) && !string.IsNullOrEmpty(SpellDamageType))
                parts.Add($"{DamageDice} {SpellDamageType}");
            else if (!string.IsNullOrEmpty(DamageDice))
                parts.Add(DamageDice);
            return string.Join(" | ", parts);
        }
    }

    public string ShortDescription {
        get {
            var firstPeriod = Description.IndexOf(". ", StringComparison.Ordinal);
            if (firstPeriod > 0 && firstPeriod < 150)
                return Description[..(firstPeriod + 1)];
            return Description.Length > 150 ? Description[..147] + "..." : Description;
        }
    }

    /// <summary>
    /// Compute the damage dice expression for a given cast level and caster level.
    /// For leveled spells: base + (castLevel - spellLevel) / interval * scalingDice.
    /// For cantrips: scales with caster level thresholds (5, 11, 17).
    /// </summary>
    public string GetDamageDiceAtLevel(int castLevel, int casterLevel) {
        if (string.IsNullOrEmpty(DamageDice)) return string.Empty;

        if (IsCantrip)
            return GetCantripDamageDice(casterLevel);

        if (!HasDamageScaling || castLevel <= Level)
            return DamageDice;

        var levelsAbove = castLevel - Level;
        var extraSets = ScalingInterval > 0 ? levelsAbove / ScalingInterval : 0;
        if (extraSets <= 0) return DamageDice;

        return CombineDice(DamageDice, ScalingDice, extraSets);
    }

    private string GetCantripDamageDice(int casterLevel) {
        // Cantrips scale at caster levels 5, 11, 17
        // Parse base dice count and die size from DamageDice (e.g. "1d10")
        var match = Regex.Match(DamageDice, @"(\d+)d(\d+)(.*)");
        if (!match.Success) return DamageDice;

        var dieSize = match.Groups[2].Value;
        var suffix = match.Groups[3].Value; // captures "+N" if present

        var diceCount = casterLevel switch {
            >= 17 => 4,
            >= 11 => 3,
            >= 5 => 2,
            _ => 1
        };

        return $"{diceCount}d{dieSize}{suffix}";
    }

    private static string CombineDice(string baseDice, string scalingDice, int extraSets) {
        // Parse both dice expressions: "8d6" + "1d6" * 3 = "11d6"
        var baseMatch = Regex.Match(baseDice, @"(\d+)d(\d+)(.*)");
        var scaleMatch = Regex.Match(scalingDice, @"(\d+)d(\d+)");

        if (!baseMatch.Success || !scaleMatch.Success) {
            // Can't combine — just concatenate
            var parts = new List<string> { baseDice };
            for (var i = 0; i < extraSets; i++)
                parts.Add(scalingDice);
            return string.Join("+", parts);
        }

        var baseCount = int.Parse(baseMatch.Groups[1].Value);
        var baseDie = baseMatch.Groups[2].Value;
        var baseSuffix = baseMatch.Groups[3].Value;
        var scaleCount = int.Parse(scaleMatch.Groups[1].Value);
        var scaleDie = scaleMatch.Groups[2].Value;

        if (baseDie == scaleDie) {
            // Same die size — add dice counts
            return $"{baseCount + scaleCount * extraSets}d{baseDie}{baseSuffix}";
        }

        // Different die sizes — append separately
        return $"{baseDice}+{scaleCount * extraSets}d{scaleDie}";
    }

    public void ParseCombatInfo() {
        // Spell attack detection
        if (Regex.IsMatch(Description, @"make a melee spell attack", RegexOptions.IgnoreCase))
            Delivery = SpellDelivery.MeleeSpellAttack;
        else if (Regex.IsMatch(Description, @"make a ranged spell attack", RegexOptions.IgnoreCase))
            Delivery = SpellDelivery.RangedSpellAttack;
        else if (Regex.IsMatch(Description, @"(?:must (?:make|succeed on) a|make a) (\w+) saving throw", RegexOptions.IgnoreCase)) {
            Delivery = SpellDelivery.SavingThrow;
            var saveMatch = Regex.Match(Description, @"(?:must (?:make|succeed on) a|make a) (\w+) saving throw", RegexOptions.IgnoreCase);
            if (saveMatch.Success)
                SaveAbility = CapitalizeFirst(saveMatch.Groups[1].Value);
        }

        // Damage detection: "XdY [+Z] <type> damage"
        var dmgMatch = Regex.Match(Description, @"(\d+d\d+(?:\s*\+\s*\d+)?)\s+(\w+)\s+damage", RegexOptions.IgnoreCase);
        if (dmgMatch.Success) {
            DamageDice = dmgMatch.Groups[1].Value.Replace(" ", "");
            var typeStr = dmgMatch.Groups[2].Value.ToLowerInvariant();
            if (IsDamageType(typeStr))
                SpellDamageType = CapitalizeFirst(typeStr);
            else
                SpellDamageType = string.Empty;
        }

        // Upcast scaling from HigherLevel text
        ParseScaling();
    }

    private void ParseScaling() {
        if (string.IsNullOrEmpty(HigherLevel)) return;

        // Pattern: "damage increases by XdY for each slot level above Nth"
        var match = Regex.Match(HigherLevel,
            @"damage increases by (\d+d\d+) for each slot level above",
            RegexOptions.IgnoreCase);
        if (match.Success) {
            ScalingDice = match.Groups[1].Value;
            ScalingInterval = 1;
            return;
        }

        // Pattern: "damage increases by XdY for every two slot levels above"
        var twoMatch = Regex.Match(HigherLevel,
            @"damage increases by (\d+d\d+) for every two slot levels above",
            RegexOptions.IgnoreCase);
        if (twoMatch.Success) {
            ScalingDice = twoMatch.Groups[1].Value;
            ScalingInterval = 2;
        }
    }

    private static bool IsDamageType(string s) =>
        s is "acid" or "bludgeoning" or "cold" or "fire" or "force" or "lightning"
            or "necrotic" or "piercing" or "poison" or "psychic" or "radiant"
            or "slashing" or "thunder";

    private static string CapitalizeFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLowerInvariant();
}
