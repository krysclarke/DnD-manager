using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DnDManager.Services;
using DnDManager.ViewModels;
using DnDManager.Views;

namespace DnDManager;

public class App : Application {
    private WebServerService? _webServerService;
    private EncounterBroadcastService? _broadcastService;
    private NetworkService? _networkService;

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var campaignRepository = new SqliteCampaignRepository();
            var diceParser = new DiceParser();
            var diceRoller = new DiceRollerService();
            var encounterService = new EncounterService(diceRoller);
            var encounterFileService = new EncounterFileService();
            var bestiaryFileService = new BestiaryFileService();
            var open5eApiClient = new Open5eApiClient();
            var themeService = new ThemeService();
            var spellDatabaseService = new SpellDatabaseService();

            var mainVm = new MainWindowViewModel(
                campaignRepository, diceParser, diceRoller,
                encounterService, encounterFileService,
                bestiaryFileService, open5eApiClient, themeService,
                spellDatabaseService);

            // Web interface services
            _networkService = new NetworkService();
            _networkService.StartMonitoring();

            var qrCodeService = new QrCodeService();
            _webServerService = new WebServerService();

            var webServerVm = new WebServerViewModel(
                _webServerService, _networkService, qrCodeService, campaignRepository);

            // Wire web server VM into view models
            mainVm.WebServerVm = webServerVm;
            mainVm.SettingsVm.WebServerVm = webServerVm;
            mainVm.EncounterTrackerVm.WebServerVm = webServerVm;

            // Create broadcast service
            _broadcastService = new EncounterBroadcastService(
                mainVm.EncounterTrackerVm,
                themeService,
                () => mainVm.SettingsVm.SelectedWebTheme,
                () => mainVm.SettingsVm.WebUiScale);

            // Wire web theme change to broadcast
            mainVm.SettingsVm.WebThemeChanged += () => _broadcastService.BroadcastThemeChange();
            mainVm.SettingsVm.WebUiScaleChanged += () => _broadcastService.BroadcastScaleChange();

            // Set state provider (server started on-demand via Start button)
            _webServerService.SetStateProvider(() => _broadcastService.BuildFullState());

            // Wire server started event to set hub context on broadcast service
            webServerVm.ServerStarted += hubContext => _broadcastService.SetHubContext(hubContext);

            // Wire address selection persistence
            webServerVm.SelectedAddressChanged += address =>
                _ = campaignRepository.SaveSettingAsync("webSelectedAddress", address ?? "");

            desktop.MainWindow = new MainWindow {
                DataContext = mainVm
            };

            desktop.ShutdownRequested += async (_, _) => {
                if (desktop.MainWindow is MainWindow mainWindow) {
                    mainWindow.CaptureWindowGeometry();
                }
                await mainVm.SaveCampaignAsync();
                _networkService.StopMonitoring();
                _broadcastService.Dispose();
                await _webServerService.DisposeAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

}
