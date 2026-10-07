namespace PaperTrade.Application.Trading;

public sealed class TradingSimulationOptions
{
    public const string SectionName = "Trading";
    public decimal FeeBps { get; set; } = 1m;
    public decimal SlippageBps { get; set; }
    public decimal FinancingAprPercent { get; set; } = 5m;
    public decimal MaintenanceMarginPercent { get; set; } = 50m;
    public int MaxQuoteAgeMinutes { get; set; } = 15;
}
