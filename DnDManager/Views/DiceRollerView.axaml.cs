using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using DnDManager.ViewModels;

namespace DnDManager.Views;

public partial class DiceRollerView : UserControl {
    public DiceRollerView() {
        InitializeComponent();
        DiceInputBox.KeyDown += OnDiceInputKeyDown;
    }

    private void OnDiceInputKeyDown(object? sender, KeyEventArgs e) {
        if (e.Key != Key.Enter && e.Key != Key.Return) return;

        if (DataContext is DiceRollerViewModel vm && vm.RollCommand.CanExecute(null)) {
            vm.RollCommand.Execute(null);
            e.Handled = true;
        }
    }
}
