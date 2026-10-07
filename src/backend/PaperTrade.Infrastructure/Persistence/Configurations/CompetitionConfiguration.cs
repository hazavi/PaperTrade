using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Competitions;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> b)
    {
        b.ToTable("competitions"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.OwnerId).HasColumnName("owner_id"); b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
        b.Property(x => x.JoinCode).HasColumnName("join_code").HasMaxLength(12); b.HasIndex(x => x.JoinCode).IsUnique();
        b.Property(x => x.StartsAt).HasColumnName("starts_at"); b.Property(x => x.EndsAt).HasColumnName("ends_at");
        b.HasOne<PaperTrade.Domain.Users.User>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CompetitionMemberConfiguration : IEntityTypeConfiguration<CompetitionMember>
{
    public void Configure(EntityTypeBuilder<CompetitionMember> b)
    {
        b.ToTable("competition_members"); b.HasKey(x => new { x.CompetitionId, x.UserId });
        b.Property(x => x.CompetitionId).HasColumnName("competition_id"); b.Property(x => x.UserId).HasColumnName("user_id");
        b.Property(x => x.JoinedAt).HasColumnName("joined_at"); b.Property(x => x.StartingEquity).HasColumnName("starting_equity").HasPrecision(18, 2);
        b.Property(x => x.EndingEquity).HasColumnName("ending_equity").HasPrecision(18, 2);
        b.HasOne<Competition>().WithMany().HasForeignKey(x => x.CompetitionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<PaperTrade.Domain.Users.User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
