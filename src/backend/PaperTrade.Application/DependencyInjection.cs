using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PaperTrade.Application.Authentication;
using PaperTrade.Application.Authentication.Validation;
using PaperTrade.Application.Watchlists;
using PaperTrade.Application.Watchlists.Validation;
using PaperTrade.Application.Trading;
using PaperTrade.Application.Trading.Validation;
using PaperTrade.Application.Portfolios;
using PaperTrade.Application.Engagement;
using PaperTrade.Application.Engagement.Validation;
using PaperTrade.Application.Leaderboards;
using PaperTrade.Application.Markets;

namespace PaperTrade.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        services.AddScoped<
            IValidator<RegisterRequest>,
            RegisterRequestValidator>();

        services.AddScoped<
            IValidator<LoginRequest>,
            LoginRequestValidator>();

        services.AddScoped<IWatchlistService, WatchlistService>();

        services.AddScoped<
            IValidator<CreateWatchlistRequest>,
            CreateWatchlistRequestValidator>();

        services.AddScoped<
            IValidator<AddWatchlistItemRequest>,
            AddWatchlistItemRequestValidator>();

        services.AddScoped<ITradingService, TradingService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<RiskAnalyticsService>();
        services.AddScoped<MarginService>();
        services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
        services.AddScoped<IEngagementService, EngagementService>();
        services.AddScoped<IValidator<CreatePriceAlertRequest>, CreatePriceAlertRequestValidator>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IInstrumentCatalog, InstrumentCatalog>();

        return services;
    }
}
