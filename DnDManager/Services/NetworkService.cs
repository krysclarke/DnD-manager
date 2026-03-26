using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DnDManager.Services;

public class NetworkService : INetworkService {
    private bool _lastConnectivity;

    public event Action<bool>? ConnectivityChanged;

    public IPAddress? GetLanIpAddress() {
        try {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces()) {
                if (iface.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (iface.NetworkInterfaceType is NetworkInterfaceType.Loopback
                    or NetworkInterfaceType.Tunnel)
                    continue;

                var props = iface.GetIPProperties();
                foreach (var addr in props.UnicastAddresses) {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    if (IsPrivateIp(addr.Address))
                        return addr.Address;
                }
            }
        } catch {
            // Network enumeration can fail on some platforms
        }

        return null;
    }

    public bool HasLanConnectivity() => GetLanIpAddress() != null;

    public void StartMonitoring() {
        _lastConnectivity = HasLanConnectivity();
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
    }

    public void StopMonitoring() {
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) {
        CheckAndNotify();
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e) {
        CheckAndNotify();
    }

    private void CheckAndNotify() {
        var current = HasLanConnectivity();
        if (current != _lastConnectivity) {
            _lastConnectivity = current;
            ConnectivityChanged?.Invoke(current);
        }
    }

    private static bool IsPrivateIp(IPAddress address) {
        var bytes = address.GetAddressBytes();
        return bytes[0] switch {
            10 => true,
            172 => bytes[1] >= 16 && bytes[1] <= 31,
            192 => bytes[1] == 168,
            _ => false
        };
    }
}
