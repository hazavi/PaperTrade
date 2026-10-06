using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Abstractions.Persistence;

public interface IInstrumentRepository
{
    Task<Instrument?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken);
    Task<string?> GetProviderSymbolAsync(string symbol, string provider, CancellationToken cancellationToken);
    Task<Instrument> UpsertUsEquityAsync(string symbol, string? displayName,
        AssetClass assetClass, string exchange, CancellationToken cancellationToken);
    Task<Instrument> UpsertPairAsync(Instrument candidate, CancellationToken cancellationToken);
}
