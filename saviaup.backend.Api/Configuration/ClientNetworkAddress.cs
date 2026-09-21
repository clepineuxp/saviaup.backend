using System.Net;

namespace SaviaUp.Backend.Api.Configuration;

public static class ClientNetworkAddress
{
    public static string? From(HttpContext? context)
    {
        var address = context?.Connection.RemoteIpAddress;
        if (address is null) return null;
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
    }

    public static string? Normalize(string? sourceIpAddress)
    {
        if (!IPAddress.TryParse(sourceIpAddress, out var address)) return null;
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
    }
}
