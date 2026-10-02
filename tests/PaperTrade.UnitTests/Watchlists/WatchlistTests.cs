using PaperTrade.Domain.Watchlists;

namespace PaperTrade.UnitTests.Watchlists;

public sealed class WatchlistTests
{
    [Fact]
    public void AddItem_NormalizesSymbol()
    {
        var watchlist = CreateWatchlist();

        var item = watchlist.AddItem(
            Guid.NewGuid(),
            " aapl ",
            DateTimeOffset.UtcNow);

        Assert.Equal("AAPL", item.Symbol);
        Assert.Single(watchlist.Items);
    }

    [Fact]
    public void AddItem_WithDuplicateSymbol_Throws()
    {
        var watchlist = CreateWatchlist();

        watchlist.AddItem(
            Guid.NewGuid(),
            "AAPL",
            DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            watchlist.AddItem(
                Guid.NewGuid(),
                "aapl",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RemoveItem_RemovesMatchingSymbol()
    {
        var watchlist = CreateWatchlist();

        watchlist.AddItem(
            Guid.NewGuid(),
            "MSFT",
            DateTimeOffset.UtcNow);

        var removed = watchlist.RemoveItem("msft");

        Assert.True(removed);
        Assert.Empty(watchlist.Items);
    }

    private static Watchlist CreateWatchlist()
    {
        return new Watchlist(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Technology",
            DateTimeOffset.UtcNow);
    }
}
