using Microsoft.EntityFrameworkCore;
using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.Users;
using PaperTrade.Infrastructure.Persistence;

namespace PaperTrade.IntegrationTests.Persistence;

public sealed class DatabaseSmokeTests
{
    [Fact]
    public async Task Can_create_and_read_user_with_portfolio()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "PAPERTRADE_TEST_CONNECTION_STRING");

        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set PAPERTRADE_TEST_CONNECTION_STRING before running this test.");

        var options = new DbContextOptionsBuilder<PaperTradeDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var dbContext = new PaperTradeDbContext(options);

        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var user = new User(
            userId,
            $"day2-{userId:N}@example.test",
            "integration-test-hash",
            "Day 2 Test User",
            now);

        var portfolio = new Portfolio(
            Guid.NewGuid(),
            userId,
            "Paper Portfolio",
            100_000m,
            100_000m,
            now);

        dbContext.Users.Add(user);
        dbContext.Portfolios.Add(portfolio);

        try
        {
            await dbContext.SaveChangesAsync();

            var savedUser = await dbContext.Users
                .AsNoTracking()
                .Include(saved => saved.Portfolio)
                .SingleAsync(saved => saved.Id == userId);

            Assert.Equal(user.Email, savedUser.Email);
            Assert.NotNull(savedUser.Portfolio);
            Assert.Equal("Paper Portfolio", savedUser.Portfolio.Name);
            Assert.Equal(100_000m, savedUser.Portfolio.CashBalance);
            Assert.Equal(100_000m, savedUser.Portfolio.InitialBalance);
        }
        finally
        {
            await dbContext.Users
                .Where(saved => saved.Id == userId)
                .ExecuteDeleteAsync();
        }
    }
}