using Microsoft.EntityFrameworkCore;
using PaperTrade.Application.Portfolios;
using PaperTrade.Infrastructure.Persistence;

namespace PaperTrade.Api.BackgroundServices;

public sealed class CompetitionWorker(IServiceScopeFactory scopes, ILogger<CompetitionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(token))
        {
            try { await ProcessAsync(token); }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Competition scoring cycle failed");
            }
        }
    }

    private async Task ProcessAsync(CancellationToken token)
    {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
            var portfolios = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
            var now = DateTimeOffset.UtcNow;
            var pending = await db.CompetitionMembers.Join(db.Competitions,
                member => member.CompetitionId, competition => competition.Id,
                (member, competition) => new { Member = member, competition.StartsAt, competition.EndsAt })
                .Where(x => x.Member.StartingEquity == null && x.StartsAt <= now && x.EndsAt > now)
                .ToListAsync(token);
            foreach (var item in pending)
            {
                try
                {
                    var portfolio = await portfolios.GetAsync(item.Member.UserId, token);
                    if (portfolio is null) continue;
                    item.Member.RecordStartingEquity(portfolio.PortfolioValue);
                    await db.SaveChangesAsync(token);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(exception, "Could not record competition start equity for {UserId}", item.Member.UserId);
                }
            }
            var ending = await db.CompetitionMembers.Join(db.Competitions,
                member => member.CompetitionId, competition => competition.Id,
                (member, competition) => new { Member = member, competition.EndsAt })
                .Where(x => x.Member.StartingEquity != null && x.Member.EndingEquity == null && x.EndsAt <= now)
                .ToListAsync(token);
            foreach (var item in ending)
            {
                try
                {
                    var portfolio = await portfolios.GetAsync(item.Member.UserId, token);
                    if (portfolio is null) continue;
                    item.Member.RecordEndingEquity(portfolio.PortfolioValue);
                    await db.SaveChangesAsync(token);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(exception, "Could not record competition end equity for {UserId}", item.Member.UserId);
                }
            }
    }
}
