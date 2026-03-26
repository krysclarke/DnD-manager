using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DnDManager.ViewModels;

namespace DnDManager.Views;

public partial class MainWindow : Window {
    private double _lastNormalX;
    private double _lastNormalY;
    private double _lastNormalWidth;
    private double _lastNormalHeight;

    public MainWindow() {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e) {
        base.OnOpened(e);

        var vm = (MainWindowViewModel)DataContext!;
        await vm.InitializeAsync();

        RestoreWindowGeometry(vm);

        // Track normal-state geometry for save on close
        _lastNormalWidth = Width;
        _lastNormalHeight = Height;
        _lastNormalX = Position.X;
        _lastNormalY = Position.Y;

        PositionChanged += OnPositionChanged;
        PropertyChanged += OnWindowPropertyChanged;

        vm.ThemeService.ScaleChanged += ApplyScale;
        ApplyScale();
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e) {
        if (WindowState == WindowState.Normal) {
            _lastNormalX = e.Point.X;
            _lastNormalY = e.Point.Y;
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) {
        if (WindowState == WindowState.Normal) {
            if (e.Property == WidthProperty) {
                _lastNormalWidth = (double)e.NewValue!;
            } else if (e.Property == HeightProperty) {
                _lastNormalHeight = (double)e.NewValue!;
            }
        }
    }

    private void RestoreWindowGeometry(MainWindowViewModel vm) {
        if (vm.WindowWidth.HasValue && vm.WindowHeight.HasValue) {
            Width = vm.WindowWidth.Value;
            Height = vm.WindowHeight.Value;
        }

        if (vm.WindowX.HasValue && vm.WindowY.HasValue) {
            var pos = new PixelPoint((int)vm.WindowX.Value, (int)vm.WindowY.Value);

            if (IsPositionOnScreen(pos)) {
                Position = pos;
            }
        }

        if (vm.WindowState == "Maximized") {
            WindowState = WindowState.Maximized;
        }
    }

    private bool IsPositionOnScreen(PixelPoint pos) {
        var screens = Screens;
        if (screens?.All == null) return true;

        foreach (var screen in screens.All) {
            var bounds = screen.WorkingArea;
            if (pos.X >= bounds.X - 100 && pos.X < bounds.X + bounds.Width &&
                pos.Y >= bounds.Y - 50 && pos.Y < bounds.Y + bounds.Height) {
                return true;
            }
        }

        return false;
    }

    public void CaptureWindowGeometry() {
        var vm = (MainWindowViewModel)DataContext!;

        vm.WindowX = _lastNormalX;
        vm.WindowY = _lastNormalY;
        vm.WindowWidth = _lastNormalWidth;
        vm.WindowHeight = _lastNormalHeight;
        vm.WindowState = WindowState == WindowState.Maximized ? "Maximized" : "Normal";
    }

    private void ApplyScale() {
        var vm = (MainWindowViewModel)DataContext!;
        var scale = vm.ThemeService.CurrentScale;
        var transform = new ScaleTransform(scale, scale);

        var encounter = this.FindControl<LayoutTransformControl>("EncounterScaleContainer");
        var monster = this.FindControl<LayoutTransformControl>("MonsterScaleContainer");
        if (encounter != null) encounter.LayoutTransform = transform;
        if (monster != null) monster.LayoutTransform = transform;
    }
}
