using System.Text.RegularExpressions;
using DnDManager.Models;

namespace DnDManager.Services;

public record ParsedSpellReference(string SpellName, int SlotLevel, bool IsPreCast, SpellUsageType UsageType, int? UsesPerDay);

public record ParsedSpellcastingInfo(
    List<ParsedSpellReference> Spells,
    List<SpellSlotLevel> SlotLevels,
    int SpellSaveDc,
    int SpellAttackBonus,
    int CasterLevel);

public static partial class SpellcastingParser {
    public static ParsedSpellcastingInfo Parse(List<NamedAbility> specialAbilities) {
        var allSpells = new List<ParsedSpellReference>();
        var allSlots = new List<SpellSlotLevel>();
        var saveDc = 0;
        var attackBonus = 0;
        var casterLevel = 0;

        foreach (var ability in specialAbilities) {
            if (!ability.Name.Contains("Spellcasting", StringComparison.OrdinalIgnoreCase))
                continue;

            // Parse caster level from preamble (e.g. "18th-level spellcaster")
            var lvlMatch = CasterLevelRegex().Match(ability.Description);
            if (lvlMatch.Success && int.TryParse(lvlMatch.Groups[1].Value, out var cl))
                casterLevel = cl;

            // Parse save DC and attack bonus from preamble
            var dcMatch = SpellSaveDcRegex().Match(ability.Description);
            if (dcMatch.Success && int.TryParse(dcMatch.Groups[1].Value, out var dc))
                saveDc = dc;

            var atkMatch = SpellAttackBonusRegex().Match(ability.Description);
            if (atkMatch.Success && int.TryParse(atkMatch.Groups[1].Value, out var atk))
                attackBonus = atk;

            var isInnate = ability.Name.Contains("Innate", StringComparison.OrdinalIgnoreCase);
            if (isInnate) {
                allSpells.AddRange(ParseInnateSpellcasting(ability.Description));
            } else {
                var (spells, slots) = ParseRegularSpellcasting(ability.Description);
                allSpells.AddRange(spells);
                allSlots.AddRange(slots);
            }
        }

        return new ParsedSpellcastingInfo(allSpells, allSlots, saveDc, attackBonus, casterLevel);
    }

    private static (List<ParsedSpellReference> Spells, List<SpellSlotLevel> Slots) ParseRegularSpellcasting(string description) {
        var spells = new List<ParsedSpellReference>();
        var slots = new List<SpellSlotLevel>();

        // Parse "can cast X and Y at will" from preamble
        var atWillPreamble = AtWillPreambleRegex().Match(description);
        if (atWillPreamble.Success) {
            var spellText = atWillPreamble.Groups[1].Value;
            foreach (var name in SplitSpellList(spellText, splitOnAnd: true))
                spells.Add(new ParsedSpellReference(name.Name, 0, name.IsPreCast, SpellUsageType.AtWill, null));
        }

        // Parse spell level lines: "Cantrips (at will): ..." and "1st level (4 slots): ..."
        foreach (Match match in SpellLevelLineRegex().Matches(description)) {
            var levelText = match.Groups[1].Value;
            var parenthetical = match.Groups[3].Value;
            var spellList = match.Groups[4].Value;

            var isCantrip = levelText.StartsWith("Cantrip", StringComparison.OrdinalIgnoreCase);
            var level = isCantrip ? 0 : int.TryParse(match.Groups[2].Value, out var l) ? l : 0;

            // Parse slot count from parenthetical, e.g. "4 slots" or "at will"
            if (!isCantrip) {
                var slotMatch = SlotCountRegex().Match(parenthetical);
                if (slotMatch.Success && int.TryParse(slotMatch.Groups[1].Value, out var slotCount)) {
                    slots.Add(new SpellSlotLevel { Level = level, TotalSlots = slotCount });
                }
            }

            var usageType = isCantrip ? SpellUsageType.AtWill : SpellUsageType.SlotBased;

            foreach (var spell in SplitSpellList(spellList, splitOnAnd: false))
                spells.Add(new ParsedSpellReference(spell.Name, level, spell.IsPreCast, usageType, null));
        }

        return (spells, slots);
    }

    private static List<ParsedSpellReference> ParseInnateSpellcasting(string description) {
        var results = new List<ParsedSpellReference>();

        // Parse lines like "At will: detect magic, detect evil and good"
        // and "3/day each: counterspell, dispel magic"
        foreach (Match match in InnateSpellLineRegex().Matches(description)) {
            var usesText = match.Groups[1].Value;
            var spellList = match.Groups[2].Value;

            var isAtWill = usesText.Equals("At will", StringComparison.OrdinalIgnoreCase);
            int? usesPerDay = isAtWill ? null : int.TryParse(usesText, out var u) ? u : null;
            var usageType = isAtWill ? SpellUsageType.AtWill : SpellUsageType.InnatePerDay;

            foreach (var spell in SplitSpellList(spellList, splitOnAnd: false))
                results.Add(new ParsedSpellReference(spell.Name, 0, spell.IsPreCast, usageType, usesPerDay));
        }

        return results;
    }

    private static List<(string Name, bool IsPreCast)> SplitSpellList(string spellList, bool splitOnAnd) {
        var results = new List<(string Name, bool IsPreCast)>();

        // Primary split on comma
        var parts = spellList.Split(',');

        foreach (var part in parts) {
            var trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (splitOnAnd) {
                // For preamble "X and Y", split on " and "
                var andParts = trimmed.Split(" and ", StringSplitOptions.RemoveEmptyEntries);
                foreach (var ap in andParts)
                    AddSpellName(results, ap.Trim());
            } else {
                AddSpellName(results, trimmed);
            }
        }

        return results;
    }

    private static void AddSpellName(List<(string Name, bool IsPreCast)> results, string raw) {
        if (string.IsNullOrWhiteSpace(raw)) return;

        // Strip trailing asterisks (pre-cast marker)
        var isPreCast = raw.EndsWith('*');
        var name = raw.TrimEnd('*').Trim();

        // Strip leading bullet markers
        name = name.TrimStart('*', '\u2022', '-', '\u2013').Trim();

        if (!string.IsNullOrEmpty(name))
            results.Add((name, isPreCast));
    }

    public static string DeriveSpellSlug(string spellName) {
        // "Mage Armor" -> "mage-armor", "Tasha's Hideous Laughter" -> "tashas-hideous-laughter"
        var slug = spellName.ToLowerInvariant()
            .Replace("'", "")
            .Replace("\u2019", "");
        slug = NonAlphanumericRegex().Replace(slug, "-");
        slug = MultipleHyphenRegex().Replace(slug, "-");
        return slug.Trim('-');
    }

    [GeneratedRegex(@"(\d+)\w*-level spellcaster", RegexOptions.IgnoreCase)]
    private static partial Regex CasterLevelRegex();

    [GeneratedRegex(@"spell save DC (\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex SpellSaveDcRegex();

    [GeneratedRegex(@"\+(\d+) to hit with spell attacks", RegexOptions.IgnoreCase)]
    private static partial Regex SpellAttackBonusRegex();

    [GeneratedRegex(@"can cast (.+?) at will", RegexOptions.IgnoreCase)]
    private static partial Regex AtWillPreambleRegex();

    [GeneratedRegex(@"(?:\*\s*)?(Cantrips|(\d+)\w*\s+level)\s*\(([^)]*)\)\s*:\s*(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex SpellLevelLineRegex();

    [GeneratedRegex(@"(\d+)\s*slot")]
    private static partial Regex SlotCountRegex();

    [GeneratedRegex(@"(At will|\d+)/day(?:\s+each)?\s*:\s*(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex InnateSpellLineRegex();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphenRegex();
}
