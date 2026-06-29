namespace DnDManager.Models;

public class DiceExpression {
    public string RawInput { get; set; } = string.Empty;
    public List<DiceExpressionPart> Parts { get; set; } = [];
    public string? Context { get; set; }
}
