using System.Net;
using DnDManager.Models;

namespace DnDManager.Services;

public interface INetworkService {
    event Action<bool>? ConnectivityChanged;
    event Action? AddressesChanged;
    IPAddress? GetLanIpAddress();
    IReadOnlyList<NetworkAddressInfo> GetAllLanAddresses();
    bool HasLanConnectivity();
    void StartMonitoring();
    void StopMonitoring();
}
