namespace DnDManager.Models;

/// <summary>Compact persistence DTO for an NPC's stat block (ability scores, proficiency, saves, skills).</summary>
public class NpcStatsSnapshot {
    public int Str { get; set; } = 10;
    public int Dex { get; set; } = 10;
    public int Con { get; set; } = 10;
    public int Int { get; set; } = 10;
    public int Wis { get; set; } = 10;
    public int Cha { get; set; } = 10;
    public int Pb { get; set; } = 2;
    public Dictionary<string, int>? Saves { get; set; }
    public Dictionary<string, int>? Skills { get; set; }
}
