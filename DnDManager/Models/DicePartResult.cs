namespace DnDManager.Models;

public class DicePartResult {
    public DiceExpressionPart Part { get; set; } = null!;
    public int[] Rolls { get; set; } = [];
    public int[]? SecondRolls { get; set; }
    public int[] ChosenRolls { get; set; } = [];
    public int Modifier { get; set; }
    public int Sides { get; set; }

    public int Total => ChosenRolls.Sum() + Modifier;
}
