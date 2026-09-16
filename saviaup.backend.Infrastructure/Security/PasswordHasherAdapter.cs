using Microsoft.AspNetCore.Identity;
using SaviaUp.Backend.Domain.Entities;
using SaviaUp.Backend.Domain.Ports;

namespace SaviaUp.Backend.Infrastructure.Security;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new User(), password);

    public bool Verify(string passwordHash, string providedPassword)
        => _hasher.VerifyHashedPassword(new User(), passwordHash, providedPassword) != PasswordVerificationResult.Failed;
}
