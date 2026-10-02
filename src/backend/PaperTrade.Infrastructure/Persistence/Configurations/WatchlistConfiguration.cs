using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Watchlists;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class WatchlistConfiguration
    : IEntityTypeConfiguration<Watchlist>
{
    public void Configure(EntityTypeBuilder<Watchlist> builder)
    {
        builder.ToTable("watchlists");

        builder.HasKey(watchlist => watchlist.Id)
            .HasName("pk_watchlists");

        builder.Property(watchlist => watchlist.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(watchlist => watchlist.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(watchlist => watchlist.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(watchlist => watchlist.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(watchlist => new
            {
                watchlist.UserId,
                watchlist.Name
            })
            .IsUnique()
            .HasDatabaseName("ux_watchlists_user_id_name");

        builder.HasOne(watchlist => watchlist.User)
            .WithMany(user => user.Watchlists)
            .HasForeignKey(watchlist => watchlist.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_watchlists_users_user_id");

        builder.HasMany(watchlist => watchlist.Items)
            .WithOne(item => item.Watchlist)
            .HasForeignKey(item => item.WatchlistId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName(
                "fk_watchlist_items_watchlists_watchlist_id");

        builder.Navigation(watchlist => watchlist.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
