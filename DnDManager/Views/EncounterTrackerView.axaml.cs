using Avalonia.Controls;
using Avalonia.Interactivity;
using DnDManager.ViewModels;

namespace DnDManager.Views;

public partial class EncounterTrackerView : UserControl {
    public EncounterTrackerView() {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e) {
        base.OnLoaded(e);
        RestoreSplitterPositions();

        // Track splitter drags by listening for column/row size changes
        ContentGrid.LayoutUpdated += OnContentGridLayoutUpdated;
        InnerGrid.LayoutUpdated += OnInnerGridLayoutUpdated;
    }

    protected override void OnUnloaded(RoutedEventArgs e) {
        ContentGrid.LayoutUpdated -= OnContentGridLayoutUpdated;
        InnerGrid.LayoutUpdated -= OnInnerGridLayoutUpdated;
        base.OnUnloaded(e);
    }

    private void RestoreSplitterPositions() {
        if (DataContext is not EncounterTrackerViewModel vm) return;

        if (vm.DiceRollerColumnRatio is { } colRatio and > 0 and < 1) {
            ContentGrid.ColumnDefinitions[0].Width = new GridLength(1 - colRatio, GridUnitType.Star);
            ContentGrid.ColumnDefinitions[2].Width = new GridLength(colRatio, GridUnitType.Star);
        }

        if (vm.CampaignNotesRowRatio is { } rowRatio and > 0 and < 1) {
            InnerGrid.RowDefinitions[0].Height = new GridLength(1 - rowRatio, GridUnitType.Star);
            InnerGrid.RowDefinitions[2].Height = new GridLength(rowRatio, GridUnitType.Star);
        }
    }

    private double _lastContentCol0 = -1;
    private double _lastContentCol2 = -1;
    private double _lastInnerRow0 = -1;
    private double _lastInnerRow2 = -1;

    private void OnContentGridLayoutUpdated(object? sender, EventArgs e) {
        if (DataContext is not EncounterTrackerViewModel vm) return;

        var col0 = ContentGrid.ColumnDefinitions[0].ActualWidth;
        var col2 = ContentGrid.ColumnDefinitions[2].ActualWidth;

        // Only update when values actually changed (avoid unnecessary writes)
        if (Math.Abs(col0 - _lastContentCol0) < 1 && Math.Abs(col2 - _lastContentCol2) < 1) return;
        _lastContentCol0 = col0;
        _lastContentCol2 = col2;

        var total = col0 + col2;
        if (total > 0) {
            vm.DiceRollerColumnRatio = col2 / total;
        }
    }

    private void OnInnerGridLayoutUpdated(object? sender, EventArgs e) {
        if (DataContext is not EncounterTrackerViewModel vm) return;

        var row0 = InnerGrid.RowDefinitions[0].ActualHeight;
        var row2 = InnerGrid.RowDefinitions[2].ActualHeight;

        if (Math.Abs(row0 - _lastInnerRow0) < 1 && Math.Abs(row2 - _lastInnerRow2) < 1) return;
        _lastInnerRow0 = row0;
        _lastInnerRow2 = row2;

        var total = row0 + row2;
        if (total > 0) {
            vm.CampaignNotesRowRatio = row2 / total;
        }
    }
}
