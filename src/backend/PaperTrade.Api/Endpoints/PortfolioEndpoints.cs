using System.Security.Claims;
using PaperTrade.Api.ErrorHandling;
using PaperTrade.Api.Extensions;
using PaperTrade.Application.Portfolios;

namespace PaperTrade.Api.Endpoints;

public static class PortfolioEndpoints
{
    public static IEndpointRouteBuilder MapPortfolioEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/portfolio", GetAsync)
            .WithTags("Portfolio")
            .RequireAuthorization();
        var margin = endpoints.MapGroup("/api/portfolio/margin").WithTags("Portfolio").RequireAuthorization();
        margin.MapGet("/", async (ClaimsPrincipal principal, MarginService service, CancellationToken token) =>
            principal.TryGetUserId(out var userId) ? await service.GetAsync(userId, token) is { } settings
                ? Results.Ok(settings) : Results.NotFound() : Results.Unauthorized());
        margin.MapPut("/", async (MarginSettingsDto settings, ClaimsPrincipal principal,
            MarginService service, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();
            if (settings.EquityLeverage is < 1 or > 5 || settings.ForexLeverage is < 1 or > 10 ||
                settings.MetalLeverage is < 1 or > 5 || settings.CommodityLeverage is < 1 or > 5 ||
                settings.IndexLeverage is < 1 or > 5 || settings.CryptoLeverage is < 1 or > 3)
                return Results.BadRequest(new { error = "Leverage is outside the simulation cap." });
            return await service.SetAsync(userId, settings, token) is { } saved
                ? Results.Ok(saved) : Results.NotFound();
        });
        margin.MapGet("/charges", async (ClaimsPrincipal principal, MarginService service, CancellationToken token) =>
            principal.TryGetUserId(out var userId) ? await service.ChargesAsync(userId, token) is { } charges
                ? Results.Ok(charges) : Results.NotFound() : Results.Unauthorized());
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        ClaimsPrincipal principal,
        IPortfolioService portfolioService,
        CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();

        var portfolio = await portfolioService.GetAsync(userId, cancellationToken);
        return portfolio is null
            ? Results.Problem(type: ApiProblemTypes.NotFound,
                title: "The portfolio was not found.",
                statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(portfolio);
    }
}
