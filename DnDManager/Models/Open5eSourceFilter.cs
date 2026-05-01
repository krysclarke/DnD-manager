using CommunityToolkit.Mvvm.ComponentModel;

namespace DnDManager.Models;

public partial class Open5eSourceFilter : ObservableObject {
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    [ObservableProperty] private bool _isEnabled = true;
}
