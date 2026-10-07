using System.Security.Claims;
using FluentValidation;
using PaperTrade.Api.ErrorHandling;
using PaperTrade.Api.Extensions;
using PaperTrade.Application.Trading;

namespace PaperTrade.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization()
            .RequireRateLimiting("orders");

        group.MapGet("/", GetAsync);
        group.MapGet("/executions", GetExecutionsAsync);
        group.MapPost("/", CreateAsync);
        group.MapDelete("/{id:guid}", CancelAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        ClaimsPrincipal principal,
        ITradingService tradingService,
        CancellationToken cancellationToken)
    {
        return principal.TryGetUserId(out var userId)
            ? Results.Ok(await tradingService.GetOrdersAsync(userId, cancellationToken))
            : Results.Unauthorized();
    }

    private static async Task<IResult> GetExecutionsAsync(ClaimsPrincipal principal,
        ITradingService tradingService, CancellationToken cancellationToken) =>
        principal.TryGetUserId(out var userId)
            ? Results.Ok(await tradingService.GetExecutionsAsync(userId, cancellationToken))
            : Results.Unauthorized();

    private static async Task<IResult> CancelAsync(Guid id, ClaimsPrincipal principal,
        ITradingService tradingService, CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();
        return await tradingService.CancelOrderAsync(userId, id, cancellationToken)
            ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> CreateAsync(
        CreateOrderRequest request,
        IValidator<CreateOrderRequest> validator,
        ClaimsPrincipal principal,
        ITradingService tradingService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToErrorDictionary(),
                type: ApiProblemTypes.Validation,
                title: "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await tradingService.PlaceOrderAsync(
            userId, request, cancellationToken);

        if (result.Status is OrderExecutionStatus.Filled or OrderExecutionStatus.Pending)
        {
            loggerFactory.CreateLogger("Trading")
                .LogInformation(
                    "Order {OrderId} {Status} for user {UserId}, portfolio {PortfolioId}, {Side} {Quantity} {Symbol} at {ExecutedPrice}",
                    result.Order!.Id, result.Status, userId, result.Order.PortfolioId,
                    result.Order.Side, result.Order.Quantity,
                    result.Order.Symbol, result.Order.ExecutedPrice);

            return Results.Created($"/api/orders/{result.Order.Id}", result);
        }

        return result.Status switch
        {
            OrderExecutionStatus.InsufficientFunds => Problem(
                ApiProblemTypes.InsufficientFunds,
                "Insufficient available cash for this order.",
                StatusCodes.Status409Conflict,
                new Dictionary<string, object?> { ["availableCash"] = result.CashBalance }),
            OrderExecutionStatus.InsufficientQuantity => Problem(
                ApiProblemTypes.InsufficientQuantity,
                "The order quantity exceeds the owned quantity.",
                StatusCodes.Status409Conflict,
                new Dictionary<string, object?> { ["ownedQuantity"] = result.OwnedQuantity }),
            OrderExecutionStatus.RiskLimitExceeded => Problem(
                ApiProblemTypes.Validation, "Portfolio risk limit prevents this buy.",
                StatusCodes.Status409Conflict),
            OrderExecutionStatus.QuoteNotFound => Problem(
                ApiProblemTypes.NotFound,
                "A current quote for this symbol was not found.",
                StatusCodes.Status404NotFound),
            OrderExecutionStatus.StaleQuote => Problem(
                ApiProblemTypes.MarketDataUnavailable,
                "The quote is too old for an order. Refresh market data and try again.",
                StatusCodes.Status409Conflict),
            OrderExecutionStatus.PortfolioNotFound => Problem(
                ApiProblemTypes.NotFound,
                "The portfolio was not found.",
                StatusCodes.Status404NotFound),
            _ => Problem(ApiProblemTypes.Validation,
                "The order is invalid.", StatusCodes.Status400BadRequest)
        };
    }

    private static IResult Problem(
        string type,
        string title,
        int status,
        Dictionary<string, object?>? extensions = null)
    {
        return Results.Problem(type: type, title: title,
            statusCode: status, extensions: extensions);
    }
}
