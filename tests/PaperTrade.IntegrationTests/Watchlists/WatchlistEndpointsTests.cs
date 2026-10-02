using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaperTrade.Application.Authentication;
using PaperTrade.Application.Watchlists;
using PaperTrade.Infrastructure.Persistence;
using PaperTrade.IntegrationTests.Authentication;

namespace PaperTrade.IntegrationTests.Watchlists;

public sealed class WatchlistEndpointsTests(
    PaperTradeApiFactory factory)
    : IClassFixture<PaperTradeApiFactory>
{
    private const string Password = "a-long-passphrase";

    [Fact]
    public async Task User_CanCreateAddAndRemoveWatchlistItem()
    {
        var email = $"watchlist-{Guid.NewGuid():N}@example.test";
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

        try
        {
            var registration = await client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterRequest(email, Password, "Watch Tester"));

            Assert.Equal(
                HttpStatusCode.Created,
                registration.StatusCode);

            var createResponse = await client.PostAsJsonAsync(
                "/api/watchlists",
                new CreateWatchlistRequest("Technology"));

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);

            var watchlist = await createResponse.Content
                .ReadFromJsonAsync<WatchlistDto>();

            Assert.NotNull(watchlist);

            var addResponse = await client.PostAsJsonAsync(
                $"/api/watchlists/{watchlist.Id}/assets",
                new AddWatchlistItemRequest("aapl"));

            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            var updated = await addResponse.Content
                .ReadFromJsonAsync<WatchlistDto>();

            Assert.Equal("AAPL", Assert.Single(updated!.Items).Symbol);

            var duplicateResponse = await client.PostAsJsonAsync(
                $"/api/watchlists/{watchlist.Id}/assets",
                new AddWatchlistItemRequest("AAPL"));

            Assert.Equal(
                HttpStatusCode.Conflict,
                duplicateResponse.StatusCode);

            var deleteResponse = await client.DeleteAsync(
                $"/api/watchlists/{watchlist.Id}/assets/AAPL");

            Assert.Equal(
                HttpStatusCode.NoContent,
                deleteResponse.StatusCode);

            var list = await client
                .GetFromJsonAsync<WatchlistDto[]>("/api/watchlists");

            Assert.Empty(Assert.Single(list!).Items);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<PaperTradeDbContext>();

            await dbContext.Users
                .Where(user => user.Email == email)
                .ExecuteDeleteAsync();
        }
    }
}
