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
        var pair = SupportedPairs.Create(symbol);
        if (pair is not null) return await repository.UpsertPairAsync(pair, cancellationToken);
        if (symbol.Contains('/')) throw new ArgumentException("Unsupported market pair.", nameof(symbol));
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
        var matchedPairs = SupportedPairs.Symbols.Where(symbol => symbol.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) ||
            (symbol.StartsWith("XAU") && "gold".Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)) ||
            (symbol.StartsWith("XAG") && "silver".Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var pairResults = new List<InstrumentDto>();
        foreach (var symbol in matchedPairs)
            pairResults.Add(InstrumentDto.From(await repository.UpsertPairAsync(SupportedPairs.Create(symbol)!, cancellationToken)));

        if (pairResults.Count > 0) return pairResults;

        var results = await marketDataService.SearchAssetsAsync(query, cancellationToken);
        var instruments = pairResults;
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
