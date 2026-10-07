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
    public async Task LayoutCompetitionAndAdvancedAlerts_AreOwnedAndOptIn()
    {
        var firstEmail = $"phase5-a-{Guid.NewGuid():N}@example.test";
        var secondEmail = $"phase5-b-{Guid.NewGuid():N}@example.test";
        using var first = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = true });
        using var second = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = true });
        try
        {
            await first.PostAsJsonAsync("/api/auth/register", new RegisterRequest(firstEmail, "a-long-passphrase", "First"));
            await second.PostAsJsonAsync("/api/auth/register", new RegisterRequest(secondEmail, "a-long-passphrase", "Second"));
            var alert = await first.PostAsJsonAsync("/api/alerts", new CreatePriceAlertRequest("AAPL", "below", -3m, "percentChange"));
            Assert.Equal(HttpStatusCode.Created, alert.StatusCode);
            Assert.Equal("percentChange", (await alert.Content.ReadFromJsonAsync<PriceAlertDto>())!.Metric);
            Assert.Equal(HttpStatusCode.OK, (await first.PutAsJsonAsync("/api/notification-preferences", new { emailAlertsEnabled = true })).StatusCode);
            var preference = await first.GetFromJsonAsync<EmailPreference>("/api/notification-preferences");
            Assert.True(preference!.EmailAlertsEnabled);
            var layout = await first.PostAsJsonAsync("/api/chart-layouts", new { name = "Test", symbol = "AAPL", timeframe = "1M", state = new { style = "candles", indicators = new[] { "volume" }, drawings = Array.Empty<object>(), drawingsVisible = true } });
            Assert.Equal(HttpStatusCode.Created, layout.StatusCode);
            var saved = await layout.Content.ReadFromJsonAsync<LayoutId>();
            Assert.Equal(HttpStatusCode.NotFound, (await second.DeleteAsync($"/api/chart-layouts/{saved!.Id}")).StatusCode);
            var competition = await first.PostAsJsonAsync("/api/competitions", new { name = "Paper cup", startsAt = DateTimeOffset.UtcNow.AddMinutes(10), endsAt = DateTimeOffset.UtcNow.AddDays(1) });
            Assert.Equal(HttpStatusCode.Created, competition.StatusCode);
            var created = await competition.Content.ReadFromJsonAsync<CompetitionCreated>();
            Assert.Equal(HttpStatusCode.NotFound, (await second.GetAsync($"/api/competitions/{created!.Id}/leaderboard")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await second.PostAsJsonAsync("/api/competitions/join", new { code = created.JoinCode })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await second.GetAsync($"/api/competitions/{created.Id}/leaderboard")).StatusCode);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
            await db.Users.Where(user => user.Email == firstEmail || user.Email == secondEmail).ExecuteDeleteAsync();
        }
    }

    private sealed record EmailPreference(bool EmailAlertsEnabled);
    private sealed record LayoutId(Guid Id);
    private sealed record CompetitionCreated(Guid Id, string JoinCode);
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
