using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaperTrade.Api.Extensions;
using PaperTrade.Domain.Charts;
using PaperTrade.Domain.Instruments;
using PaperTrade.Infrastructure.Persistence;

namespace PaperTrade.Api.Endpoints;

public static class ChartLayoutEndpoints
{
    public sealed record SaveLayoutRequest(string Name, string Symbol, string Timeframe, JsonElement State);
    public sealed record LayoutDto(Guid Id, string Name, string Symbol, string Timeframe,
        JsonElement State, DateTimeOffset UpdatedAt);

    public static IEndpointRouteBuilder MapChartLayoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chart-layouts").WithTags("Chart layouts").RequireAuthorization();
        group.MapGet("/", async (ClaimsPrincipal principal, PaperTradeDbContext db, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();
            var layouts = await db.ChartLayouts.AsNoTracking().Where(x => x.UserId == userId)
                .OrderByDescending(x => x.UpdatedAt).Take(50).ToListAsync(token);
            return Results.Ok(layouts.Select(Map));
        });
        group.MapPost("/", async (SaveLayoutRequest request, ClaimsPrincipal principal,
            PaperTradeDbContext db, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();
            if (!Valid(request)) return Results.BadRequest(new { error = "Invalid chart layout." });
            if (await db.ChartLayouts.CountAsync(x => x.UserId == userId, token) >= 50)
                return Results.Conflict(new { error = "The 50-layout limit has been reached." });
            var layout = new ChartLayout(Guid.NewGuid(), userId, request.Name,
                request.Symbol, request.Timeframe, request.State.GetRawText(), DateTimeOffset.UtcNow);
            db.ChartLayouts.Add(layout);
            await db.SaveChangesAsync(token);
            return Results.Created($"/api/chart-layouts/{layout.Id}", Map(layout));
        });
        group.MapPut("/{id:guid}", async (Guid id, SaveLayoutRequest request,
            ClaimsPrincipal principal, PaperTradeDbContext db, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();
            if (!Valid(request)) return Results.BadRequest(new { error = "Invalid chart layout." });
            var layout = await db.ChartLayouts.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, token);
            if (layout is null) return Results.NotFound();
            layout.Update(request.Name, request.Symbol, request.Timeframe, request.State.GetRawText(), DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(token);
            return Results.Ok(Map(layout));
        });
        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal principal,
            PaperTradeDbContext db, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();
            var deleted = await db.ChartLayouts.Where(x => x.Id == id && x.UserId == userId).ExecuteDeleteAsync(token);
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
        return app;
    }

    private static bool Valid(SaveLayoutRequest request) =>
        !string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim().Length <= 100 &&
        !string.IsNullOrWhiteSpace(request.Symbol) && request.Symbol.Length <= 32 &&
        request.Symbol.All(c => char.IsLetterOrDigit(c) || c is '/' or '.' or '-') &&
        (!request.Symbol.Contains('/') || SupportedPairs.Create(request.Symbol) is not null) &&
        (request.Timeframe is "1D" or "1W" or "1M" or "3M" or "1Y") &&
        request.State.ValueKind == JsonValueKind.Object && request.State.GetRawText().Length <= 32768;

    private static LayoutDto Map(ChartLayout x)
    {
        using var document = JsonDocument.Parse(x.StateJson);
        return new(x.Id, x.Name, x.Symbol, x.Timeframe,
            document.RootElement.Clone(), x.UpdatedAt);
    }
}
