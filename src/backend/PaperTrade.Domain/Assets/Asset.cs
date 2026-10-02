namespace PaperTrade.Domain.Assets;

public sealed class Asset
{
    public Asset(
        Guid id,
        string symbol,
        string name,
        string exchange,
        AssetType type,
        string currency,
        bool isActive)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Asset ID cannot be empty.",
                nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        Id = id;
        Symbol = symbol.Trim().ToUpperInvariant();
        Name = name.Trim();
        Exchange = exchange.Trim();
        Type = type;
        Currency = currency.Trim().ToUpperInvariant();
        IsActive = isActive;
    }

    public Guid Id { get; }

    public string Symbol { get; }

    public string Name { get; }

    public string Exchange { get; }

    public AssetType Type { get; }

    public string Currency { get; }

    public bool IsActive { get; }
}
