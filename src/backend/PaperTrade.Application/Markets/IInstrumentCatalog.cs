using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Markets;

public interface IInstrumentCatalog
{
    Task<Instrument> GetOrCreateAsync(string symbol, CancellationToken cancellationToken);
    Task<InstrumentDto> GetAsync(string symbol, CancellationToken cancellationToken);
    Task<IReadOnlyList<InstrumentDto>> SearchAsync(string query, CancellationToken cancellationToken);
}
