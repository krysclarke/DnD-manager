namespace DnDManager.Models;

public class DiceExpressionPart {
    public int Quantity { get; set; } = 1;
    public int Sides { get; set; }
    public int Modifier { get; set; }
    public bool HasAdvantage { get; set; }
    public bool HasDisadvantage { get; set; }
    public bool HalflingLuck { get; set; }
    public string RawText { get; set; } = string.Empty;
}
