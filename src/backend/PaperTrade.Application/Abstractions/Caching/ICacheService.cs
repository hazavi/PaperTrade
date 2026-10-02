namespace PaperTrade.Application.Abstractions.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan timeToLive,
        CancellationToken cancellationToken);
}
