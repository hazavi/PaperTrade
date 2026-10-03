namespace PaperTrade.Application.Leaderboards;

public sealed record LeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string DisplayName,
    decimal PortfolioValue,
    decimal ReturnPercentage);

public sealed record LeaderboardPageDto(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<LeaderboardEntryDto> Entries);
