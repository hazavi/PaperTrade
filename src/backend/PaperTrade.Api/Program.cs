using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using PaperTrade.Api.Endpoints;
using PaperTrade.Api.ErrorHandling;
using PaperTrade.Application;
using PaperTrade.Infrastructure;
using PaperTrade.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "Frontend";

var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var problem = context.ProblemDetails;

        problem.Instance =
            context.HttpContext.Request.Path.ToString();

        problem.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;

        var hasPaperTradeType =
            problem.Type?.StartsWith(
                "urn:papertrade:error:",
                StringComparison.Ordinal) == true;

        if (hasPaperTradeType)
        {
            return;
        }

        switch (problem.Status)
        {
            case StatusCodes.Status401Unauthorized:
                problem.Type = ApiProblemTypes.Unauthorized;
                problem.Title = "Authentication is required.";
                break;

            case StatusCodes.Status404NotFound:
                problem.Type = ApiProblemTypes.NotFound;
                problem.Title =
                    "The requested resource was not found.";
                break;

            case StatusCodes.Status500InternalServerError:
                problem.Type =
                    ApiProblemTypes.InternalServerError;
                problem.Title =
                    "An unexpected error occurred.";
                break;
        }
    };
});
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields =
        HttpLoggingFields.RequestMethod |
        HttpLoggingFields.RequestPath |
        HttpLoggingFields.ResponseStatusCode |
        HttpLoggingFields.Duration;

    options.CombineLogs = true;
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "papertrade.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;

            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode =
                StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseHttpLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();

    var dbContext =
        scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();

    await dbContext.Database.MigrateAsync();

    app.MapOpenApi();
}

app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/status", () =>
{
    return Results.Ok(new { status = "ok" });
});

app.MapAuthenticationEndpoints();

app.Run();
