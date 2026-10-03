using PaperTrade.Application.Abstractions.Caching;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Markets;
using Microsoft.Extensions.Logging;

namespace PaperTrade.Application.Leaderboards;

public sealed class LeaderboardService(
    ILeaderboardRepository repository,
    IMarketDataService marketDataService,
    ICacheService cacheService,
    ILogger<LeaderboardService> logger) : ILeaderboardService
{
    private const string CacheKey = "leaderboard:all";

    public async Task<LeaderboardPageDto> GetAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        LeaderboardEntryDto[]? entries = null;
        try
        {
            entries = await cacheService.GetAsync<LeaderboardEntryDto[]>(CacheKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Leaderboard cache read failed");
        }
        if (entries is null)
        {
            entries = await BuildAsync(cancellationToken);
            try
            {
                await cacheService.SetAsync(CacheKey, entries, TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Leaderboard cache write failed");
            }
        }

        return new LeaderboardPageDto(page, pageSize, entries.Length,
            entries.Skip((page - 1) * pageSize).Take(pageSize).ToArray());
    }

    private async Task<LeaderboardEntryDto[]> BuildAsync(CancellationToken cancellationToken)
    {
        var accounts = await repository.GetAccountsAsync(cancellationToken);
        var symbols = accounts.SelectMany(account => account.Positions)
            .Select(position => position.Symbol).Distinct().ToArray();
        var quoteTasks = symbols.ToDictionary(symbol => symbol,
            symbol => marketDataService.GetQuoteAsync(symbol, cancellationToken));
        await Task.WhenAll(quoteTasks.Values);
        var prices = quoteTasks.ToDictionary(pair => pair.Key,
            pair => pair.Value.Result?.CurrentPrice ?? 0m);

        return accounts.Select(account =>
            {
                var value = account.CashBalance + account.Positions.Sum(position =>
                    position.Quantity * prices.GetValueOrDefault(position.Symbol));
                var returnPercentage = account.InitialBalance == 0 ? 0 :
                    decimal.Round((value - account.InitialBalance) /
                        account.InitialBalance * 100, 2, MidpointRounding.AwayFromZero);
                return new
                {
                    account.UserId,
                    account.DisplayName,
                    Value = decimal.Round(value, 2),
                    Return = returnPercentage
                };
            })
            .OrderByDescending(entry => entry.Return)
            .ThenBy(entry => entry.DisplayName)
            .Select((entry, index) => new LeaderboardEntryDto(index + 1,
                entry.UserId, entry.DisplayName, entry.Value, entry.Return))
            .ToArray();
    }
}
