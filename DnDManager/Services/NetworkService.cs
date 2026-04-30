using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DnDManager.Models;

namespace DnDManager.Services;

public class NetworkService : INetworkService {
    private bool _lastConnectivity;

    public event Action<bool>? ConnectivityChanged;
    public event Action? AddressesChanged;

    public IReadOnlyList<NetworkAddressInfo> GetAllLanAddresses() {
        var results = new List<NetworkAddressInfo>();

        try {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces()) {
                if (iface.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (iface.NetworkInterfaceType is NetworkInterfaceType.Loopback
                    or NetworkInterfaceType.Tunnel)
                    continue;

                var props = iface.GetIPProperties();
                foreach (var addr in props.UnicastAddresses) {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork) {
                        if (IsPrivateIp(addr.Address)) {
                            results.Add(new NetworkAddressInfo {
                                Address = addr.Address,
                                InterfaceName = iface.Name
                            });
                        }
                    } else if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6) {
                        if (IsValidV6(addr.Address)) {
                            results.Add(new NetworkAddressInfo {
                                Address = addr.Address,
                                InterfaceName = iface.Name
                            });
                        }
                    }
                }
            }
        } catch {
            // Network enumeration can fail on some platforms
        }

        return results;
    }

    public IPAddress? GetLanIpAddress() =>
        GetAllLanAddresses()
            .FirstOrDefault(a => a.Family == AddressFamily.InterNetwork)?
            .Address;

    public bool HasLanConnectivity() => GetAllLanAddresses().Count > 0;

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
        AddressesChanged?.Invoke();

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

    private static bool IsValidV6(IPAddress address) {
        if (address.Equals(IPAddress.IPv6Loopback))
            return false;
        if (address.IsIPv6Multicast)
            return false;

        var bytes = address.GetAddressBytes();

        // Link-local (fe80::/10)
        if (address.IsIPv6LinkLocal)
            return true;

        // Global unicast (2000::/3)
        if ((bytes[0] & 0xE0) == 0x20)
            return true;

        // Unique local (fc00::/7)
        if ((bytes[0] & 0xFE) == 0xFC)
            return true;

        return false;
    }
}
