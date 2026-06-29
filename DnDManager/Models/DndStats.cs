using System.Globalization;

namespace DnDManager.Models;

/// <summary>
/// Static reference data and helpers for D&D 5e ability scores and skills.
/// </summary>
public static class DndStats {
    /// <summary>Three-letter ability codes in canonical order.</summary>
    public static readonly IReadOnlyList<string> AbilityCodes = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];

    /// <summary>Skill names in display order, each mapped to its default ability code.</summary>
    public static readonly IReadOnlyList<string> Skills = [
        "Acrobatics", "Animal Handling", "Arcana", "Athletics", "Deception",
        "History", "Insight", "Intimidation", "Investigation", "Medicine",
        "Nature", "Perception", "Performance", "Persuasion", "Religion",
        "Sleight of Hand", "Stealth", "Survival"
    ];

    private static readonly Dictionary<string, string> SkillToAbility = new(StringComparer.OrdinalIgnoreCase) {
        ["Acrobatics"] = "DEX",
        ["Animal Handling"] = "WIS",
        ["Arcana"] = "INT",
        ["Athletics"] = "STR",
        ["Deception"] = "CHA",
        ["History"] = "INT",
        ["Insight"] = "WIS",
        ["Intimidation"] = "CHA",
        ["Investigation"] = "INT",
        ["Medicine"] = "WIS",
        ["Nature"] = "INT",
        ["Perception"] = "WIS",
        ["Performance"] = "CHA",
        ["Persuasion"] = "CHA",
        ["Religion"] = "INT",
        ["Sleight of Hand"] = "DEX",
        ["Stealth"] = "DEX",
        ["Survival"] = "WIS"
    };

    /// <summary>Default ability code for a skill, or "INT" if unknown.</summary>
    public static string DefaultAbilityFor(string skill) =>
        SkillToAbility.GetValueOrDefault(skill, "INT");

    /// <summary>D&D ability modifier (floored) for a raw score.</summary>
    public static int Modifier(int score) =>
        (int)Math.Floor((score - 10) / 2.0);

    /// <summary>Formats a signed modifier, e.g. "+3" or "-1".</summary>
    public static string FormatSigned(int value) =>
        value >= 0 ? $"+{value}" : value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Proficiency bonus derived from a challenge rating string (e.g. "1/4", "5", "17").</summary>
    public static int ProficiencyBonusForCr(string? challengeRating) {
        var cr = ParseChallengeRating(challengeRating);
        // CR 0-4 -> +2, then +1 per additional 4 CR.
        return 2 + (int)Math.Max(0, Math.Ceiling((cr - 4) / 4.0));
    }

    private static double ParseChallengeRating(string? cr) {
        if (string.IsNullOrWhiteSpace(cr)) return 0;
        cr = cr.Trim();
        if (cr.Contains('/')) {
            var parts = cr.Split('/', 2);
            if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var num)
                && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var den)
                && den != 0) {
                return num / den;
            }
            return 0;
        }
        return double.TryParse(cr, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}

/// <summary>How a d20 check/save should be rolled.</summary>
public enum D20RollMode {
    Normal,
    Advantage,
    Disadvantage
}
