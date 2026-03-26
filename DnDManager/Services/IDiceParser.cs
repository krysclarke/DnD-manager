using DnDManager.Models;

namespace DnDManager.Services;

public interface IDiceParser {
    (DiceExpression? expression, string? error) Parse(string input);
}
