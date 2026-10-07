using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Portfolios;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class PortfolioConfiguration
    : IEntityTypeConfiguration<Portfolio>
{
    public void Configure(EntityTypeBuilder<Portfolio> builder)
    {
        builder.ToTable(
            "portfolios",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_portfolios_cash_balance_nonnegative",
                    "cash_balance >= 0");

                table.HasCheckConstraint(
                    "ck_portfolios_initial_balance_nonnegative",
                    "initial_balance >= 0");
                table.HasCheckConstraint("ck_portfolios_max_daily_loss_percent",
                    "max_daily_loss_percent IS NULL OR (max_daily_loss_percent > 0 AND max_daily_loss_percent <= 100)");
                table.HasCheckConstraint("ck_portfolios_max_concentration_percent",
                    "max_position_concentration_percent IS NULL OR (max_position_concentration_percent > 0 AND max_position_concentration_percent <= 100)");
            });

        builder.HasKey(portfolio => portfolio.Id)
            .HasName("pk_portfolios");

        builder.Property(portfolio => portfolio.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(portfolio => portfolio.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(portfolio => portfolio.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(portfolio => portfolio.CashBalance)
            .HasColumnName("cash_balance")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(portfolio => portfolio.InitialBalance)
            .HasColumnName("initial_balance")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(portfolio => portfolio.RealizedPnl)
            .HasColumnName("realized_pnl")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(portfolio => portfolio.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(portfolio => portfolio.MaxDailyLossPercent)
            .HasColumnName("max_daily_loss_percent").HasPrecision(5, 2);
        builder.Property(portfolio => portfolio.MaxPositionConcentrationPercent)
            .HasColumnName("max_position_concentration_percent").HasPrecision(5, 2);

        builder.HasIndex(portfolio => portfolio.UserId)
            .IsUnique()
            .HasDatabaseName("ux_portfolios_user_id");
    }
}
