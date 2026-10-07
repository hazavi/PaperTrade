using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.TradingJournal;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries");
        builder.HasKey(x => x.Id).HasName("pk_journal_entries");
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.PortfolioId).HasColumnName("portfolio_id");
        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("ux_journal_entries_order_id");
        builder.HasIndex(x => x.PortfolioId).HasDatabaseName("ix_journal_entries_portfolio_id");
        builder.HasOne<PaperTrade.Domain.Orders.Order>().WithMany().HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_journal_entries_orders_order_id");
        builder.HasOne<PaperTrade.Domain.Portfolios.Portfolio>().WithMany().HasForeignKey(x => x.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_journal_entries_portfolios_portfolio_id");
    }
}
