using Microsoft.EntityFrameworkCore;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class InstrumentRepository(PaperTradeDbContext dbContext) : IInstrumentRepository
{
    public Task<Instrument?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken) =>
        dbContext.Instruments.AsNoTracking().SingleOrDefaultAsync(
            instrument => instrument.Symbol == symbol.Trim().ToUpperInvariant(), cancellationToken);

    public Task<string?> GetProviderSymbolAsync(string symbol, string provider, CancellationToken cancellationToken) =>
        dbContext.ProviderSymbols
            .Where(mapping => mapping.Instrument.Symbol == symbol.Trim().ToUpperInvariant() &&
                              mapping.Provider == provider)
            .Select(mapping => mapping.Symbol)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Instrument> UpsertUsEquityAsync(string symbol, string? displayName,
        AssetClass assetClass, string exchange, CancellationToken cancellationToken)
    {
        var candidate = Instrument.UsEquity(symbol, displayName, assetClass, exchange);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO instruments (id, symbol, display_name, asset_class, exchange,
                base_currency, quote_currency, price_precision, quantity_precision,
                tick_size, minimum_order_size, market_time_zone, trading_session, is_tradable)
            VALUES ({candidate.Id}, {candidate.Symbol}, {candidate.DisplayName},
                {candidate.AssetClass.ToString()}, {candidate.Exchange}, NULL, 'USD',
                2, 6, 0.01, 0.000001, 'America/New_York', 'US equities', TRUE)
            ON CONFLICT (symbol) DO NOTHING
            """, cancellationToken);

        if (candidate.DisplayName != candidate.Symbol)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE instruments SET display_name = {candidate.DisplayName},
                    exchange = {candidate.Exchange}, asset_class = {candidate.AssetClass.ToString()}
                WHERE symbol = {candidate.Symbol} AND lower(display_name) = lower(symbol)
                """, cancellationToken);
        }

        var instrument = await GetBySymbolAsync(candidate.Symbol, cancellationToken)
            ?? throw new InvalidOperationException("Instrument upsert did not return a row.");

        foreach (var provider in new[] { "finnhub", "twelvedata" })
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO provider_symbols (instrument_id, provider, symbol)
                VALUES ({instrument.Id}, {provider}, {instrument.Symbol})
                ON CONFLICT DO NOTHING
                """, cancellationToken);
        }

        return instrument;
    }

    public async Task<Instrument> UpsertPairAsync(Instrument candidate, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO instruments (id, symbol, display_name, asset_class, exchange,
                base_currency, quote_currency, price_precision, quantity_precision,
                tick_size, minimum_order_size, market_time_zone, trading_session, is_tradable)
            VALUES ({candidate.Id}, {candidate.Symbol}, {candidate.DisplayName},
                {candidate.AssetClass.ToString()}, {candidate.Exchange}, {candidate.BaseCurrency},
                {candidate.QuoteCurrency}, {candidate.PricePrecision}, {candidate.QuantityPrecision},
                {candidate.TickSize}, {candidate.MinimumOrderSize}, {candidate.MarketTimeZone},
                {candidate.TradingSession}, {candidate.IsTradable})
            ON CONFLICT (symbol) DO UPDATE SET
                display_name = EXCLUDED.display_name, asset_class = EXCLUDED.asset_class,
                exchange = EXCLUDED.exchange, base_currency = EXCLUDED.base_currency,
                quote_currency = EXCLUDED.quote_currency, price_precision = EXCLUDED.price_precision,
                quantity_precision = EXCLUDED.quantity_precision, tick_size = EXCLUDED.tick_size,
                minimum_order_size = EXCLUDED.minimum_order_size,
                market_time_zone = EXCLUDED.market_time_zone,
                trading_session = EXCLUDED.trading_session, is_tradable = EXCLUDED.is_tradable
            """, cancellationToken);

        var instrument = await GetBySymbolAsync(candidate.Symbol, cancellationToken)
            ?? throw new InvalidOperationException("Instrument upsert did not return a row.");
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO provider_symbols (instrument_id, provider, symbol)
            VALUES ({instrument.Id}, 'twelvedata', {instrument.Symbol})
            ON CONFLICT (instrument_id, provider) DO UPDATE SET symbol = EXCLUDED.symbol
            """, cancellationToken);
        return instrument;
    }
}
