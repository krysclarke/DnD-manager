using CommunityToolkit.Mvvm.ComponentModel;

namespace DnDManager.ViewModels;

/// <summary>
/// A single condition row in the conditions editor. <see cref="Selected"/> is the
/// DM's independent choice; <see cref="Imposed"/> is applied automatically by
/// another active condition and locks the row.
/// </summary>
public partial class ConditionToggleViewModel : ObservableObject {
    public string Name { get; }
    public bool IsExhaustion { get; }

    public ConditionToggleViewModel(string name, bool isExhaustion = false) {
        Name = name;
        IsExhaustion = isExhaustion;
    }

    [ObservableProperty]
    private bool _selected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChecked))]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    [NotifyPropertyChangedFor(nameof(SourceLabel))]
    private bool _imposed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SourceLabel))]
    private IReadOnlyList<string> _imposedBy = [];

    [ObservableProperty]
    private int _exhaustionLevel = 1;

    /// <summary>True when the condition is active for any reason.</summary>
    public bool IsChecked {
        get => Selected || Imposed;
        set => Selected = value;
    }

    /// <summary>The checkbox is locked while the condition is imposed.</summary>
    public bool IsEditable => !Imposed;

    public string SourceLabel =>
        Imposed && ImposedBy.Count > 0 ? $"(from {string.Join(", ", ImposedBy)})" : string.Empty;

    partial void OnSelectedChanged(bool value) {
        OnPropertyChanged(nameof(IsChecked));
    }
}
