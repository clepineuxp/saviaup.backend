using System.Net;

namespace SaviaUp.Backend.Api.Configuration;

public static class ClientNetworkAddress
{
    public static string? From(HttpContext? context)
    {
        var forwardedFor = context?.Request.Headers["X-Forwarded-For"].ToString();
        var originalClientAddress = forwardedFor?
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        var realIp = context?.Request.Headers["X-Real-IP"].ToString();

        return Normalize(originalClientAddress)
            ?? Normalize(realIp)
            ?? Normalize(context?.Connection.RemoteIpAddress?.ToString());
    }

    public static string? Normalize(string? sourceIpAddress)
    {
        if (!IPAddress.TryParse(sourceIpAddress, out var address)) return null;
        return address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();
    }
}
