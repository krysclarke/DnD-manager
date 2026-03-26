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

            var mainVm = new MainWindowViewModel(
                campaignRepository, diceParser, diceRoller,
                encounterService, encounterFileService,
                bestiaryFileService, open5eApiClient, themeService);

            // Web interface services
            _networkService = new NetworkService();
            _networkService.StartMonitoring();

            var qrCodeService = new QrCodeService();
            _webServerService = new WebServerService(_networkService);

            var webServerVm = new WebServerViewModel(
                _webServerService, _networkService, qrCodeService);

            // Wire web server VM into view models
            mainVm.WebServerVm = webServerVm;
            mainVm.SettingsVm.WebServerVm = webServerVm;
            mainVm.EncounterTrackerVm.WebServerVm = webServerVm;

            // Wire enable callbacks for QR button prompt
            webServerVm.IsWebInterfaceEnabled = () => mainVm.SettingsVm.IsWebInterfaceEnabled;
            webServerVm.RequestEnableWebInterface = () => mainVm.SettingsVm.IsWebInterfaceEnabled = true;

            // Create broadcast service
            _broadcastService = new EncounterBroadcastService(
                mainVm.EncounterTrackerVm,
                themeService,
                () => mainVm.SettingsVm.SelectedWebTheme,
                () => mainVm.SettingsVm.WebUiScale);

            // Wire web theme change to broadcast
            mainVm.SettingsVm.WebThemeChanged += () => _broadcastService.BroadcastThemeChange();
            mainVm.SettingsVm.WebUiScaleChanged += () => _broadcastService.BroadcastScaleChange();

            // Set state provider (server started on-demand via toggle)
            _webServerService.SetStateProvider(() => _broadcastService.BuildFullState());

            // Wire web interface enable/disable toggle
            mainVm.SettingsVm.WebInterfaceEnabledChanged += enabled => {
                if (enabled) {
                    _ = StartWebServerAsync(_webServerService, _broadcastService, campaignRepository);
                } else {
                    _ = StopWebServerAsync(_webServerService);
                }
            };

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

    private static async Task StartWebServerAsync(
        WebServerService webServerService,
        EncounterBroadcastService broadcastService,
        ICampaignRepository campaignRepository) {
        try {
            var portStr = await campaignRepository.LoadSettingAsync("webServerPort");
            var preferredPort = int.TryParse(portStr, out var p) ? p : 0;

            await webServerService.StartAsync(preferredPort);

            if (webServerService.Port != preferredPort)
                _ = campaignRepository.SaveSettingAsync("webServerPort", webServerService.Port.ToString());

            if (webServerService.HubContext != null)
                broadcastService.SetHubContext(webServerService.HubContext);
        } catch (Exception ex) {
            // Web server failure shouldn't crash the app
            Console.Error.WriteLine($"Failed to start web server: {ex.Message}");
        }
    }

    private static async Task StopWebServerAsync(WebServerService webServerService) {
        try {
            await webServerService.StopAsync();
        } catch (Exception ex) {
            Console.Error.WriteLine($"Failed to stop web server: {ex.Message}");
        }
    }
}
