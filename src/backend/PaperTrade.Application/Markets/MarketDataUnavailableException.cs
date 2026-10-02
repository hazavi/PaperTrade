namespace PaperTrade.Application.Markets;

public sealed class MarketDataUnavailableException(
    string message,
    Exception? innerException = null)
    : Exception(message, innerException);
