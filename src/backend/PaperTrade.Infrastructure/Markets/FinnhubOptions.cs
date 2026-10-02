namespace PaperTrade.Infrastructure.Markets;

public sealed class FinnhubOptions
{
    public const string SectionName = "MarketData:Finnhub";

    public string BaseUrl { get; init; } =
        "https://finnhub.io/api/v1/";

    public string ApiKey { get; init; } = string.Empty;
}
