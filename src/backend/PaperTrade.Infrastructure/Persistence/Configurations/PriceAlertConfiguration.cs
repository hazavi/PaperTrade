using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Alerts;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> builder)
    {
        builder.ToTable("price_alerts");
        builder.HasKey(alert => alert.Id).HasName("pk_price_alerts");
        builder.Property(alert => alert.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(alert => alert.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(alert => alert.InstrumentId).HasColumnName("instrument_id").IsRequired();
        builder.Property(alert => alert.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
        builder.Property(alert => alert.Direction).HasColumnName("direction").HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(alert => alert.Metric).HasColumnName("metric").HasMaxLength(24).HasDefaultValue("price").IsRequired();
        builder.Property(alert => alert.TargetPrice).HasColumnName("target_price").HasPrecision(18, 6).IsRequired();
        builder.Property(alert => alert.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(alert => alert.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(alert => alert.TriggeredAt).HasColumnName("triggered_at");
        builder.HasIndex(alert => new { alert.IsActive, alert.Symbol }).HasDatabaseName("ix_price_alerts_active_symbol");
        builder.HasIndex(alert => alert.UserId).HasDatabaseName("ix_price_alerts_user_id");
        builder.HasOne(alert => alert.User).WithMany().HasForeignKey(alert => alert.UserId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_price_alerts_users_user_id");
        builder.HasOne(alert => alert.Instrument).WithMany().HasForeignKey(alert => alert.InstrumentId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_price_alerts_instruments_instrument_id");
    }
}
