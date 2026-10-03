using PaperTrade.Application.Leaderboards;

namespace PaperTrade.Api.Endpoints;

public static class LeaderboardEndpoints
{
    public static IEndpointRouteBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/leaderboard", async (int? page, int? pageSize,
            ILeaderboardService service, CancellationToken token) =>
        {
            var selectedPage = Math.Max(1, page ?? 1);
            var selectedSize = Math.Clamp(pageSize ?? 20, 1, 100);
            return Results.Ok(await service.GetAsync(selectedPage, selectedSize, token));
        }).WithTags("Leaderboard").RequireAuthorization();
        return endpoints;
    }
}
