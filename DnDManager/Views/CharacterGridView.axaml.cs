using Avalonia.Controls;
using Avalonia.Input;
using DnDManager.ViewModels;

namespace DnDManager.Views;

public partial class CharacterGridView : UserControl {
    public CharacterGridView() {
        InitializeComponent();
    }

    private void OnConditionsDoubleTapped(object? sender, TappedEventArgs e) {
        if (sender is Control { DataContext: CharacterViewModel charVm }
            && DataContext is EncounterTrackerViewModel trackerVm) {
            trackerVm.OpenConditionsEditorCommand.Execute(charVm);
            e.Handled = true;
        }
    }
}
