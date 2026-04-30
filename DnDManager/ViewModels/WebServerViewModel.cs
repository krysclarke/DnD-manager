using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DnDManager.Models;
using DnDManager.Services;
using Microsoft.AspNetCore.SignalR;
using DnDManager.Web;

namespace DnDManager.ViewModels;

public partial class WebServerViewModel : ObservableObject {
    private readonly IWebServerService _webServerService;
    private readonly INetworkService _networkService;
    private readonly IQrCodeService _qrCodeService;
    private readonly ICampaignRepository _campaignRepository;

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string? _networkError;

    [ObservableProperty]
    private bool _isQrCodeVisible;

    [ObservableProperty]
    private bool _isStartPromptVisible;

    [ObservableProperty]
    private Bitmap? _qrCodeImage;

    [ObservableProperty]
    private NetworkAddressInfo? _selectedAddress;

    public ObservableCollection<NetworkAddressInfo> AvailableAddresses { get; } = new();

    public event Action<string?>? SelectedAddressChanged;
    public event Action<IHubContext<EncounterHub>>? ServerStarted;

    public WebServerViewModel(
        IWebServerService webServerService,
        INetworkService networkService,
        IQrCodeService qrCodeService,
        ICampaignRepository campaignRepository) {
        _webServerService = webServerService;
        _networkService = networkService;
        _qrCodeService = qrCodeService;
        _campaignRepository = campaignRepository;

        _webServerService.UrlChanged += url => {
            ServerUrl = url;
            NetworkError = null;
        };

        _webServerService.RunningChanged += running => {
            IsRunning = running;
            StartServerCommand.NotifyCanExecuteChanged();
            StopServerCommand.NotifyCanExecuteChanged();
        };

        _networkService.ConnectivityChanged += hasConnectivity => {
            if (hasConnectivity) {
                NetworkError = null;
                if (IsQrCodeVisible && _webServerService.Url != null)
                    QrCodeImage = _qrCodeService.GenerateQrCode(_webServerService.Url);
            } else {
                NetworkError = "No network connectivity detected. The web interface will be available once a LAN connection is established.";
            }
        };

        _networkService.AddressesChanged += () => {
            Dispatcher.UIThread.Post(RefreshAddresses);
        };
    }

    public void Initialize(string? savedAddress) {
        RefreshAddresses();

        if (savedAddress != null) {
            SelectedAddress = AvailableAddresses
                .FirstOrDefault(a => a.Address.ToString() == savedAddress);
        }

        SelectedAddress ??= AvailableAddresses.FirstOrDefault();
    }

    partial void OnSelectedAddressChanged(NetworkAddressInfo? value) {
        StartServerCommand.NotifyCanExecuteChanged();
        SelectedAddressChanged?.Invoke(value?.Address.ToString());
    }

    private bool CanStartServer() => SelectedAddress != null && !IsRunning;

    [RelayCommand(CanExecute = nameof(CanStartServer))]
    private async Task StartServerAsync() {
        if (SelectedAddress == null) return;

        try {
            var portStr = await _campaignRepository.LoadSettingAsync("webServerPort");
            var preferredPort = int.TryParse(portStr, out var p) ? p : 0;

            await _webServerService.StartAsync(SelectedAddress.Address, preferredPort);

            if (_webServerService.Port != preferredPort)
                _ = _campaignRepository.SaveSettingAsync("webServerPort", _webServerService.Port.ToString());

            if (_webServerService is WebServerService wss && wss.HubContext != null)
                ServerStarted?.Invoke(wss.HubContext);
        } catch (Exception ex) {
            NetworkError = $"Failed to start web server: {ex.Message}";
        }
    }

    private bool CanStopServer() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanStopServer))]
    private async Task StopServerAsync() {
        try {
            await _webServerService.StopAsync();
        } catch (Exception ex) {
            NetworkError = $"Failed to stop web server: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ShowQrCode() {
        if (!IsRunning) {
            IsStartPromptVisible = true;
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
    private async Task StartServerFromPrompt() {
        IsStartPromptVisible = false;

        if (SelectedAddress == null) {
            // Try to pick a default address
            SelectedAddress = AvailableAddresses.FirstOrDefault();
            if (SelectedAddress == null) {
                NetworkError = "No network addresses available.";
                return;
            }
        }

        await StartServerAsync();

        // Auto-show QR once the server finishes starting
        if (IsRunning)
            ShowQrCode();
    }

    [RelayCommand]
    private void HideStartPrompt() {
        IsStartPromptVisible = false;
    }

    private void RefreshAddresses() {
        var currentSelection = SelectedAddress;
        var addresses = _networkService.GetAllLanAddresses();

        AvailableAddresses.Clear();
        foreach (var addr in addresses) {
            AvailableAddresses.Add(addr);
        }

        // Restore selection if the same address is still available
        if (currentSelection != null) {
            SelectedAddress = AvailableAddresses
                .FirstOrDefault(a => a.Address.ToString() == currentSelection.Address.ToString()
                                     && a.InterfaceName == currentSelection.InterfaceName);
        }

        SelectedAddress ??= AvailableAddresses.FirstOrDefault();
        StartServerCommand.NotifyCanExecuteChanged();
    }
}
