using Microsoft.AspNetCore.Identity;
using PaperTrade.Application.Abstractions.Security;

namespace PaperTrade.Infrastructure.Security;

internal sealed class AspNetCorePasswordHasher : IPasswordHasher
{
    private static readonly object UserPlaceholder = new();

    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(
            UserPlaceholder,
            password);
    }

    public bool Verify(string passwordHash, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            UserPlaceholder,
            passwordHash,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}