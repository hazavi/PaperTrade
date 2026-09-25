using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaperTrade.Application.Abstractions.Security;
using PaperTrade.Infrastructure;

namespace PaperTrade.UnitTests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        using var serviceProvider = CreateServiceProvider();
        var passwordHasher =
            serviceProvider.GetRequiredService<IPasswordHasher>();

        var firstHash = passwordHasher.Hash("a-long-passphrase");
        var secondHash = passwordHasher.Hash("a-long-passphrase");

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        using var serviceProvider = CreateServiceProvider();
        var passwordHasher =
            serviceProvider.GetRequiredService<IPasswordHasher>();

        var hash = passwordHasher.Hash("a-long-passphrase");

        var verified = passwordHasher.Verify(
            hash,
            "a-long-passphrase");

        Assert.True(verified);
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        using var serviceProvider = CreateServiceProvider();
        var passwordHasher =
            serviceProvider.GetRequiredService<IPasswordHasher>();

        var hash = passwordHasher.Hash("a-long-passphrase");

        var verified = passwordHasher.Verify(
            hash,
            "the-wrong-password");

        Assert.False(verified);
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Database=unused"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}