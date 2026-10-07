namespace PaperTrade.Application.Abstractions.Persistence;

public sealed record LeaderboardAccount(
    Guid UserId,
    string DisplayName,
    decimal CashBalance,
    decimal InitialBalance,
    IReadOnlyList<LeaderboardPosition> Positions);

public sealed record LeaderboardPosition(
    string Symbol,
    decimal Quantity,
    decimal AverageEntryPrice,
    decimal MarginReserved,
    string QuoteCurrency);

public interface ILeaderboardRepository
{
    Task<IReadOnlyList<LeaderboardAccount>> GetAccountsAsync(CancellationToken cancellationToken);
}
