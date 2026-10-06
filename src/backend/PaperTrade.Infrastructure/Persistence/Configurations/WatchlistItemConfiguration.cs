using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Watchlists;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class WatchlistItemConfiguration
    : IEntityTypeConfiguration<WatchlistItem>
{
    public void Configure(EntityTypeBuilder<WatchlistItem> builder)
    {
        builder.ToTable("watchlist_items");

        builder.HasKey(item => item.Id)
            .HasName("pk_watchlist_items");

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(item => item.WatchlistId)
            .HasColumnName("watchlist_id")
            .IsRequired();

        builder.Property(item => item.InstrumentId).HasColumnName("instrument_id").IsRequired();

        builder.Property(item => item.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.AddedAt)
            .HasColumnName("added_at")
            .IsRequired();

        builder.HasIndex(item => new
            {
                item.WatchlistId,
                item.InstrumentId
            })
            .IsUnique()
            .HasDatabaseName(
                "ux_watchlist_items_watchlist_id_instrument_id");
        builder.HasOne(item => item.Instrument).WithMany().HasForeignKey(item => item.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_watchlist_items_instruments_instrument_id");
    }
}
