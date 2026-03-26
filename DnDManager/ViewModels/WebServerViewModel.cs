using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Services;

namespace DnDManager.ViewModels;

public partial class WebServerViewModel : ObservableObject {
    private readonly IWebServerService _webServerService;
    private readonly INetworkService _networkService;
    private readonly IQrCodeService _qrCodeService;

    [ObservableProperty]
    private string _serverUrl = "Starting...";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string? _networkError;

    [ObservableProperty]
    private bool _isQrCodeVisible;

    [ObservableProperty]
    private bool _isEnablePromptVisible;

    [ObservableProperty]
    private Bitmap? _qrCodeImage;

    public Func<bool>? IsWebInterfaceEnabled { get; set; }
    public Action? RequestEnableWebInterface { get; set; }

    public WebServerViewModel(
        IWebServerService webServerService,
        INetworkService networkService,
        IQrCodeService qrCodeService) {
        _webServerService = webServerService;
        _networkService = networkService;
        _qrCodeService = qrCodeService;

        _webServerService.UrlChanged += url => {
            ServerUrl = url;
            NetworkError = null;
        };

        _webServerService.RunningChanged += running => {
            IsRunning = running;
        };

        _networkService.ConnectivityChanged += hasConnectivity => {
            if (hasConnectivity) {
                NetworkError = null;
                // Regenerate QR if visible
                if (IsQrCodeVisible && _webServerService.Url != null)
                    QrCodeImage = _qrCodeService.GenerateQrCode(_webServerService.Url);
            } else {
                NetworkError = "No network connectivity detected. The web interface will be available once a LAN connection is established.";
            }
        };
    }

    [RelayCommand]
    private void ShowQrCode() {
        if (IsWebInterfaceEnabled != null && !IsWebInterfaceEnabled()) {
            IsEnablePromptVisible = true;
            return;
        }

        if (!_networkService.HasLanConnectivity()) {
            NetworkError = "No network connectivity detected. The web interface will be available once a LAN connection is established.";
            IsQrCodeVisible = false;
            return;
        }

        if (_webServerService.Url == null) {
            NetworkError = "Web server is not running.";
            IsQrCodeVisible = false;
            return;
        }

        NetworkError = null;
        QrCodeImage = _qrCodeService.GenerateQrCode(_webServerService.Url, 6);
        IsQrCodeVisible = true;
    }

    [RelayCommand]
    private void HideQrCode() {
        IsQrCodeVisible = false;
    }

    [RelayCommand]
    private void EnableWebInterface() {
        IsEnablePromptVisible = false;
        RequestEnableWebInterface?.Invoke();

        // Auto-show QR once the server finishes starting
        void OnRunning(bool running) {
            if (!running) return;
            _webServerService.RunningChanged -= OnRunning;
            ShowQrCode();
        }

        if (_webServerService.IsRunning) {
            ShowQrCode();
        } else {
            _webServerService.RunningChanged += OnRunning;
        }
    }

    [RelayCommand]
    private void HideEnablePrompt() {
        IsEnablePromptVisible = false;
    }
}
