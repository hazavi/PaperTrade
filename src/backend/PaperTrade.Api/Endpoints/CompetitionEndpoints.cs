using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PaperTrade.Api.Extensions;
using PaperTrade.Application.Portfolios;
using PaperTrade.Domain.Competitions;
using PaperTrade.Infrastructure.Persistence;

namespace PaperTrade.Api.Endpoints;

public static class CompetitionEndpoints
{
    public sealed record CreateRequest(string Name, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
    public sealed record JoinRequest(string Code);
    public sealed record LeaderboardEntry(Guid UserId, string DisplayName, decimal? StartingEquity, decimal? CurrentEquity, decimal? ReturnPercent);
    public static IEndpointRouteBuilder MapCompetitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/competitions").WithTags("Competitions").RequireAuthorization();
        group.MapGet("/", async (ClaimsPrincipal principal, PaperTradeDbContext db, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var id)) return Results.Unauthorized();
            var ids = db.CompetitionMembers.Where(x => x.UserId == id).Select(x => x.CompetitionId);
            return Results.Ok(await db.Competitions.AsNoTracking().Where(x => ids.Contains(x.Id))
                .OrderByDescending(x => x.StartsAt).Select(x => new { x.Id, x.Name, x.StartsAt, x.EndsAt, x.JoinCode }).ToListAsync(token));
        });
        group.MapPost("/", async (CreateRequest request, ClaimsPrincipal principal, PaperTradeDbContext db, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var id)) return Results.Unauthorized();
            var now = DateTimeOffset.UtcNow;
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100 ||
                request.StartsAt < now.AddMinutes(5) || request.StartsAt > now.AddDays(30) ||
                request.EndsAt <= request.StartsAt || request.EndsAt > request.StartsAt.AddDays(90))
                return Results.BadRequest(new { error = "Choose a name, a start at least five minutes ahead, and an end within 90 days." });
            var competition = new Competition(Guid.NewGuid(), id, request.Name.Trim(),
                Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(), request.StartsAt, request.EndsAt);
            db.Competitions.Add(competition);
            db.CompetitionMembers.Add(new CompetitionMember(competition.Id, id, now));
            await db.SaveChangesAsync(token);
            return Results.Created($"/api/competitions/{competition.Id}", new { competition.Id, competition.Name, competition.StartsAt, competition.EndsAt, competition.JoinCode });
        });
        group.MapPost("/join", async (JoinRequest request, ClaimsPrincipal principal, PaperTradeDbContext db, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var id)) return Results.Unauthorized();
            var competition = await db.Competitions.SingleOrDefaultAsync(x => x.JoinCode == request.Code.Trim().ToUpper(), token);
            if (competition is null) return Results.NotFound();
            if (DateTimeOffset.UtcNow >= competition.StartsAt) return Results.Conflict(new { error = "Registration has closed." });
            if (await db.CompetitionMembers.AnyAsync(x => x.CompetitionId == competition.Id && x.UserId == id, token)) return Results.Ok(new { competition.Id });
            db.CompetitionMembers.Add(new CompetitionMember(competition.Id, id, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(token);
            return Results.Ok(new { competition.Id });
        });
        group.MapGet("/{id:guid}/leaderboard", async (Guid id, ClaimsPrincipal principal, PaperTradeDbContext db,
            IPortfolioService portfolios, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();
            var competition = await db.Competitions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
            if (competition is null || !await db.CompetitionMembers.AnyAsync(x => x.CompetitionId == id && x.UserId == userId, token)) return Results.NotFound();
            var members = await db.CompetitionMembers.AsNoTracking().Where(x => x.CompetitionId == id).Join(db.Users,
                member => member.UserId, user => user.Id, (member, user) => new { member.UserId, user.DisplayName, member.StartingEquity, member.EndingEquity }).ToListAsync(token);
            var rows = new List<LeaderboardEntry>();
            foreach (var member in members)
            {
                decimal? current = member.EndingEquity;
                if (current is null && member.StartingEquity is > 0 && DateTimeOffset.UtcNow >= competition.StartsAt && DateTimeOffset.UtcNow < competition.EndsAt)
                {
                    try { current = (await portfolios.GetAsync(member.UserId, token))?.PortfolioValue; }
                    catch (Exception exception) when (exception is not OperationCanceledException) { /* Mark quote unavailable for this entry. */ }
                }
                var returnPercent = current is null ? (decimal?)null : decimal.Round((current.Value / member.StartingEquity!.Value - 1) * 100, 2);
                rows.Add(new LeaderboardEntry(member.UserId, member.DisplayName, member.StartingEquity, current, returnPercent));
            }
            return Results.Ok(new { competition.Id, competition.Name, competition.StartsAt, competition.EndsAt,
                Entries = rows.OrderByDescending(x => x.ReturnPercent).ToArray() });
        });
        return app;
    }
}
