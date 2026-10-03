using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PaperTrade.Application.Markets;

namespace PaperTrade.Api.Realtime;

[Authorize]
public sealed class MarketHub(
    MarketSubscriptionTracker tracker,
    IMarketDataService marketDataService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }

    public async Task Subscribe(string symbol)
    {
        var normalized = Normalize(symbol);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"market:{normalized}");
        tracker.Add(Context.ConnectionId, normalized);
        var quote = await marketDataService.GetQuoteAsync(normalized, Context.ConnectionAborted);
        if (quote is not null) await Clients.Caller.SendAsync("QuoteUpdated", quote, Context.ConnectionAborted);
    }

    public async Task Unsubscribe(string symbol)
    {
        var normalized = Normalize(symbol);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"market:{normalized}");
        tracker.Remove(Context.ConnectionId, normalized);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        tracker.RemoveConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string Normalize(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var normalized = symbol.Trim().ToUpperInvariant();
        if (normalized.Length > 32 || normalized.Any(character =>
                !char.IsLetterOrDigit(character) && character is not '.' and not '-'))
            throw new HubException("Invalid symbol.");
        return normalized;
    }
}
