using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaperTrade.Application.Authentication;
using PaperTrade.Infrastructure.Persistence;

namespace PaperTrade.IntegrationTests.Authentication;

public sealed class AuthenticationEndpointsTests
    : IClassFixture<PaperTradeApiFactory>
{
    private const string ValidPassword = "a-long-passphrase";

    private readonly PaperTradeApiFactory _factory;

    public AuthenticationEndpointsTests(
        PaperTradeApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidRequest_CreatesUserAndPortfolio()
    {
        var email = CreateUniqueEmail();
        using var client = CreateClient();

        try
        {
            var response = await RegisterAsync(client, email);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            await using var scope =
                _factory.Services.CreateAsyncScope();

            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<PaperTradeDbContext>();

            var user = await dbContext.Users
                .AsNoTracking()
                .Include(saved => saved.Portfolio)
                .SingleAsync(saved => saved.Email == email);

            Assert.NotEqual(ValidPassword, user.PasswordHash);
            Assert.NotNull(user.Portfolio);
            Assert.Equal(
                "Paper Portfolio",
                user.Portfolio.Name);
            Assert.Equal(
                100_000m,
                user.Portfolio.CashBalance);
            Assert.Equal(
                100_000m,
                user.Portfolio.InitialBalance);
        }
        finally
        {
            await DeleteUserAsync(email);
        }
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = CreateUniqueEmail();
        using var client = CreateClient();

        try
        {
            var firstResponse = await RegisterAsync(client, email);
            var secondResponse = await RegisterAsync(client, email);

            Assert.Equal(
                HttpStatusCode.Created,
                firstResponse.StatusCode);

            Assert.Equal(
                HttpStatusCode.Conflict,
                secondResponse.StatusCode);
        }
        finally
        {
            await DeleteUserAsync(email);
        }
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                "not-an-email",
                ValidPassword,
                "Paper Trader"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                CreateUniqueEmail(),
                "too-short",
                "Paper Trader"));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_CreatesSession()
    {
        var email = CreateUniqueEmail();
        using var registrationClient = CreateClient();

        try
        {
            var registrationResponse = await RegisterAsync(
                registrationClient,
                email);

            Assert.Equal(
                HttpStatusCode.Created,
                registrationResponse.StatusCode);

            using var loginClient = CreateClient();

            var loginResponse = await loginClient.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(email, ValidPassword));

            Assert.Equal(
                HttpStatusCode.OK,
                loginResponse.StatusCode);

            var meResponse = await loginClient.GetAsync(
                "/api/auth/me");

            Assert.Equal(
                HttpStatusCode.OK,
                meResponse.StatusCode);
        }
        finally
        {
            await DeleteUserAsync(email);
        }
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var email = CreateUniqueEmail();
        using var registrationClient = CreateClient();

        try
        {
            await RegisterAsync(registrationClient, email);

            using var loginClient = CreateClient();

            var response = await loginClient.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest(
                    email,
                    "definitely-the-wrong-password"));

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }
        finally
        {
            await DeleteUserAsync(email);
        }
    }

    [Fact]
    public async Task Me_WithoutSession_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Logout_RemovesSession()
    {
        var email = CreateUniqueEmail();
        using var client = CreateClient();

        try
        {
            await RegisterAsync(client, email);

            var beforeLogout = await client.GetAsync(
                "/api/auth/me");

            var logoutResponse = await client.PostAsync(
                "/api/auth/logout",
                content: null);

            var afterLogout = await client.GetAsync(
                "/api/auth/me");

            Assert.Equal(
                HttpStatusCode.OK,
                beforeLogout.StatusCode);

            Assert.Equal(
                HttpStatusCode.NoContent,
                logoutResponse.StatusCode);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                afterLogout.StatusCode);
        }
        finally
        {
            await DeleteUserAsync(email);
        }
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });
    }

    private static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email)
    {
        return client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(
                email,
                ValidPassword,
                "Paper Trader"));
    }

    private async Task DeleteUserAsync(string email)
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<PaperTradeDbContext>();

        await dbContext.Users
            .Where(user => user.Email == email)
            .ExecuteDeleteAsync();
    }

    private static string CreateUniqueEmail()
    {
        return $"day3-{Guid.NewGuid():N}@example.test";
    }
}