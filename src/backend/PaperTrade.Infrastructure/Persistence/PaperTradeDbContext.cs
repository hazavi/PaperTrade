using Microsoft.EntityFrameworkCore;
using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.Users;

namespace PaperTrade.Infrastructure.Persistence;

public sealed class PaperTradeDbContext(
    DbContextOptions<PaperTradeDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PaperTradeDbContext).Assembly);
    }
}