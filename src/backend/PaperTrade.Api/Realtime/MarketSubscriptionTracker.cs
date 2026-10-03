using System.Collections.Concurrent;

namespace PaperTrade.Api.Realtime;

public sealed class MarketSubscriptionTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();

    public void Add(string connectionId, string symbol)
    {
        var symbols = _connections.GetOrAdd(connectionId, _ => []);
        lock (symbols) symbols.Add(symbol);
    }

    public void Remove(string connectionId, string symbol)
    {
        if (!_connections.TryGetValue(connectionId, out var symbols)) return;
        lock (symbols) symbols.Remove(symbol);
    }

    public void RemoveConnection(string connectionId) => _connections.TryRemove(connectionId, out _);

    public IReadOnlyList<string> GetSymbols() => _connections.Values
        .SelectMany(symbols =>
        {
            lock (symbols) return symbols.ToArray();
        })
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}
