using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Abstractions.Security;
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

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();

        return services;
    }
}