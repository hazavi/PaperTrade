using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class ProviderSymbolConfiguration : IEntityTypeConfiguration<ProviderSymbol>
{
    public void Configure(EntityTypeBuilder<ProviderSymbol> builder)
    {
        builder.ToTable("provider_symbols");
        builder.HasKey(mapping => new { mapping.InstrumentId, mapping.Provider }).HasName("pk_provider_symbols");
        builder.Property(mapping => mapping.InstrumentId).HasColumnName("instrument_id");
        builder.Property(mapping => mapping.Provider).HasColumnName("provider").HasMaxLength(40).IsRequired();
        builder.Property(mapping => mapping.Symbol).HasColumnName("symbol").HasMaxLength(80).IsRequired();
        builder.HasIndex(mapping => new { mapping.Provider, mapping.Symbol }).IsUnique().HasDatabaseName("ux_provider_symbols_provider_symbol");
        builder.HasOne(mapping => mapping.Instrument).WithMany().HasForeignKey(mapping => mapping.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_provider_symbols_instruments_instrument_id");
    }
}
