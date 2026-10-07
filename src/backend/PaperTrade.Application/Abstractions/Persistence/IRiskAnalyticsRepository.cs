using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.TradingJournal;

namespace PaperTrade.Application.Abstractions.Persistence;

public interface IRiskAnalyticsRepository
{
    Task<IReadOnlyList<EquitySnapshot>> GetSnapshotsAsync(Guid portfolioId, CancellationToken token);
    Task<EquitySnapshot?> GetLatestSnapshotAsync(Guid portfolioId, CancellationToken token);
    void AddSnapshot(EquitySnapshot snapshot);
    Task<IReadOnlyList<JournalEntry>> GetJournalAsync(Guid portfolioId, CancellationToken token);
    Task<JournalEntry?> GetJournalEntryAsync(Guid portfolioId, Guid orderId, CancellationToken token);
    void AddJournalEntry(JournalEntry entry);
    void RemoveJournalEntry(JournalEntry entry);
}
