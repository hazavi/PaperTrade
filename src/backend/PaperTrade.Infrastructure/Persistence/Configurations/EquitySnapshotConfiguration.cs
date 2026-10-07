using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Portfolios;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class EquitySnapshotConfiguration : IEntityTypeConfiguration<EquitySnapshot>
{
    public void Configure(EntityTypeBuilder<EquitySnapshot> builder)
    {
        builder.ToTable("equity_snapshots");
        builder.HasKey(x => x.Id).HasName("pk_equity_snapshots");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(x => x.RecordedAt).HasColumnName("recorded_at");
        builder.Property(x => x.Equity).HasColumnName("equity").HasPrecision(18, 2);
        builder.Property(x => x.Cash).HasColumnName("cash").HasPrecision(18, 2);
        builder.Property(x => x.RealizedPnl).HasColumnName("realized_pnl").HasPrecision(18, 2);
        builder.Property(x => x.UnrealizedPnl).HasColumnName("unrealized_pnl").HasPrecision(18, 2);
        builder.HasIndex(x => new { x.PortfolioId, x.RecordedAt }).HasDatabaseName("ix_equity_snapshots_portfolio_id_recorded_at");
        builder.HasOne(x => x.Portfolio).WithMany().HasForeignKey(x => x.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_equity_snapshots_portfolios_portfolio_id");
    }
}
