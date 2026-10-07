using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Trades;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("trades", table =>
        {
            table.HasCheckConstraint("ck_trades_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_trades_price_positive", "price > 0");
            table.HasCheckConstraint("ck_trades_total_value_positive", "total_value > 0");
            table.HasCheckConstraint("ck_trades_fee_nonnegative", "fee >= 0");
        });
        builder.HasKey(trade => trade.Id).HasName("pk_trades");
        builder.Property(trade => trade.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(trade => trade.OrderId).HasColumnName("order_id").IsRequired();
        builder.Property(trade => trade.PortfolioId).HasColumnName("portfolio_id").IsRequired();
        builder.Property(trade => trade.InstrumentId).HasColumnName("instrument_id").IsRequired();
        builder.Property(trade => trade.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
        builder.Property(trade => trade.Side).HasColumnName("side").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(trade => trade.Quantity).HasColumnName("quantity").HasPrecision(18, 6).IsRequired();
        builder.Property(trade => trade.Price).HasColumnName("price").HasPrecision(18, 6).IsRequired();
        builder.Property(trade => trade.TotalValue).HasColumnName("total_value").HasPrecision(18, 2).IsRequired();
        builder.Property(trade => trade.RealizedPnl).HasColumnName("realized_pnl").HasPrecision(18, 2).IsRequired();
        builder.Property(trade => trade.Fee).HasColumnName("fee").HasPrecision(18, 2).IsRequired();
        builder.Property(trade => trade.QuotePrice).HasColumnName("quote_price").HasPrecision(18, 6).IsRequired();
        builder.Property(trade => trade.ExecutedAt).HasColumnName("executed_at").IsRequired();
        builder.HasIndex(trade => trade.OrderId).HasDatabaseName("ix_trades_order_id");
        builder.HasIndex(trade => new { trade.PortfolioId, trade.ExecutedAt }).HasDatabaseName("ix_trades_portfolio_id_executed_at");
        builder.HasOne(trade => trade.Order).WithMany().HasForeignKey(trade => trade.OrderId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_trades_orders_order_id");
        builder.HasOne(trade => trade.Portfolio).WithMany().HasForeignKey(trade => trade.PortfolioId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_trades_portfolios_portfolio_id");
        builder.HasOne(trade => trade.Instrument).WithMany().HasForeignKey(trade => trade.InstrumentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_trades_instruments_instrument_id");
    }
}
