namespace PaperTrade.Infrastructure.Markets;

public sealed class TwelveDataOptions
{
    public const string SectionName = "MarketData:TwelveData";

    public string BaseUrl { get; init; } = "https://api.twelvedata.com/";
    public string ApiKey { get; init; } = string.Empty;
}
