using Microsoft.EntityFrameworkCore;
using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.Users;
using PaperTrade.Domain.Watchlists;
using PaperTrade.Domain.Orders;
using PaperTrade.Domain.Positions;
using PaperTrade.Domain.Trades;
using PaperTrade.Domain.Alerts;
using PaperTrade.Domain.Notifications;
using PaperTrade.Domain.Instruments;
using PaperTrade.Domain.TradingJournal;

namespace PaperTrade.Infrastructure.Persistence;

public sealed class PaperTradeDbContext(
    DbContextOptions<PaperTradeDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<EquitySnapshot> EquitySnapshots => Set<EquitySnapshot>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    public DbSet<Watchlist> Watchlists => Set<Watchlist>();

    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Trade> Trades => Set<Trade>();

    public DbSet<Position> Positions => Set<Position>();

    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<ProviderSymbol> ProviderSymbols => Set<ProviderSymbol>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(PaperTradeDbContext).Assembly);
    }
}
