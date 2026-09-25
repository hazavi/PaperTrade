using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using PaperTrade.Api.Extensions;
using PaperTrade.Application.Authentication;
using ApplicationAuthenticationService =
    PaperTrade.Application.Authentication.IAuthenticationService;

namespace PaperTrade.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);

        group.MapPost("/logout", (Delegate)LogoutAsync)
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        ApplicationAuthenticationService authenticationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(
                validationResult.ToErrorDictionary());
        }

        var result = await authenticationService.RegisterAsync(
            request,
            cancellationToken);

        if (result.Status ==
            RegistrationStatus.EmailAlreadyExists)
        {
            return Results.Problem(
                type: "duplicate_email",
                title: "Email is already registered.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var user = result.User!;

        await SignInAsync(httpContext, user);

        return Results.Created(
            $"/api/auth/users/{user.Id}",
            user);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        ApplicationAuthenticationService authenticationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(
                validationResult.ToErrorDictionary());
        }

        var user = await authenticationService.AuthenticateAsync(
            request,
            cancellationToken);

        if (user is null)
        {
            return Results.Problem(
                type: "invalid_credentials",
                title: "Invalid email or password.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        await SignInAsync(httpContext, user);

        return Results.Ok(user);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext)
    {
        await httpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        ApplicationAuthenticationService authenticationService,
        CancellationToken cancellationToken)
    {
        var userIdValue = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await authenticationService.GetUserAsync(
            userId,
            cancellationToken);

        return user is null
            ? Results.Unauthorized()
            : Results.Ok(user);
    }

    private static Task SignInAsync(
        HttpContext httpContext,
        AuthUser user)
    {
        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        return httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);
    }
}
