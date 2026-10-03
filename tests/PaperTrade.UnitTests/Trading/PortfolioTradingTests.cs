using PaperTrade.Domain.Portfolios;

namespace PaperTrade.UnitTests.Trading;

public sealed class PortfolioTradingTests
{
    [Fact]
    public void Debit_CannotExceedAvailableCash()
    {
        var portfolio = CreatePortfolio();

        Assert.Throws<InvalidOperationException>(() => portfolio.Debit(100_001));
        Assert.Equal(100_000, portfolio.CashBalance);
    }

    [Fact]
    public void CreditAndRealizedPnl_UpdatePortfolio()
    {
        var portfolio = CreatePortfolio();
        portfolio.Debit(1_000);
        portfolio.Credit(1_200);
        portfolio.RecordRealizedPnl(200);

        Assert.Equal(100_200, portfolio.CashBalance);
        Assert.Equal(200, portfolio.RealizedPnl);
    }

    private static Portfolio CreatePortfolio() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Paper Portfolio",
            100_000, 100_000, DateTimeOffset.UtcNow);
}
