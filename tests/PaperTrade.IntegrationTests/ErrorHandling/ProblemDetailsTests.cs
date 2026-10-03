using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PaperTrade.Application.Authentication;
using PaperTrade.IntegrationTests.Authentication;

namespace PaperTrade.IntegrationTests.ErrorHandling;

public sealed class ProblemDetailsTests(
    PaperTradeApiFactory factory)
    : IClassFixture<PaperTradeApiFactory>
{
    [Fact]
    public async Task UnknownRoute_ReturnsNotFoundProblem()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/does-not-exist");

        await AssertProblemAsync(
            response,
            HttpStatusCode.NotFound,
            "urn:papertrade:error:not-found",
            "/api/does-not-exist");
    }

    [Fact]
    public async Task ProtectedRoute_ReturnsUnauthorizedProblem()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/markets/AAPL/quote");

        await AssertProblemAsync(
            response,
            HttpStatusCode.Unauthorized,
            "urn:papertrade:error:unauthorized",
            "/api/markets/AAPL/quote");
    }

    [Fact]
    public async Task UnhandledException_ReturnsSafeProblem()
    {
        using var throwingFactory = factory.WithWebHostBuilder(
            builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAuthenticationService>();
                    services.AddScoped<
                        IAuthenticationService,
                        ThrowingAuthenticationService>();
                });
            });

        using var client = throwingFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(
                "trader@example.com",
                "a-long-passphrase"));

        var problem = await AssertProblemAsync(
            response,
            HttpStatusCode.InternalServerError,
            "urn:papertrade:error:internal-server-error",
            "/api/auth/login");

        var responseBody = problem.GetRawText();

        Assert.DoesNotContain(
            ThrowingAuthenticationService.SensitiveMessage,
            responseBody,
            StringComparison.Ordinal);

        Assert.False(problem.TryGetProperty("detail", out _));
    }

    private static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedType,
        string expectedInstance)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            expectedType,
            problem.GetProperty("type").GetString());

        Assert.Equal(
            (int)expectedStatus,
            problem.GetProperty("status").GetInt32());

        Assert.Equal(
            expectedInstance,
            problem.GetProperty("instance").GetString());

        Assert.False(
            string.IsNullOrWhiteSpace(
                problem.GetProperty("traceId").GetString()));

        return problem;
    }

    private sealed class ThrowingAuthenticationService
        : IAuthenticationService
    {
        public const string SensitiveMessage =
            "Synthetic sensitive exception details.";

        public Task<RegistrationResult> RegisterAsync(
            RegisterRequest request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(SensitiveMessage);
        }

        public Task<AuthUser?> AuthenticateAsync(
            LoginRequest request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(SensitiveMessage);
        }

        public Task<AuthUser?> GetUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(SensitiveMessage);
        }
    }
}
