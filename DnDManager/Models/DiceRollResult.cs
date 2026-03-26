namespace DnDManager.Models;

public class DiceRollResult {
    public string RawInput { get; set; } = string.Empty;
    public List<DicePartResult> PartResults { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsValid { get; set; } = true;
    public string? ErrorReason { get; set; }
}
