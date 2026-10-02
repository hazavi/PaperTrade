using System.Security.Claims;
using FluentValidation;
using PaperTrade.Api.ErrorHandling;
using PaperTrade.Api.Extensions;
using PaperTrade.Application.Watchlists;

namespace PaperTrade.Api.Endpoints;

public static class WatchlistEndpoints
{
    public static IEndpointRouteBuilder MapWatchlistEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/watchlists")
            .WithTags("Watchlists")
            .RequireAuthorization();

        group.MapGet("/", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPost("/{id:guid}/assets", AddItemAsync);
        group.MapDelete(
            "/{id:guid}/assets/{symbol}",
            RemoveItemAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        ClaimsPrincipal principal,
        IWatchlistService watchlistService,
        CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(
            await watchlistService.GetAsync(
                userId,
                cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        CreateWatchlistRequest request,
        IValidator<CreateWatchlistRequest> validator,
        ClaimsPrincipal principal,
        IWatchlistService watchlistService,
        CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return Results.Unauthorized();
        }

        var validation = await validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validation.IsValid)
        {
            return Results.ValidationProblem(
                validation.ToErrorDictionary(),
                type: ApiProblemTypes.Validation,
                title: "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await watchlistService.CreateAsync(
            userId,
            request,
            cancellationToken);

        return result.Status == WatchlistChangeStatus.Conflict
            ? Conflict("A watchlist with this name already exists.")
            : Results.Created(
                $"/api/watchlists/{result.Watchlist!.Id}",
                result.Watchlist);
    }

    private static async Task<IResult> AddItemAsync(
        Guid id,
        AddWatchlistItemRequest request,
        IValidator<AddWatchlistItemRequest> validator,
        ClaimsPrincipal principal,
        IWatchlistService watchlistService,
        CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return Results.Unauthorized();
        }

        var validation = await validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validation.IsValid)
        {
            return Results.ValidationProblem(
                validation.ToErrorDictionary(),
                type: ApiProblemTypes.Validation,
                title: "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await watchlistService.AddItemAsync(
            userId,
            id,
            request,
            cancellationToken);

        return result.Status switch
        {
            WatchlistChangeStatus.Success =>
                Results.Ok(result.Watchlist),
            WatchlistChangeStatus.Conflict =>
                Conflict("This symbol is already in the watchlist."),
            _ => NotFound()
        };
    }

    private static async Task<IResult> RemoveItemAsync(
        Guid id,
        string symbol,
        ClaimsPrincipal principal,
        IWatchlistService watchlistService,
        CancellationToken cancellationToken)
    {
        if (!principal.TryGetUserId(out var userId))
        {
            return Results.Unauthorized();
        }

        var status = await watchlistService.RemoveItemAsync(
            userId,
            id,
            symbol,
            cancellationToken);

        return status == WatchlistChangeStatus.Success
            ? Results.NoContent()
            : NotFound();
    }

    private static IResult Conflict(string title)
    {
        return Results.Problem(
            type: ApiProblemTypes.Conflict,
            title: title,
            statusCode: StatusCodes.Status409Conflict);
    }

    private static IResult NotFound()
    {
        return Results.Problem(
            type: ApiProblemTypes.NotFound,
            title: "The requested watchlist item was not found.",
            statusCode: StatusCodes.Status404NotFound);
    }
}
