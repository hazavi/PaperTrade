using System.Security.Claims;
using FluentValidation;
using PaperTrade.Api.ErrorHandling;
using PaperTrade.Api.Extensions;
using PaperTrade.Application.Engagement;
using PaperTrade.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PaperTrade.Api.Endpoints;

public static class EngagementEndpoints
{
    public static IEndpointRouteBuilder MapEngagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var alerts = endpoints.MapGroup("/api/alerts").WithTags("Alerts").RequireAuthorization();
        alerts.MapGet("/", GetAlertsAsync);
        alerts.MapPost("/", CreateAlertAsync);
        alerts.MapDelete("/{id:guid}", DeleteAlertAsync);
        var notifications = endpoints.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();
        notifications.MapGet("/", GetNotificationsAsync);
        notifications.MapPost("/{id:guid}/read", MarkReadAsync);
        var preferences = endpoints.MapGroup("/api/notification-preferences").RequireAuthorization();
        preferences.MapGet("/", async (ClaimsPrincipal principal, PaperTradeDbContext db, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var id)) return Results.Unauthorized();
            var enabled = await db.Users.Where(x => x.Id == id).Select(x => x.EmailAlertsEnabled).SingleAsync(token);
            return Results.Ok(new { emailAlertsEnabled = enabled });
        });
        preferences.MapPut("/", async (EmailPreferenceRequest request, ClaimsPrincipal principal, PaperTradeDbContext db, CancellationToken token) =>
        {
            if (!principal.TryGetUserId(out var id)) return Results.Unauthorized();
            var user = await db.Users.SingleAsync(x => x.Id == id, token);
            user.SetEmailAlerts(request.EmailAlertsEnabled);
            await db.SaveChangesAsync(token);
            return Results.Ok(new { emailAlertsEnabled = user.EmailAlertsEnabled });
        });
        return endpoints;
    }

    public sealed record EmailPreferenceRequest(bool EmailAlertsEnabled);

    private static async Task<IResult> GetAlertsAsync(ClaimsPrincipal principal, IEngagementService service, CancellationToken token) =>
        principal.TryGetUserId(out var userId) ? Results.Ok(await service.GetAlertsAsync(userId, token)) : Results.Unauthorized();

    private static async Task<IResult> CreateAlertAsync(CreatePriceAlertRequest request,
        IValidator<CreatePriceAlertRequest> validator, ClaimsPrincipal principal,
        IEngagementService service, CancellationToken token)
    {
        if (!principal.TryGetUserId(out var userId)) return Results.Unauthorized();
        var validation = await validator.ValidateAsync(request, token);
        if (!validation.IsValid) return Results.ValidationProblem(
            validation.ToErrorDictionary(), type: ApiProblemTypes.Validation,
            title: "Validation failed.", statusCode: StatusCodes.Status400BadRequest);
        var alert = await service.CreateAlertAsync(userId, request, token);
        return Results.Created($"/api/alerts/{alert.Id}", alert);
    }

    private static async Task<IResult> DeleteAlertAsync(Guid id, ClaimsPrincipal principal,
        IEngagementService service, CancellationToken token) =>
        principal.TryGetUserId(out var userId) && await service.DeleteAlertAsync(userId, id, token)
            ? Results.NoContent()
            : Results.NotFound();

    private static async Task<IResult> GetNotificationsAsync(ClaimsPrincipal principal,
        IEngagementService service, CancellationToken token) =>
        principal.TryGetUserId(out var userId) ? Results.Ok(await service.GetNotificationsAsync(userId, token)) : Results.Unauthorized();

    private static async Task<IResult> MarkReadAsync(Guid id, ClaimsPrincipal principal,
        IEngagementService service, CancellationToken token) =>
        principal.TryGetUserId(out var userId) && await service.MarkReadAsync(userId, id, token)
            ? Results.NoContent()
            : Results.NotFound();
}
