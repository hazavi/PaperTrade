using PaperTrade.Api.ErrorHandling;
using PaperTrade.Application.Markets;

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
        group.MapGet("/{symbol}/quote", GetQuoteAsync);
        group.MapGet("/{symbol}/history", GetHistoryAsync);
        group.MapGet("/status", GetStatusAsync);

        return endpoints;
    }

    private static async Task<IResult> SearchAsync(
        string? q,
        IMarketDataService marketDataService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return ValidationProblem(
                "q",
                "Search query must contain at least 2 characters.");
        }

        var results = await marketDataService.SearchAssetsAsync(
            q,
            cancellationToken);

        return Results.Ok(results);
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

        return prices.Count == 0
            ? Results.Problem(
                type: ApiProblemTypes.NotFound,
                title: "No historical prices were found.",
                statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(prices);
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
               symbol.All(character =>
                   char.IsLetterOrDigit(character) ||
                   character is '.' or '-');
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
