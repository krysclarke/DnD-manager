using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;

namespace DnDManager.ViewModels;

public partial class ImportDuplicateDialogViewModel : ObservableObject {
    private readonly TaskCompletionSource<ImportDuplicateMode?> _tcs = new();

    public int TotalCount { get; }
    public int DuplicateCount { get; }
    public ObservableCollection<string> DuplicateNames { get; }
    public string Message { get; }

    public Task<ImportDuplicateMode?> Result => _tcs.Task;

    public ImportDuplicateDialogViewModel(int totalCount, List<string> duplicateNames) {
        TotalCount = totalCount;
        DuplicateCount = duplicateNames.Count;
        DuplicateNames = new ObservableCollection<string>(duplicateNames);
        Message = $"{DuplicateCount} of {TotalCount} entries already exist in your bestiary";
    }

    [RelayCommand]
    private void Skip() {
        _tcs.TrySetResult(ImportDuplicateMode.Skip);
    }

    [RelayCommand]
    private void Overwrite() {
        _tcs.TrySetResult(ImportDuplicateMode.Overwrite);
    }

    [RelayCommand]
    private void Cancel() {
        _tcs.TrySetResult(null);
    }
}
