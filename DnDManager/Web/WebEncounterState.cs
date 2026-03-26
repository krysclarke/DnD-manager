namespace DnDManager.Web;

public class WebEncounterState {
    public bool IsEncounterActive { get; set; }
    public int RoundNumber { get; set; }
    public int ActiveCharacterIndex { get; set; }
    public List<WebCharacterDto> Characters { get; set; } = [];
    public WebThemeDto Theme { get; set; } = new();
    public double UiScale { get; set; } = 1.0;
}

public class WebCharacterDto {
    public bool IsPc { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int? Initiative { get; set; }
    public bool IsActive { get; set; }
    public double? HpPercent { get; set; }
    public string? HpCategory { get; set; }
    public string? Conditions { get; set; }
}

public class WebThemeDto {
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsDark { get; set; }
    public string Surface { get; set; } = "#1E1E1E";
    public string Accent { get; set; } = "#2D6CA3";
    public string AccentForeground { get; set; } = "#FFFFFF";
    public string MutedText { get; set; } = "#888888";
    public string ActiveHighlight { get; set; } = "#333333";
    public string HpGreen { get; set; } = "#0F7B0F";
    public string HpYellow { get; set; } = "#C4A700";
    public string HpRed { get; set; } = "#C42B1C";
    public string DialogBg { get; set; } = "#2D2D2D";
    public string OverlayBg { get; set; } = "#1A1A1A";
}
