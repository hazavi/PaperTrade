using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PaperTrade.Application.Authentication;
using PaperTrade.Application.Authentication.Validation;
using PaperTrade.Application.Watchlists;
using PaperTrade.Application.Watchlists.Validation;

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

        return services;
    }
}
