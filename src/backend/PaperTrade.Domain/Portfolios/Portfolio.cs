using PaperTrade.Domain.Users;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Domain.Portfolios;

public sealed class Portfolio
{
    private Portfolio()
    {
    }

    public Portfolio(
        Guid id,
        Guid userId,
        string name,
        decimal cashBalance,
        decimal initialBalance,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Portfolio ID cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (cashBalance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cashBalance),
                "Cash balance cannot be negative.");
        }

        if (initialBalance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialBalance),
                "Initial balance cannot be negative.");
        }

        Id = id;
        UserId = userId;
        Name = name;
        CashBalance = cashBalance;
        InitialBalance = initialBalance;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal CashBalance { get; private set; }

    public decimal InitialBalance { get; private set; }

    public decimal RealizedPnl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public decimal? MaxDailyLossPercent { get; private set; }
    public decimal? MaxPositionConcentrationPercent { get; private set; }
    public bool MarginEnabled { get; private set; }
    public int EquityLeverage { get; private set; } = 1;
    public int ForexLeverage { get; private set; } = 1;
    public int MetalLeverage { get; private set; } = 1;
    public int CommodityLeverage { get; private set; } = 1;
    public int IndexLeverage { get; private set; } = 1;
    public int CryptoLeverage { get; private set; } = 1;

    public User User { get; private set; } = null!;

    public void Debit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (amount > CashBalance)
        {
            throw new InvalidOperationException("Insufficient cash balance.");
        }

        CashBalance -= amount;
    }

    public void Credit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        CashBalance += amount;
    }

    public void RecordRealizedPnl(decimal amount)
    {
        RealizedPnl += amount;
    }

    public void SetMargin(bool enabled, int equity, int forex, int metal,
        int commodity, int index, int crypto)
    {
        if (equity is < 1 or > 5 || forex is < 1 or > 10 || metal is < 1 or > 5 ||
            commodity is < 1 or > 5 || index is < 1 or > 5 || crypto is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(equity), "Leverage exceeds the simulation cap.");
        MarginEnabled = enabled;
        EquityLeverage = equity;
        ForexLeverage = forex;
        MetalLeverage = metal;
        CommodityLeverage = commodity;
        IndexLeverage = index;
        CryptoLeverage = crypto;
    }

    public int LeverageFor(AssetClass assetClass) => !MarginEnabled ? 1 : assetClass switch
    {
        AssetClass.Forex => ForexLeverage,
        AssetClass.Metal => MetalLeverage,
        AssetClass.Commodity => CommodityLeverage,
        AssetClass.Index => IndexLeverage,
        AssetClass.Crypto => CryptoLeverage,
        _ => EquityLeverage
    };

    public void Settle(decimal amount)
    {
        CashBalance += amount;
    }

    public void SetRiskLimits(decimal? dailyLossPercent, decimal? concentrationPercent)
    {
        if (dailyLossPercent is <= 0 or > 100 || concentrationPercent is <= 0 or > 100 ||
            dailyLossPercent is decimal daily && decimal.Round(daily, 2) != daily ||
            concentrationPercent is decimal concentration && decimal.Round(concentration, 2) != concentration)
            throw new ArgumentOutOfRangeException(nameof(dailyLossPercent), "Limits must be from 0.01 to 100 percent in hundredths.");
        MaxDailyLossPercent = dailyLossPercent;
        MaxPositionConcentrationPercent = concentrationPercent;
    }
}
