using Microsoft.AspNetCore.DataProtection;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Security;

public sealed class OrganizationAccessLinkCodeProtector(IDataProtectionProvider provider)
    : IOrganizationAccessLinkCodeProtector
{
    private readonly IDataProtector protector = provider.CreateProtector("SaviaUp.OrganizationAccessLinkCode.v1");

    public string Protect(string code) => protector.Protect(code);

    public string Unprotect(string protectedCode) => protector.Unprotect(protectedCode);
}
