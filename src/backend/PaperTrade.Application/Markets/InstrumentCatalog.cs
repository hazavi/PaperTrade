using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Assets;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Markets;

public sealed class InstrumentCatalog(
    IInstrumentRepository repository,
    IMarketDataService marketDataService) : IInstrumentCatalog
{
    public async Task<Instrument> GetOrCreateAsync(string symbol, CancellationToken cancellationToken)
    {
        return await repository.UpsertUsEquityAsync(symbol, null,
            AssetClass.Equity, "US", cancellationToken);
    }

    public async Task<InstrumentDto> GetAsync(string symbol, CancellationToken cancellationToken)
    {
        var instrument = await GetOrCreateAsync(symbol, cancellationToken);
        return InstrumentDto.From(instrument);
    }

    public async Task<IReadOnlyList<InstrumentDto>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var results = await marketDataService.SearchAssetsAsync(query, cancellationToken);
        var instruments = new List<InstrumentDto>();
        foreach (var result in results.DistinctBy(asset => asset.Symbol, StringComparer.OrdinalIgnoreCase))
        {
            var instrument = await repository.UpsertUsEquityAsync(result.Symbol, result.Name,
                result.Type == AssetType.ExchangeTradedFund ? AssetClass.Etf : AssetClass.Equity,
                result.Exchange, cancellationToken);
            instruments.Add(InstrumentDto.From(instrument));
        }
        return instruments;
    }

}
