using PaperTrade.Application.Markets;

namespace PaperTrade.Infrastructure.Markets;

internal static class SimulatedIndexData
{
    public static MarketQuote Quote(string symbol, DateTimeOffset now)
    {
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(now.ToUnixTimeSeconds() / 300 * 300);
        var current = Value(symbol, timestamp);
        var previous = Value(symbol, timestamp.AddDays(-1));
        var open = Value(symbol, new DateTimeOffset(timestamp.UtcDateTime.Date, TimeSpan.Zero));
        var spread = 0.1m;
        return new MarketQuote(symbol, current, current - previous,
            previous == 0 ? 0 : decimal.Round((current - previous) / previous * 100, 2),
            open, Math.Max(open, current), Math.Min(open, current), previous,
            timestamp, current - spread / 2, current + spread / 2, spread, true,
            "PaperTrade generated index", true);
    }

    public static IReadOnlyList<HistoricalPrice> History(string symbol, DateTimeOffset from,
        DateTimeOffset to, string resolution)
    {
        var step = resolution switch
        {
            "1" => TimeSpan.FromMinutes(1),
            "5" => TimeSpan.FromMinutes(5),
            "15" => TimeSpan.FromMinutes(15),
            "30" => TimeSpan.FromMinutes(30),
            "60" => TimeSpan.FromHours(1),
            "W" => TimeSpan.FromDays(7),
            _ => TimeSpan.FromDays(1)
        };
        var count = Math.Min(500, Math.Max(0, (int)((to - from).Ticks / step.Ticks)));
        var start = to - TimeSpan.FromTicks(step.Ticks * count);
        var result = new List<HistoricalPrice>(count);
        for (var index = 0; index < count; index++)
        {
            var time = start + TimeSpan.FromTicks(step.Ticks * index);
            var open = Value(symbol, time);
            var close = Value(symbol, time + step);
            var wick = Math.Max(open, close) * 0.0008m;
            result.Add(new HistoricalPrice(time, open, Math.Max(open, close) + wick,
                Math.Min(open, close) - wick, close, 0,
                "PaperTrade generated index", true));
        }
        return result;
    }

    private static decimal Value(string symbol, DateTimeOffset time)
    {
        var basis = symbol switch { "PT500" => 5000m, "PT100" => 18000m, _ => 40000m };
        var phase = symbol switch { "PT500" => 1.7d, "PT100" => 3.1d, _ => 4.6d };
        var bucket = time.ToUnixTimeSeconds() / 300d;
        var factor = 1 + 0.027 * Math.Sin(bucket / 1543d + phase) +
            0.011 * Math.Sin(bucket / 329d);
        return decimal.Round(basis * (decimal)factor, 2);
    }
}
