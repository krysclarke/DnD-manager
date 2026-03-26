using System.Net;

namespace DnDManager.Services;

public interface INetworkService {
    event Action<bool>? ConnectivityChanged;
    IPAddress? GetLanIpAddress();
    bool HasLanConnectivity();
    void StartMonitoring();
    void StopMonitoring();
}
