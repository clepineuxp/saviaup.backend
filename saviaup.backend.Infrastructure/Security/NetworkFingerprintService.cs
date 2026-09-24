using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Security;

public sealed class NetworkFingerprintService(
    IOptions<PrintingOptions> printingOptions,
    IOptions<JwtOptions> jwtOptions) : INetworkFingerprintService
{
    public string? Compute(string? sourceIpAddress)
    {
        if (string.IsNullOrWhiteSpace(sourceIpAddress)) return null;
        var configuredKey = printingOptions.Value.NetworkFingerprintKey;
        var key = string.IsNullOrWhiteSpace(configuredKey) ? jwtOptions.Value.SigningKey : configuredKey;
        if (string.IsNullOrWhiteSpace(key)) return null;

        var fingerprint = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(key),
            Encoding.UTF8.GetBytes(NetworkIdentity(sourceIpAddress)));
        return Convert.ToHexString(fingerprint).ToLowerInvariant();
    }

    private static string NetworkIdentity(string sourceIpAddress)
    {
        if (!IPAddress.TryParse(sourceIpAddress, out var address)) return sourceIpAddress.Trim();
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return address.ToString();

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return $"{new IPAddress(bytes)}/64";
    }
}
