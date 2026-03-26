using Avalonia.Controls;
using Avalonia.Input;
using DnDManager.ViewModels;

namespace DnDManager.Views;

public partial class CampaignNotesView : UserControl {
    public CampaignNotesView() {
        InitializeComponent();

        MarkdownViewer.DoubleTapped += OnMarkdownDoubleTapped;
    }

    private void OnMarkdownDoubleTapped(object? sender, TappedEventArgs e) {
        if (DataContext is CampaignNotesViewModel vm) {
            vm.BeginEditing();
            EditTextBox.Focus();
        }
    }
}
