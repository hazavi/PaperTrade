using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using PaperTrade.Application.Abstractions.Caching;

namespace PaperTrade.Infrastructure.Caching;

internal sealed class RedisCacheService(
    IDistributedCache distributedCache)
    : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken)
    {
        var json = await distributedCache.GetStringAsync(
            key,
            cancellationToken);

        return json is null
            ? default
            : JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value, SerializerOptions);

        return distributedCache.SetStringAsync(
            key,
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeToLive
            },
            cancellationToken);
    }
}
