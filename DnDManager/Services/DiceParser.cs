using System.Text.RegularExpressions;
using DnDManager.Models;

namespace DnDManager.Services;

public partial class DiceParser : IDiceParser {
    private static readonly HashSet<int> ValidSides = [2, 3, 4, 6, 8, 10, 12, 20, 100];

    [GeneratedRegex(@"^(\d+)?(h)?d(\d+)([><])?([+-]\d+)?$")]
    private static partial Regex DiceRegex();

    public (DiceExpression? expression, string? error) Parse(string input) {
        var stripped = input.Replace(" ", "").Replace("\t", "");

        if (string.IsNullOrEmpty(stripped)) {
            return (null, "no dice expression");
        }

        var rawParts = stripped.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (rawParts.Length == 0) {
            return (null, "no dice expression");
        }

        var expression = new DiceExpression {
            RawInput = input,
            Parts = []
        };

        foreach (var rawPart in rawParts) {
            var match = DiceRegex().Match(rawPart);

            if (!match.Success) {
                return (null, "invalid dice notation");
            }

            var quantityStr = match.Groups[1].Value;
            var halflingFlag = match.Groups[2].Value;
            var sidesStr = match.Groups[3].Value;
            var advantageFlag = match.Groups[4].Value;
            var modifierStr = match.Groups[5].Value;

            var quantity = string.IsNullOrEmpty(quantityStr) ? 1 : int.Parse(quantityStr);
            var sides = int.Parse(sidesStr);
            var modifier = string.IsNullOrEmpty(modifierStr) ? 0 : int.Parse(modifierStr);
            var hasHalflingLuck = halflingFlag == "h";
            var hasAdvantage = advantageFlag == ">";
            var hasDisadvantage = advantageFlag == "<";

            if (!ValidSides.Contains(sides)) {
                return (null, "invalid dice size");
            }

            if (hasHalflingLuck && sides != 20) {
                return (null, "Halfling Luck only applies to d20");
            }

            if ((hasAdvantage || hasDisadvantage) && sides != 20) {
                return (null, "advantage/disadvantage only applies to d20");
            }

            expression.Parts.Add(new DiceExpressionPart {
                Quantity = quantity,
                Sides = sides,
                Modifier = modifier,
                HasAdvantage = hasAdvantage,
                HasDisadvantage = hasDisadvantage,
                HalflingLuck = hasHalflingLuck,
                RawText = rawPart
            });
        }

        return (expression, null);
    }
}
