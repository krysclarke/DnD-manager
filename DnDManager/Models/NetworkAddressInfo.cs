using System.Net;
using System.Net.Sockets;

namespace DnDManager.Models;

public class NetworkAddressInfo {
    public required IPAddress Address { get; init; }
    public required string InterfaceName { get; init; }

    public AddressFamily Family => Address.AddressFamily;

    public string FamilyLabel => Family == AddressFamily.InterNetwork ? "IPv4" : "IPv6";

    public string DisplayText => $"{Address} ({InterfaceName}, {FamilyLabel})";

    public string FormatForUrl() {
        if (Family == AddressFamily.InterNetworkV6) {
            var addrStr = Address.ToString();
            // Percent-encode the scope ID separator for URLs
            addrStr = addrStr.Replace("%", "%25");
            return $"[{addrStr}]";
        }

        return Address.ToString();
    }

    public override string ToString() => DisplayText;

    public override bool Equals(object? obj) =>
        obj is NetworkAddressInfo other &&
        Address.ToString() == other.Address.ToString() &&
        InterfaceName == other.InterfaceName;

    public override int GetHashCode() =>
        HashCode.Combine(Address.ToString(), InterfaceName);
}
