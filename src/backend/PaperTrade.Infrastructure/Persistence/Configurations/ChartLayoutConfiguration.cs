using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Charts;
using PaperTrade.Domain.Users;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class ChartLayoutConfiguration : IEntityTypeConfiguration<ChartLayout>
{
    public void Configure(EntityTypeBuilder<ChartLayout> b)
    {
        b.ToTable("chart_layouts");
        b.HasKey(x => x.Id).HasName("pk_chart_layouts");
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
        b.Property(x => x.Timeframe).HasColumnName("timeframe").HasMaxLength(3).IsRequired();
        b.Property(x => x.StateJson).HasColumnName("state_json").HasMaxLength(32768).IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(x => new { x.UserId, x.UpdatedAt }).HasDatabaseName("ix_chart_layouts_user_updated");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_chart_layouts_users_user_id");
    }
}
