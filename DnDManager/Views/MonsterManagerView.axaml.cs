using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DnDManager.ViewModels;

namespace DnDManager.Views;

public partial class MonsterManagerView : UserControl {
    private MonsterManagerViewModel? _vm;
    private Open5eImportViewModel? _importVm;

    public MonsterManagerView() {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e) {
        if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
        if (_importVm != null) _importVm.PropertyChanged -= OnImportVmPropertyChanged;

        _vm = DataContext as MonsterManagerViewModel;
        _importVm = _vm?.ImportVm;

        if (_vm != null) _vm.PropertyChanged += OnVmPropertyChanged;
        if (_importVm != null) _importVm.PropertyChanged += OnImportVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName != nameof(MonsterManagerViewModel.IsImportPanelOpen)) return;
        if (_vm?.IsImportPanelOpen != true) return;

        // Defer until the overlay's visual tree is realised, then focus the search box.
        Dispatcher.UIThread.Post(() => ImportSearchBox.Focus(), DispatcherPriority.Loaded);
    }

    private void OnImportVmPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName != nameof(Open5eImportViewModel.IsSearching)) return;
        ImportOverlay.Cursor = _importVm?.IsSearching == true
            ? new Cursor(StandardCursorType.Wait)
            : Cursor.Default;
    }

    private void ImportSearchBox_KeyDown(object? sender, KeyEventArgs e) {
        if (e.Key != Key.Enter && e.Key != Key.Return) return;
        if (_importVm?.SearchCommand.CanExecute(null) == true) {
            _importVm.SearchCommand.Execute(null);
            e.Handled = true;
        }
    }
}