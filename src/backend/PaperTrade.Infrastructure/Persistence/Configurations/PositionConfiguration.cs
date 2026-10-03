using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Positions;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions", table =>
        {
            table.HasCheckConstraint("ck_positions_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_positions_average_entry_price_positive", "average_entry_price > 0");
        });
        builder.HasKey(position => position.Id).HasName("pk_positions");
        builder.Property(position => position.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(position => position.PortfolioId).HasColumnName("portfolio_id").IsRequired();
        builder.Property(position => position.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
        builder.Property(position => position.Quantity).HasColumnName("quantity").HasPrecision(18, 6).IsRequired();
        builder.Property(position => position.AverageEntryPrice).HasColumnName("average_entry_price").HasPrecision(18, 6).IsRequired();
        builder.Property(position => position.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(position => position.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.HasIndex(position => new { position.PortfolioId, position.Symbol }).IsUnique().HasDatabaseName("ux_positions_portfolio_id_symbol");
        builder.HasOne(position => position.Portfolio).WithMany().HasForeignKey(position => position.PortfolioId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_positions_portfolios_portfolio_id");
    }
}
