using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Notifications;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(notification => notification.Id).HasName("pk_notifications");
        builder.Property(notification => notification.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(notification => notification.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(notification => notification.Title).HasColumnName("title").HasMaxLength(150).IsRequired();
        builder.Property(notification => notification.Message).HasColumnName("message").HasMaxLength(500).IsRequired();
        builder.Property(notification => notification.IsRead).HasColumnName("is_read").IsRequired();
        builder.Property(notification => notification.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(notification => new { notification.UserId, notification.CreatedAt }).HasDatabaseName("ix_notifications_user_id_created_at");
        builder.HasOne(notification => notification.User).WithMany().HasForeignKey(notification => notification.UserId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_notifications_users_user_id");
    }
}
