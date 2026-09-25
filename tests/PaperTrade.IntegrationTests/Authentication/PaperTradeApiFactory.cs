using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PaperTrade.IntegrationTests.Authentication;

public sealed class PaperTradeApiFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "PAPERTRADE_TEST_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set PAPERTRADE_TEST_CONNECTION_STRING " +
                "before running integration tests.");
        }

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            connectionString
                    });
            });
    }
}