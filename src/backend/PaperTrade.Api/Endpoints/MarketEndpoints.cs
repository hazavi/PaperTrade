using PaperTrade.Api.ErrorHandling;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Api.Endpoints;

public static class MarketEndpoints
{
    private static readonly HashSet<string> SupportedTimeframes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "1D",
            "1W",
            "1M",
            "3M",
            "1Y"
        };

    public static IEndpointRouteBuilder MapMarketEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/markets")
            .WithTags("Markets")
            .RequireAuthorization();

        group.MapGet("/search", SearchAsync);
        group.MapGet("/pair/{baseCurrency}/{quoteCurrency}/instrument",
            (string baseCurrency, string quoteCurrency, IInstrumentCatalog catalog, CancellationToken token) =>
                GetInstrumentAsync($"{baseCurrency}/{quoteCurrency}", catalog, token));
        group.MapGet("/pair/{baseCurrency}/{quoteCurrency}/quote",
            (string baseCurrency, string quoteCurrency, IMarketDataService service, CancellationToken token) =>
                GetQuoteAsync($"{baseCurrency}/{quoteCurrency}", service, token));
        group.MapGet("/pair/{baseCurrency}/{quoteCurrency}/history",
            (string baseCurrency, string quoteCurrency, string? timeframe, IMarketDataService service, CancellationToken token) =>
                GetHistoryAsync($"{baseCurrency}/{quoteCurrency}", timeframe, service, token));
        group.MapGet("/{symbol}/instrument", GetInstrumentAsync);
        group.MapGet("/{symbol}/quote", GetQuoteAsync);
        group.MapGet("/{symbol}/history", GetHistoryAsync);
        group.MapGet("/status", GetStatusAsync);

        return endpoints;
    }

    private static async Task<IResult> SearchAsync(
        string? q,
        IInstrumentCatalog instrumentCatalog,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return ValidationProblem(
                "q",
                "Search query must contain at least 2 characters.");
        }

        var results = await instrumentCatalog.SearchAsync(
            q,
            cancellationToken);

        return Results.Ok(results);
    }

    private static async Task<IResult> GetInstrumentAsync(
        string symbol, IInstrumentCatalog instrumentCatalog,
        CancellationToken cancellationToken)
    {
        if (!IsValidSymbol(symbol)) return ValidationProblem("symbol", "Enter a valid symbol.");
        return Results.Ok(await instrumentCatalog.GetAsync(symbol, cancellationToken));
    }

    private static async Task<IResult> GetQuoteAsync(
        string symbol,
        IMarketDataService marketDataService,
        CancellationToken cancellationToken)
    {
        if (!IsValidSymbol(symbol))
        {
            return ValidationProblem(
                "symbol",
                "Enter a valid symbol.");
        }

        var quote = await marketDataService.GetQuoteAsync(
            symbol,
            cancellationToken);

        return quote is null
            ? Results.Problem(
                type: ApiProblemTypes.NotFound,
                title: "The requested symbol was not found.",
                statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(quote);
    }

    private static async Task<IResult> GetHistoryAsync(
        string symbol,
        string? timeframe,
        IMarketDataService marketDataService,
        CancellationToken cancellationToken)
    {
        if (!IsValidSymbol(symbol))
        {
            return ValidationProblem(
                "symbol",
                "Enter a valid symbol.");
        }

        var selectedTimeframe = timeframe ?? "1M";

        if (!SupportedTimeframes.Contains(selectedTimeframe))
        {
            return ValidationProblem(
                "timeframe",
                "Timeframe must be 1D, 1W, 1M, 3M, or 1Y.");
        }

        var (from, to, resolution) = GetHistoryRange(
            selectedTimeframe);

        var prices =
            await marketDataService.GetHistoricalPricesAsync(
                symbol,
                from,
                to,
                resolution,
                cancellationToken);

        return Results.Ok(prices);
    }

    private static async Task<IResult> GetStatusAsync(
        string? exchange,
        IMarketDataService marketDataService,
        CancellationToken cancellationToken)
    {
        var status = await marketDataService.GetMarketStatusAsync(
            string.IsNullOrWhiteSpace(exchange) ? "US" : exchange,
            cancellationToken);

        return Results.Ok(status);
    }

    private static IResult ValidationProblem(
        string field,
        string message)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                [field] = [message]
            },
            type: ApiProblemTypes.Validation,
            title: "Validation failed.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static bool IsValidSymbol(string symbol)
    {
        return !string.IsNullOrWhiteSpace(symbol) &&
               symbol.Length <= 32 &&
               (!symbol.Contains('/') || SupportedPairs.Create(symbol) is not null) &&
               symbol.All(character =>
                   char.IsLetterOrDigit(character) ||
                   character is '.' or '-' or '/');
    }

    private static (DateTimeOffset From, DateTimeOffset To,
        string Resolution) GetHistoryRange(string timeframe)
    {
        var currentSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var bucketedSeconds = currentSeconds - currentSeconds % 300;
        var to = DateTimeOffset.FromUnixTimeSeconds(bucketedSeconds);

        return timeframe.ToUpperInvariant() switch
        {
            "1D" => (to.AddDays(-1), to, "5"),
            "1W" => (to.AddDays(-7), to, "30"),
            "1M" => (to.AddMonths(-1), to, "60"),
            "3M" => (to.AddMonths(-3), to, "D"),
            "1Y" => (to.AddYears(-1), to, "D"),
            _ => throw new ArgumentOutOfRangeException(nameof(timeframe))
        };
    }
}
