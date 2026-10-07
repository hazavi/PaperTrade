using PaperTrade.Application.Abstractions.Persistence;

namespace PaperTrade.Application.Portfolios;

public sealed record MarginSettingsDto(bool Enabled, int EquityLeverage, int ForexLeverage,
    int MetalLeverage, int CommodityLeverage, int IndexLeverage, int CryptoLeverage);
public sealed record FinancingChargeDto(Guid Id, string Symbol, DateTimeOffset ChargedAt, decimal Amount);

public sealed class MarginService(IPortfolioRepository portfolios, ITradingRepository trading,
    IUnitOfWork unitOfWork)
{
    public async Task<MarginSettingsDto?> GetAsync(Guid userId, CancellationToken token)
    {
        var p = await portfolios.GetByUserIdAsync(userId, token);
        return p is null ? null : Map(p);
    }

    public async Task<MarginSettingsDto?> SetAsync(Guid userId, MarginSettingsDto settings, CancellationToken token)
    {
        var p = await portfolios.GetByUserIdAsync(userId, token);
        if (p is null) return null;
        p.SetMargin(settings.Enabled, settings.EquityLeverage, settings.ForexLeverage,
            settings.MetalLeverage, settings.CommodityLeverage, settings.IndexLeverage,
            settings.CryptoLeverage);
        await unitOfWork.SaveChangesAsync(token);
        return Map(p);
    }

    public async Task<IReadOnlyList<FinancingChargeDto>?> ChargesAsync(Guid userId, CancellationToken token)
    {
        var p = await portfolios.GetByUserIdAsync(userId, token);
        if (p is null) return null;
        return (await trading.GetFinancingChargesAsync(p.Id, token))
            .Select(x => new FinancingChargeDto(x.Id, x.Symbol, x.ChargedAt, x.Amount)).ToArray();
    }

    private static MarginSettingsDto Map(PaperTrade.Domain.Portfolios.Portfolio p) =>
        new(p.MarginEnabled, p.EquityLeverage, p.ForexLeverage, p.MetalLeverage,
            p.CommodityLeverage, p.IndexLeverage, p.CryptoLeverage);
}
