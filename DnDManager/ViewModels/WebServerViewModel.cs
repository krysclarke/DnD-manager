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
    private Bitmap? _qrCodeImage;

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
}
