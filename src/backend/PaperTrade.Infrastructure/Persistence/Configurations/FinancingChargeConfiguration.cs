using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Portfolios;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class FinancingChargeConfiguration : IEntityTypeConfiguration<FinancingCharge>
{
    public void Configure(EntityTypeBuilder<FinancingCharge> b)
    {
        b.ToTable("financing_charges");
        b.HasKey(x => x.Id).HasName("pk_financing_charges");
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.PortfolioId).HasColumnName("portfolio_id");
        b.Property(x => x.PositionId).HasColumnName("position_id");
        b.Property(x => x.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
        b.Property(x => x.ChargedAt).HasColumnName("charged_at");
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        b.HasIndex(x => new { x.PositionId, x.ChargedAt }).IsUnique()
            .HasDatabaseName("ux_financing_charges_position_date");
        b.HasIndex(x => x.PortfolioId).HasDatabaseName("ix_financing_charges_portfolio_id");
        b.HasOne<Portfolio>().WithMany().HasForeignKey(x => x.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_financing_charges_portfolios_portfolio_id");
    }
}
