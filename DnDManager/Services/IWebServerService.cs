using System.Net;

namespace DnDManager.Services;

public interface IWebServerService : IAsyncDisposable {
    string? Url { get; }
    int Port { get; }
    bool IsRunning { get; }
    event Action<string>? UrlChanged;
    event Action<bool>? RunningChanged;
    Task StartAsync(IPAddress selectedAddress, int preferredPort = 0);
    Task StopAsync();
}
