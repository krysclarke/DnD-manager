using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DnDManager.ViewModels;

public partial class CampaignNotesViewModel : ObservableObject {
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private string _markdownText = string.Empty;

    [ObservableProperty]
    private int _caretPosition;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _isEditing;

    private string _editBackup = string.Empty;

    public bool IsEmpty => !IsEditing && string.IsNullOrWhiteSpace(MarkdownText);

    public void BeginEditing() {
        _editBackup = MarkdownText;
        IsEditing = true;
    }

    [RelayCommand]
    private void Save() {
        IsEditing = false;
    }

    [RelayCommand]
    private void Cancel() {
        MarkdownText = _editBackup;
        IsEditing = false;
    }
}
