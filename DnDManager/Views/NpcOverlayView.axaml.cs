using Avalonia.Controls;
using Avalonia.Input;
using DnDManager.ViewModels;

namespace DnDManager.Views;

public partial class NpcOverlayView : UserControl {
    public NpcOverlayView() {
        InitializeComponent();
    }

    private void OnHpDoubleTapped(object? sender, TappedEventArgs e) {
        if (DataContext is NpcOverlayViewModel { SelectedNpc: { } npc }) {
            npc.EnterHpEditModeCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnHpDeltaKeyDown(object? sender, KeyEventArgs e) {
        if (e.Key == Key.Escape && DataContext is NpcOverlayViewModel { SelectedNpc: { } npc }) {
            npc.CancelHpEditCommand.Execute(null);
            e.Handled = true;
        }
    }
}
