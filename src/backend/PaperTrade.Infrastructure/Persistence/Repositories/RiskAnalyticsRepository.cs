using Microsoft.EntityFrameworkCore;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.TradingJournal;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class RiskAnalyticsRepository(PaperTradeDbContext db) : IRiskAnalyticsRepository
{
    public async Task<IReadOnlyList<EquitySnapshot>> GetSnapshotsAsync(Guid id, CancellationToken token) =>
        await db.EquitySnapshots.AsNoTracking().Where(x => x.PortfolioId == id)
            .OrderBy(x => x.RecordedAt).ToListAsync(token);
    public Task<EquitySnapshot?> GetLatestSnapshotAsync(Guid id, CancellationToken token) =>
        db.EquitySnapshots.AsNoTracking().Where(x => x.PortfolioId == id)
            .OrderByDescending(x => x.RecordedAt).FirstOrDefaultAsync(token);
    public void AddSnapshot(EquitySnapshot snapshot) => db.EquitySnapshots.Add(snapshot);
    public async Task<IReadOnlyList<JournalEntry>> GetJournalAsync(Guid id, CancellationToken token) =>
        await db.JournalEntries.AsNoTracking().Where(x => x.PortfolioId == id)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(token);
    public Task<JournalEntry?> GetJournalEntryAsync(Guid id, Guid orderId, CancellationToken token) =>
        db.JournalEntries.SingleOrDefaultAsync(x => x.PortfolioId == id && x.OrderId == orderId, token);
    public void AddJournalEntry(JournalEntry entry) => db.JournalEntries.Add(entry);
    public void RemoveJournalEntry(JournalEntry entry) => db.JournalEntries.Remove(entry);
}
