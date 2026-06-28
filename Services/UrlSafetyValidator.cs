using System.Net;
using System.Net.Sockets;

namespace cerberus_securitygate.Services;

public class UrlSafetyValidator
{
    public bool IsSafe(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return false;

        var host = uri.Host;

        if (IsBlockedHostname(host))
            return false;

        if (IPAddress.TryParse(host, out var literalIp))
            return !IsPrivateOrReserved(literalIp);

        var resolved = ResolveHost(host);
        if (resolved.Count == 0)
            return false;

        return resolved.All(ip => !IsPrivateOrReserved(ip));
    }

    private static bool IsBlockedHostname(string host)
    {
        var lowered = host.ToLowerInvariant();
        return lowered is "localhost" or "metadata.google.internal"
            || lowered.EndsWith(".localhost")
            || lowered.EndsWith(".internal")
            || lowered.EndsWith(".local");
    }

    private static List<IPAddress> ResolveHost(string host)
    {
        try
        {
            return Dns.GetHostAddresses(host).ToList();
        }
        catch (SocketException)
        {
            return new List<IPAddress>();
        }
    }

    private static bool IsPrivateOrReserved(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        var bytes = ip.GetAddressBytes();

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] switch
            {
                10 => true,
                127 => true,
                169 when bytes[1] == 254 => true,
                172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
                192 when bytes[1] == 168 => true,
                0 => true,
                _ => false
            };
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
                return true;

            if (bytes[0] == 0xfc || bytes[0] == 0xfd)
                return true;
        }

        return false;
    }
}