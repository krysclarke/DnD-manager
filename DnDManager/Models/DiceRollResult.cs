using System.Text.Json;

namespace DnDManager.Models;

public class DiceRollResult {
    public string RawInput { get; set; } = string.Empty;
    public List<DicePartResult> PartResults { get; set; } = [];
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsValid { get; set; } = true;
    public string? ErrorReason { get; set; }
    public string? Context { get; set; }

    // Reconstructs a result from the persisted ResultData column. New rows store
    // the whole result (JSON object); legacy rows stored only PartResults (JSON array).
    public static DiceRollResult FromStoredJson(string json) {
        var trimmed = json.TrimStart();
        if (trimmed.StartsWith('[')) {
            return new DiceRollResult {
                PartResults = JsonSerializer.Deserialize<List<DicePartResult>>(json) ?? []
            };
        }
        return JsonSerializer.Deserialize<DiceRollResult>(json) ?? new DiceRollResult();
    }
}
