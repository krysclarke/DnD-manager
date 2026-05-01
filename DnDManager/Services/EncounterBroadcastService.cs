using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using DnDManager.Models;
using DnDManager.ViewModels;
using DnDManager.Web;
using Microsoft.AspNetCore.SignalR;

namespace DnDManager.Services;

public class EncounterBroadcastService : IDisposable {
    private readonly EncounterTrackerViewModel _encounterVm;
    private readonly IThemeService _themeService;
    private readonly Func<AppTheme> _webThemeProvider;
    private readonly Func<double> _webScaleProvider;
    private IHubContext<EncounterHub>? _hubContext;
    private readonly Dictionary<CharacterViewModel, int> _npcNumbers = new();
    private int _nextNpcNumber = 1;

    public EncounterBroadcastService(
        EncounterTrackerViewModel encounterVm,
        IThemeService themeService,
        Func<AppTheme> webThemeProvider,
        Func<double> webScaleProvider) {
        _encounterVm = encounterVm;
        _themeService = themeService;
        _webThemeProvider = webThemeProvider;
        _webScaleProvider = webScaleProvider;

        _encounterVm.PropertyChanged += OnEncounterPropertyChanged;
        _encounterVm.Characters.CollectionChanged += OnCharactersCollectionChanged;

        foreach (var c in _encounterVm.Characters)
            SubscribeToCharacter(c);
    }

    public void SetHubContext(IHubContext<EncounterHub> hubContext) {
        _hubContext = hubContext;
    }

    public WebEncounterState BuildFullState() {
        var characters = new List<WebCharacterDto>();
        for (var i = 0; i < _encounterVm.Characters.Count; i++) {
            var vm = _encounterVm.Characters[i];
            characters.Add(BuildCharacterDto(vm, i == _encounterVm.ActiveCharacterIndex && _encounterVm.IsEncounterActive));
        }

        return new WebEncounterState {
            IsEncounterActive = _encounterVm.IsEncounterActive,
            RoundNumber = _encounterVm.RoundNumber,
            ActiveCharacterIndex = _encounterVm.ActiveCharacterIndex,
            Characters = characters,
            Theme = BuildThemeDto(_webThemeProvider()),
            UiScale = _webScaleProvider()
        };
    }

    private WebCharacterDto BuildCharacterDto(CharacterViewModel vm, bool isActive) {
        var dto = new WebCharacterDto {
            IsPc = vm.IsPc,
            DisplayName = vm.IsPc ? vm.Name : GetNpcDisplayName(vm),
            Initiative = vm.Initiative,
            IsActive = isActive
        };

        if (vm.IsNpc) {
            var hpPercent = vm.MaxHitPoints > 0
                ? (double)vm.CurrentHitPoints / vm.MaxHitPoints
                : 0;
            dto.HpPercent = hpPercent;
            dto.HpCategory = hpPercent switch {
                > 0.75 => "green",
                >= 0.30 => "yellow",
                _ => "red"
            };
            dto.Conditions = string.IsNullOrWhiteSpace(vm.Conditions) ? null : vm.Conditions;
        }

        return dto;
    }

    private string GetNpcDisplayName(CharacterViewModel vm) {
        return "Monster";
    }

    public void ResetNpcNumbers() {
        _npcNumbers.Clear();
        _nextNpcNumber = 1;
    }

    public static WebThemeDto BuildThemeDto(AppTheme theme) {
        return new WebThemeDto {
            Id = theme.Id,
            DisplayName = theme.DisplayName,
            IsDark = theme.BaseVariant == ThemeVariant.Dark,
            Surface = ColorToHex(theme.Surface),
            Accent = ColorToHex(theme.Accent),
            AccentForeground = ColorToHex(theme.AccentForeground),
            MutedText = ColorToHex(theme.MutedText),
            ActiveHighlight = ColorToHex(theme.ActiveHighlight),
            DialogBg = ColorToHex(theme.DialogBg),
            OverlayBg = ColorToHex(theme.OverlayBg),
            // Health bar colors derived from theme
            HpGreen = theme.BaseVariant == ThemeVariant.Dark ? "#4CAF50" : "#2E7D32",
            HpYellow = theme.BaseVariant == ThemeVariant.Dark ? "#FFC107" : "#F9A825",
            HpRed = theme.BaseVariant == ThemeVariant.Dark ? "#F44336" : "#C62828"
        };
    }

    private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private async void OnEncounterPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (_hubContext == null) return;

        switch (e.PropertyName) {
            case nameof(EncounterTrackerViewModel.IsEncounterActive): {
                if (!_encounterVm.IsEncounterActive)
                    ResetNpcNumbers();
                var state = await Dispatcher.UIThread.InvokeAsync(BuildFullState);
                var method = _encounterVm.IsEncounterActive ? "EncounterStarted" : "EncounterStopped";
                await _hubContext.Clients.All.SendAsync(method, state);
                break;
            }
            case nameof(EncounterTrackerViewModel.ActiveCharacterIndex):
            case nameof(EncounterTrackerViewModel.RoundNumber): {
                var index = _encounterVm.ActiveCharacterIndex;
                var round = _encounterVm.RoundNumber;
                await _hubContext.Clients.All.SendAsync("TurnAdvanced", index, round);
                break;
            }
        }
    }

    private async void OnCharactersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        if (_hubContext == null) return;

        switch (e.Action) {
            case NotifyCollectionChangedAction.Add when e.NewItems != null:
                var midListInsert = false;
                foreach (CharacterViewModel vm in e.NewItems) {
                    SubscribeToCharacter(vm);
                    var index = _encounterVm.Characters.IndexOf(vm);
                    if (index >= 0 && index < _encounterVm.Characters.Count - 1) {
                        midListInsert = true;
                        continue;
                    }
                    var dto = await Dispatcher.UIThread.InvokeAsync(() =>
                        BuildCharacterDto(vm, false));
                    await _hubContext.Clients.All.SendAsync("CharacterAdded", dto, index);
                }
                if (midListInsert) {
                    var addState = await Dispatcher.UIThread.InvokeAsync(BuildFullState);
                    await _hubContext.Clients.All.SendAsync("ReceiveFullState", addState);
                }
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                foreach (CharacterViewModel vm in e.OldItems) {
                    UnsubscribeFromCharacter(vm);
                    _npcNumbers.Remove(vm);
                }
                // Send full state after removal since indices shift
                var state = await Dispatcher.UIThread.InvokeAsync(BuildFullState);
                await _hubContext.Clients.All.SendAsync("ReceiveFullState", state);
                break;

            case NotifyCollectionChangedAction.Reset:
                ResetNpcNumbers();
                var resetState = await Dispatcher.UIThread.InvokeAsync(BuildFullState);
                await _hubContext.Clients.All.SendAsync("ReceiveFullState", resetState);
                break;
        }
    }

    private async void OnCharacterPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (_hubContext == null || sender is not CharacterViewModel vm) return;

        if (e.PropertyName is nameof(CharacterViewModel.CurrentHitPoints)
            or nameof(CharacterViewModel.Conditions)) {
            var index = _encounterVm.Characters.IndexOf(vm);
            if (index < 0) return;
            var isActive = index == _encounterVm.ActiveCharacterIndex && _encounterVm.IsEncounterActive;
            var dto = await Dispatcher.UIThread.InvokeAsync(() => BuildCharacterDto(vm, isActive));
            await _hubContext.Clients.All.SendAsync("CharacterUpdated", dto, index);
        }
    }

    private void SubscribeToCharacter(CharacterViewModel vm) {
        vm.PropertyChanged += OnCharacterPropertyChanged;
    }

    private void UnsubscribeFromCharacter(CharacterViewModel vm) {
        vm.PropertyChanged -= OnCharacterPropertyChanged;
    }

    public void BroadcastThemeChange() {
        if (_hubContext == null) return;
        var dto = BuildThemeDto(_webThemeProvider());
        _ = _hubContext.Clients.All.SendAsync("ThemeChanged", dto);
    }

    public void BroadcastScaleChange() {
        if (_hubContext == null) return;
        _ = _hubContext.Clients.All.SendAsync("ScaleChanged", _webScaleProvider());
    }

    public void Dispose() {
        _encounterVm.PropertyChanged -= OnEncounterPropertyChanged;
        _encounterVm.Characters.CollectionChanged -= OnCharactersCollectionChanged;
        foreach (var c in _encounterVm.Characters)
            UnsubscribeFromCharacter(c);
        GC.SuppressFinalize(this);
    }
}
