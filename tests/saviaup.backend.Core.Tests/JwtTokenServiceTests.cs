using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Options;
using SaviaUp.Backend.Infrastructure.Security;
using SaviaUp.Backend.Shared.Constants;

namespace SaviaUp.Backend.Core.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateAccessToken_UsesFirstWordOfFirstAndLastNameForDisplayName()
    {
        var service = new JwtTokenService(
            Options.Create(new JwtOptions
            {
                Issuer = "saviaup-tests",
                Audience = "saviaup-tests",
                SigningKey = "saviaup-tests-signing-key-with-at-least-32-bytes",
                AccessTokenExpirationMinutes = 15
            }),
            new FixedClock(TestSupport.Now));
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "ana@saviaup.test",
            FirstName = "  Ana María ",
            LastName = " Prueba López "
        };

        var token = service.CreateAccessToken(user, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        Assert.Equal(
            "Ana Prueba",
            jwt.Claims.Single(claim => claim.Type == ClaimNames.DisplayName).Value);
    }
}
