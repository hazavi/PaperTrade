namespace PaperTrade.Application.Leaderboards;

public interface ILeaderboardService
{
    Task<LeaderboardPageDto> GetAsync(int page, int pageSize, CancellationToken cancellationToken);
}
