using System.Text.RegularExpressions;

namespace DnDManager.Models;

/// <summary>
/// Catalog of the standard D&D 5e conditions plus the imposed-condition
/// dependency rules and helpers for the comma-separated storage format.
/// </summary>
public static partial class Conditions5e {
    public const string Exhaustion = "Exhaustion";
    public const int MaxExhaustionLevel = 6;

    /// <summary>All conditions, in display order. Exhaustion carries a level (1-6).</summary>
    public static readonly IReadOnlyList<string> All = new[] {
        "Blinded", "Charmed", "Deafened", Exhaustion, "Frightened",
        "Grappled", "Incapacitated", "Invisible", "Paralyzed", "Petrified",
        "Poisoned", "Prone", "Restrained", "Stunned", "Unconscious"
    };

    /// <summary>Maps a condition to the conditions it imposes on the creature.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> ImposeMap =
        new Dictionary<string, string[]> {
            ["Paralyzed"] = new[] { "Incapacitated" },
            ["Petrified"] = new[] { "Incapacitated" },
            ["Stunned"] = new[] { "Incapacitated" },
            ["Unconscious"] = new[] { "Incapacitated", "Prone" }
        };

    [GeneratedRegex(@"^Exhaustion\s*:?\s*(\d+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex ExhaustionRegex();

    /// <summary>
    /// Parses the stored comma-separated string into the set of selected
    /// (non-exhaustion) conditions and the exhaustion level (0 if absent).
    /// </summary>
    public static (HashSet<string> Selected, int ExhaustionLevel) Parse(string? raw) {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var exhaustionLevel = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return (selected, exhaustionLevel);

        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            var match = ExhaustionRegex().Match(token);
            if (match.Success) {
                exhaustionLevel = match.Groups[1].Success
                    ? Math.Clamp(int.Parse(match.Groups[1].Value), 1, MaxExhaustionLevel)
                    : 1;
                continue;
            }
            var canonical = All.FirstOrDefault(c =>
                string.Equals(c, token, StringComparison.OrdinalIgnoreCase));
            if (canonical is not null && canonical != Exhaustion)
                selected.Add(canonical);
        }

        return (selected, exhaustionLevel);
    }

    /// <summary>
    /// Builds the canonical comma-separated string from the active conditions
    /// (catalog order), inserting "Exhaustion N" when a level is set.
    /// </summary>
    public static string Serialize(IEnumerable<string> activeConditions, int exhaustionLevel) {
        var active = new HashSet<string>(activeConditions, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();
        foreach (var name in All) {
            if (name == Exhaustion) {
                if (exhaustionLevel > 0)
                    ordered.Add($"{Exhaustion} {Math.Clamp(exhaustionLevel, 1, MaxExhaustionLevel)}");
            } else if (active.Contains(name)) {
                ordered.Add(name);
            }
        }
        return string.Join(", ", ordered);
    }

    /// <summary>Returns every condition imposed by the given selected conditions.</summary>
    public static HashSet<string> ResolveImposed(IEnumerable<string> selectedBase) {
        var imposed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in selectedBase) {
            if (ImposeMap.TryGetValue(name, out var children)) {
                foreach (var child in children)
                    imposed.Add(child);
            }
        }
        return imposed;
    }

    /// <summary>Returns which selected conditions impose the given condition.</summary>
    public static IReadOnlyList<string> ImposedSourcesFor(string condition, IEnumerable<string> selectedBase) {
        return selectedBase
            .Where(b => ImposeMap.TryGetValue(b, out var children)
                        && children.Contains(condition, StringComparer.OrdinalIgnoreCase))
            .ToList();
    }
}
