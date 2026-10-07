namespace PaperTrade.Application.Trading;

public sealed class TradingSimulationOptions
{
    public const string SectionName = "Trading";
    public decimal FeeBps { get; set; } = 1m;
    public decimal SlippageBps { get; set; }
}
