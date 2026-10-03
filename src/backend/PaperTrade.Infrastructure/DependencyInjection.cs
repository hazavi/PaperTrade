using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Abstractions.Security;
using PaperTrade.Application.Abstractions.Caching;
using PaperTrade.Application.Markets;
using PaperTrade.Infrastructure.Caching;
using PaperTrade.Infrastructure.Markets;
using PaperTrade.Infrastructure.Persistence;
using PaperTrade.Infrastructure.Persistence.Repositories;
using PaperTrade.Infrastructure.Security;

namespace PaperTrade.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<PaperTradeDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        var redisConnectionString =
            configuration["Redis:ConnectionString"]
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "papertrade:";
        });

        services.Configure<FinnhubOptions>(
            configuration.GetSection(FinnhubOptions.SectionName));

        services.AddHttpClient<FinnhubMarketDataService>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<
                        Microsoft.Extensions.Options
                            .IOptions<FinnhubOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
            });

        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddScoped<IMarketDataService>(serviceProvider =>
            new CachedMarketDataService(
                serviceProvider.GetRequiredService<
                    FinnhubMarketDataService>(),
                serviceProvider.GetRequiredService<ICacheService>(),
                serviceProvider.GetRequiredService<
                    Microsoft.Extensions.Logging
                        .ILogger<CachedMarketDataService>>()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IWatchlistRepository, WatchlistRepository>();
        services.AddScoped<ITradingRepository, TradingRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();

        return services;
    }
}
