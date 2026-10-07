namespace PaperTrade.Application.Markets;

public sealed class ExpandedMarketAccess
{
    public bool Enabled { get; set; }
    public bool DisplayRightsConfirmed { get; set; }
    public bool Available => Enabled && DisplayRightsConfirmed;
}

public sealed class ExpandedMarketAccessException() : Exception(
    "Expanded market data requires an enabled feed and confirmed display rights.");
