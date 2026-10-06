using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaperTrade.Application.Authentication;
using PaperTrade.Application.Engagement;
using PaperTrade.Domain.Notifications;
using PaperTrade.Infrastructure.Persistence;
using PaperTrade.IntegrationTests.Authentication;

namespace PaperTrade.IntegrationTests.Engagement;

public sealed class EngagementEndpointsTests(PaperTradeApiFactory factory)
    : IClassFixture<PaperTradeApiFactory>
{
    [Fact]
    public async Task UserCanCreateDeleteAlertAndReadNotification()
    {
        var email = $"alerts-{Guid.NewGuid():N}@example.test";
        using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = true });
        try
        {
            await client.PostAsJsonAsync("/api/auth/register",
                new RegisterRequest(email, "a-long-passphrase", "Alert Tester"));
            var createdResponse = await client.PostAsJsonAsync("/api/alerts",
                new CreatePriceAlertRequest("aapl", "above", 200));
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var created = await createdResponse.Content.ReadFromJsonAsync<PriceAlertDto>();
            Assert.Equal("AAPL", created!.Symbol);
            Assert.NotEqual(Guid.Empty, created.InstrumentId);
            Assert.Equal("AAPL", created.Instrument!.Symbol);

            Guid notificationId;
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
                var userId = await db.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
                notificationId = Guid.NewGuid();
                db.Notifications.Add(new Notification(notificationId, userId,
                    "Test notification", "A test message.", DateTimeOffset.UtcNow));
                await db.SaveChangesAsync();
            }

            var notifications = await client.GetFromJsonAsync<NotificationDto[]>("/api/notifications");
            Assert.Contains(notifications!, notification => notification.Id == notificationId && !notification.IsRead);
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PostAsync($"/api/notifications/{notificationId}/read", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.DeleteAsync($"/api/alerts/{created.Id}")).StatusCode);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
            await db.Users.Where(user => user.Email == email).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task HealthEndpointsAreAvailableWithoutAuthentication()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }
}
