using Microsoft.Extensions.Options;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Infrastructure.Security;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class PrintAgentDiscoverySecurityTests
{
    [Fact]
    public void NetworkFingerprint_IsStableAndDoesNotExposeTheAddress()
    {
        var service = new NetworkFingerprintService(
            Options.Create(new PrintingOptions { NetworkFingerprintKey = new string('p', 32) }),
            Options.Create(new JwtOptions { SigningKey = new string('j', 32) }));

        var first = service.Compute("203.0.113.10");
        var repeated = service.Compute("203.0.113.10");
        var other = service.Compute("203.0.113.11");

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, other);
        Assert.DoesNotContain("203.0.113.10", first);
        Assert.Equal(64, first!.Length);
    }

    [Fact]
    public void NetworkFingerprint_RejectsAnUnavailableAddress()
    {
        var service = new NetworkFingerprintService(
            Options.Create(new PrintingOptions { NetworkFingerprintKey = new string('p', 32) }),
            Options.Create(new JwtOptions { SigningKey = new string('j', 32) }));

        Assert.Null(service.Compute(null));
        Assert.Null(service.Compute(" "));
    }

    [Fact]
    public void NetworkFingerprint_UsesTheIpv6SubnetInsteadOfOneDeviceAddress()
    {
        var service = new NetworkFingerprintService(
            Options.Create(new PrintingOptions { NetworkFingerprintKey = new string('p', 32) }),
            Options.Create(new JwtOptions { SigningKey = new string('j', 32) }));

        Assert.Equal(
            service.Compute("2001:db8:1234:5678::10"),
            service.Compute("2001:db8:1234:5678::99"));
        Assert.NotEqual(
            service.Compute("2001:db8:1234:5678::10"),
            service.Compute("2001:db8:1234:9999::10"));
    }
}
