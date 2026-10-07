using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Orders;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", table =>
        {
            table.HasCheckConstraint("ck_orders_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_orders_requested_price_positive", "requested_price > 0");
            table.HasCheckConstraint("ck_orders_executed_price_positive", "executed_price IS NULL OR executed_price > 0");
            table.HasCheckConstraint("ck_orders_filled_quantity_range", "filled_quantity >= 0 AND filled_quantity <= quantity");
        });
        builder.HasKey(order => order.Id).HasName("pk_orders");
        builder.Property(order => order.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(order => order.PortfolioId).HasColumnName("portfolio_id").IsRequired();
        builder.Property(order => order.InstrumentId).HasColumnName("instrument_id").IsRequired();
        builder.Property(order => order.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
        builder.Property(order => order.Side).HasColumnName("side").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(order => order.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(order => order.Quantity).HasColumnName("quantity").HasPrecision(18, 6).IsRequired();
        builder.Property(order => order.RequestedPrice).HasColumnName("requested_price").HasPrecision(18, 6).IsRequired();
        builder.Property(order => order.ExecutedPrice).HasColumnName("executed_price").HasPrecision(18, 6);
        builder.Property(order => order.FilledQuantity).HasColumnName("filled_quantity").HasPrecision(18, 6).IsRequired();
        builder.Property(order => order.ParentOrderId).HasColumnName("parent_order_id");
        builder.Property(order => order.ExpiresAt).HasColumnName("expires_at");
        builder.Property(order => order.ClosedAt).HasColumnName("closed_at");
        builder.Property(order => order.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(order => order.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(order => order.ExecutedAt).HasColumnName("executed_at");
        builder.HasIndex(order => new { order.PortfolioId, order.CreatedAt }).HasDatabaseName("ix_orders_portfolio_id_created_at");
        builder.HasIndex(order => new { order.Status, order.CreatedAt }).HasDatabaseName("ix_orders_status_created_at");
        builder.HasIndex(order => order.ParentOrderId).HasDatabaseName("ix_orders_parent_order_id");
        builder.HasOne<Order>().WithMany().HasForeignKey(order => order.ParentOrderId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_orders_orders_parent_order_id");
        builder.HasOne(order => order.Portfolio).WithMany().HasForeignKey(order => order.PortfolioId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_orders_portfolios_portfolio_id");
        builder.HasOne(order => order.Instrument).WithMany().HasForeignKey(order => order.InstrumentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_orders_instruments_instrument_id");
    }
}
