using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.TradingJournal;

namespace PaperTrade.Application.Portfolios;

public sealed record RiskLimitsDto(decimal? MaxDailyLossPercent, decimal? MaxPositionConcentrationPercent);
public sealed record SizeRequest(string Symbol, decimal EntryPrice, decimal StopPrice,
    decimal RiskPercent, decimal? TakeProfitPrice);
public sealed record SizeResult(decimal Quantity, decimal RiskBudgetUsd, decimal RiskPerUnitUsd,
    decimal EstimatedRiskUsd, decimal? RewardRiskRatio, decimal StopDistancePips,
    decimal TickValueUsd, decimal LotSize, IReadOnlyList<string> Warnings);
public sealed record SnapshotDto(DateTimeOffset RecordedAt, decimal Equity, decimal Cash,
    decimal RealizedPnl, decimal UnrealizedPnl, decimal DrawdownPercent);
public sealed record PeriodPnlDto(string Period, decimal Pnl);
public sealed record PerformanceDto(IReadOnlyList<SnapshotDto> History, decimal MaxDrawdownPercent,
    IReadOnlyList<PeriodPnlDto> Daily, IReadOnlyList<PeriodPnlDto> Weekly,
    IReadOnlyList<PeriodPnlDto> Monthly);
public sealed record JournalDto(Guid Id, Guid OrderId, string Note,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed class RiskAnalyticsService(IPortfolioRepository portfolios, ITradingRepository trading,
    IRiskAnalyticsRepository analytics, IUnitOfWork unitOfWork, IInstrumentCatalog instruments,
    IPortfolioService portfolioService)
{
    public async Task<RiskLimitsDto?> GetLimitsAsync(Guid userId, CancellationToken token)
    {
        var p = await portfolios.GetByUserIdAsync(userId, token);
        return p is null ? null : new(p.MaxDailyLossPercent, p.MaxPositionConcentrationPercent);
    }

    public async Task<RiskLimitsDto?> SetLimitsAsync(Guid userId, RiskLimitsDto limits, CancellationToken token)
    {
        var p = await portfolios.GetByUserIdAsync(userId, token);
        if (p is null) return null;
        p.SetRiskLimits(limits.MaxDailyLossPercent, limits.MaxPositionConcentrationPercent);
        await unitOfWork.SaveChangesAsync(token);
        return limits;
    }

    public async Task<SizeResult?> CalculateSizeAsync(Guid userId, SizeRequest request, CancellationToken token)
    {
        var p = await portfolioService.GetAsync(userId, token);
        if (p is null) return null;
        var instrument = await instruments.GetOrCreateAsync(request.Symbol.Trim().ToUpperInvariant(), token);
        if (!instrument.IsTradable || request.EntryPrice <= 0 || request.StopPrice <= 0 ||
            request.StopPrice >= request.EntryPrice || request.RiskPercent is <= 0 or > 100 ||
            request.TakeProfitPrice is <= 0 ||
            request.TakeProfitPrice is decimal targetPrice && targetPrice <= request.EntryPrice)
            throw new ArgumentException("A buy needs a stop below entry and a target above entry.");
        var difference = Math.Abs(request.EntryPrice - request.StopPrice);
        // FX quote P&L is translated at the stop price, the price at which risk is realized.
        var conversion = instrument.QuoteCurrency == "USD" ? 1m : request.StopPrice;
        var perUnit = difference / conversion;
        var budget = decimal.Round(p.PortfolioValue * request.RiskPercent / 100m, 2);
        // Divide only after multiplying by the conversion rate. A rounded repeating
        // per-unit value can otherwise drop an exact FX lot below its size step.
        var raw = budget * conversion / difference;
        var quantity = decimal.Floor(raw / instrument.MinimumOrderSize) * instrument.MinimumOrderSize;
        quantity = Math.Min(quantity, 1_000_000m);
        quantity = decimal.Round(quantity, instrument.QuantityPrecision);
        var warnings = new List<string>();
        if (quantity < instrument.MinimumOrderSize) warnings.Add("Risk budget is below the minimum order size.");
        if (p.Positions.Any(x => x.InstrumentId == instrument.Id))
            warnings.Add("You already hold this instrument.");
        if (instrument.BaseCurrency is not null && p.Positions.Any(x =>
            x.Instrument?.BaseCurrency == instrument.BaseCurrency ||
            x.Instrument?.QuoteCurrency == instrument.BaseCurrency ||
            instrument.AssetClass == PaperTrade.Domain.Instruments.AssetClass.Forex &&
            x.Instrument?.AssetClass == "forex" &&
            x.Instrument?.QuoteCurrency == instrument.QuoteCurrency))
            warnings.Add("Another position shares this currency exposure.");
        var notional = AccountCurrency.NotionalUsd(instrument, quantity, request.EntryPrice);
        var settings = await portfolios.GetByUserIdAsync(userId, token);
        var requiredMargin = notional / (settings?.LeverageFor(instrument.AssetClass) ?? 1);
        if (requiredMargin > p.CashBalance) warnings.Add("Calculated size exceeds available cash margin.");
        var reward = request.TakeProfitPrice is decimal target
            ? Math.Abs(target - request.EntryPrice) / difference : (decimal?)null;
        return new(quantity, budget, perUnit,
            decimal.Round(quantity * difference / conversion, 2), reward,
            difference / instrument.PipSize, instrument.TickSize / conversion,
            instrument.LotSize, warnings);
    }

    public async Task<PerformanceDto?> GetPerformanceAsync(Guid userId, CancellationToken token)
    {
        var p = await portfolioService.GetAsync(userId, token);
        if (p is null) return null;
        var snapshots = (await analytics.GetSnapshotsAsync(p.Id, token)).ToList();
        if (snapshots.Count == 0 || snapshots[^1].Equity != p.PortfolioValue)
            snapshots.Add(new EquitySnapshot(Guid.NewGuid(), p.Id, DateTimeOffset.UtcNow,
                p.PortfolioValue, p.CashBalance, p.RealizedPnl, p.UnrealizedPnl));
        var peak = 0m;
        var maxDrawdown = 0m;
        var history = snapshots.Select(x =>
        {
            peak = Math.Max(peak, x.Equity);
            var drawdown = peak == 0 ? 0 : decimal.Round((peak - x.Equity) / peak * 100, 2);
            maxDrawdown = Math.Max(maxDrawdown, drawdown);
            return new SnapshotDto(x.RecordedAt, x.Equity, x.Cash, x.RealizedPnl,
                x.UnrealizedPnl, drawdown);
        }).ToArray();
        return new(history, maxDrawdown,
            Summarize(history, x => x.ToString("yyyy-MM-dd"), TimeSpan.FromDays(1)),
            Summarize(history, x => $"{System.Globalization.ISOWeek.GetYear(x)}-W{System.Globalization.ISOWeek.GetWeekOfYear(x):00}", TimeSpan.FromDays(7)),
            Summarize(history, x => x.ToString("yyyy-MM"), TimeSpan.FromDays(31)));
    }

    private static IReadOnlyList<PeriodPnlDto> Summarize(IReadOnlyList<SnapshotDto> history,
        Func<DateTime, string> key, TimeSpan maximumGap)
    {
        var prior = history.Count > 0 ? history[0].Equity : 0m;
        var priorTime = history.Count > 0 ? history[0].RecordedAt : DateTimeOffset.MinValue;
        var result = new List<PeriodPnlDto>();
        foreach (var group in history.Skip(1).GroupBy(x => key(x.RecordedAt.UtcDateTime)))
        {
            var last = group.Last().Equity;
            var baseline = group.First().RecordedAt - priorTime > maximumGap
                ? group.First().Equity : prior;
            result.Add(new(group.Key, last - baseline));
            prior = last;
            priorTime = group.Last().RecordedAt;
        }
        return result;
    }

    public async Task<IReadOnlyList<JournalDto>?> GetJournalAsync(Guid userId, CancellationToken token)
    {
        var p = await portfolios.GetByUserIdAsync(userId, token);
        if (p is null) return null;
        return (await analytics.GetJournalAsync(p.Id, token)).Select(Map).ToArray();
    }

    public async Task<JournalDto?> SaveJournalAsync(Guid userId, Guid orderId, string note, CancellationToken token)
    {
        var p = await portfolios.GetByUserIdAsync(userId, token);
        var order = await trading.GetOrderAsync(orderId, token);
        if (p is null || order?.PortfolioId != p.Id) return null;
        var entry = await analytics.GetJournalEntryAsync(p.Id, orderId, token);
        if (entry is null)
        {
            entry = new JournalEntry(Guid.NewGuid(), p.Id, orderId, note, DateTimeOffset.UtcNow);
            analytics.AddJournalEntry(entry);
        }
        else entry.Update(note);
        await unitOfWork.SaveChangesAsync(token);
        return Map(entry);
    }

    public async Task<bool> DeleteJournalAsync(Guid userId, Guid orderId, CancellationToken token)
    {
        var p = await portfolios.GetByUserIdAsync(userId, token);
        if (p is null) return false;
        var entry = await analytics.GetJournalEntryAsync(p.Id, orderId, token);
        if (entry is null) return false;
        analytics.RemoveJournalEntry(entry);
        await unitOfWork.SaveChangesAsync(token);
        return true;
    }

    private static JournalDto Map(JournalEntry x) => new(x.Id, x.OrderId, x.Note, x.CreatedAt, x.UpdatedAt);
}
