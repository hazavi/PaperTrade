using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Infrastructure.Persistence.Configurations;

internal sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("instruments", table =>
        {
            table.HasCheckConstraint("ck_instruments_tick_size_positive", "tick_size > 0");
            table.HasCheckConstraint("ck_instruments_minimum_order_size_positive", "minimum_order_size > 0");
        });
        builder.HasKey(instrument => instrument.Id).HasName("pk_instruments");
        builder.Property(instrument => instrument.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(instrument => instrument.Symbol).HasColumnName("symbol").HasMaxLength(32).IsRequired();
        builder.Property(instrument => instrument.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(instrument => instrument.AssetClass).HasColumnName("asset_class").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(instrument => instrument.Exchange).HasColumnName("exchange").HasMaxLength(40).IsRequired();
        builder.Property(instrument => instrument.BaseCurrency).HasColumnName("base_currency").HasMaxLength(3);
        builder.Property(instrument => instrument.QuoteCurrency).HasColumnName("quote_currency").HasMaxLength(3).IsRequired();
        builder.Property(instrument => instrument.PricePrecision).HasColumnName("price_precision").IsRequired();
        builder.Property(instrument => instrument.QuantityPrecision).HasColumnName("quantity_precision").IsRequired();
        builder.Property(instrument => instrument.TickSize).HasColumnName("tick_size").HasPrecision(18, 10).IsRequired();
        builder.Property(instrument => instrument.MinimumOrderSize).HasColumnName("minimum_order_size").HasPrecision(18, 10).IsRequired();
        builder.Property(instrument => instrument.MarketTimeZone).HasColumnName("market_time_zone").HasMaxLength(80).IsRequired();
        builder.Property(instrument => instrument.TradingSession).HasColumnName("trading_session").HasMaxLength(80).IsRequired();
        builder.Property(instrument => instrument.IsTradable).HasColumnName("is_tradable").IsRequired();
        builder.HasIndex(instrument => instrument.Symbol).IsUnique().HasDatabaseName("ux_instruments_symbol");
    }
}
