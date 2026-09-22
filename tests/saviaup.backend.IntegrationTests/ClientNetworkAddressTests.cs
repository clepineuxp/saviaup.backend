using System.Net;
using Microsoft.AspNetCore.Http;
using SaviaUp.Backend.Api.Configuration;

namespace SaviaUp.Backend.IntegrationTests;

public sealed class ClientNetworkAddressTests
{
    [Fact]
    public void From_UsesTheOriginalClientAddressFromForwardedFor()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.42.0.15");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.15, 198.51.100.20";

        var address = ClientNetworkAddress.From(context);

        Assert.Equal("203.0.113.15", address);
    }

    [Fact]
    public void From_FallsBackToTheRemoteAddress_WhenForwardedHeadersAreMissing()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:203.0.113.15");

        var address = ClientNetworkAddress.From(context);

        Assert.Equal("203.0.113.15", address);
    }
}
