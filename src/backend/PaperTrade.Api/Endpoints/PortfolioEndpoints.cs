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
