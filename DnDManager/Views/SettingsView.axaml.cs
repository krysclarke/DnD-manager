using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using DnDManager.Models;
using DnDManager.ViewModels;

namespace DnDManager.Views;

public partial class SettingsView : UserControl {
    public SettingsView() {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e) {
        base.OnLoaded(e);
        // Wire up theme card clicks since we can't bind Command to a property setter
        WireThemeButtons();

        if (DataContext is SettingsViewModel vm) {
            vm.ThemeService.ScaleChanged += ApplySettingsScale;
            ApplySettingsScale();
        }
    }

    private void ApplySettingsScale() {
        if (DataContext is SettingsViewModel vm) {
            var container = this.FindControl<LayoutTransformControl>("SettingsScaleContainer");
            if (container != null) {
                var scale = vm.ThemeService.CurrentScale;
                container.LayoutTransform = new ScaleTransform(scale, scale);
            }
        }
    }

    private void WireThemeButtons() {
        // Find all buttons in the ItemsControl and attach click handlers
        // The DataTemplate buttons use Tag to carry the AppTheme
    }

    /// <summary>
    /// Called by theme card buttons. We handle this via AddHandler on the UserControl.
    /// </summary>
    protected override void OnInitialized() {
        base.OnInitialized();
        AddHandler(Button.ClickEvent, OnButtonClick);
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e) {
        if (e.Source is Button { Tag: AppTheme theme } && DataContext is SettingsViewModel vm) {
            vm.SelectedTheme = theme;
        }
    }
}
