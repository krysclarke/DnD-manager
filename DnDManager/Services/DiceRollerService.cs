using DnDManager.Models;

namespace DnDManager.Services;

public class DiceRollerService : IDiceRoller {
    public DiceRollResult Roll(DiceExpression expression) {
        var result = new DiceRollResult {
            RawInput = expression.RawInput,
            PartResults = []
        };

        foreach (var part in expression.Parts) {
            var partResult = RollPart(part);
            result.PartResults.Add(partResult);
        }

        return result;
    }

    public DiceRollResult RollInitiative() {
        var expression = new DiceExpression {
            RawInput = "1d20",
            Parts = [
                new DiceExpressionPart {
                    Quantity = 1,
                    Sides = 20,
                    RawText = "1d20"
                }
            ]
        };

        return Roll(expression);
    }

    private static DicePartResult RollPart(DiceExpressionPart part) {
        var rolls = new int[part.Quantity];
        int[]? secondRolls = null;
        var chosenRolls = new int[part.Quantity];

        if (part.HasAdvantage || part.HasDisadvantage) {
            secondRolls = new int[part.Quantity];

            for (var i = 0; i < part.Quantity; i++) {
                rolls[i] = RollDie(part.Sides);
                secondRolls[i] = RollDie(part.Sides);

                chosenRolls[i] = part.HasAdvantage
                    ? Math.Max(rolls[i], secondRolls[i])
                    : Math.Min(rolls[i], secondRolls[i]);
            }
        } else if (part.HalflingLuck) {
            for (var i = 0; i < part.Quantity; i++) {
                rolls[i] = RollDie(part.Sides);

                if (rolls[i] == 1) {
                    chosenRolls[i] = RollDie(part.Sides);
                } else {
                    chosenRolls[i] = rolls[i];
                }
            }
        } else {
            for (var i = 0; i < part.Quantity; i++) {
                rolls[i] = RollDie(part.Sides);
                chosenRolls[i] = rolls[i];
            }
        }

        return new DicePartResult {
            Part = part,
            Rolls = rolls,
            SecondRolls = secondRolls,
            ChosenRolls = chosenRolls,
            Modifier = part.Modifier,
            Sides = part.Sides
        };
    }

    private static int RollDie(int sides) {
        return Random.Shared.Next(1, sides + 1);
    }
}
