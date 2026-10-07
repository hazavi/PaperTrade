using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PaperTrade.Api.BackgroundServices;
using PaperTrade.Api.Endpoints;
using PaperTrade.Api.ErrorHandling;
using PaperTrade.Api.Health;
using PaperTrade.Api.Realtime;
using PaperTrade.Application;
using PaperTrade.Infrastructure;
using PaperTrade.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

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
builder.Services.AddSignalR();
builder.Services.AddSingleton<MarketSubscriptionTracker>();
builder.Services.AddHostedService<CompetitionWorker>();

if (builder.Configuration.GetValue("MarketDataWorker:Enabled", true))
    builder.Services.AddHostedService<MarketDataWorker>();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("orders", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

var telemetry = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("PaperTrade.Api"));

telemetry.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation();
    if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        tracing.AddOtlpExporter();
});

telemetry.WithMetrics(metrics =>
{
    metrics.AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter();
    if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        metrics.AddOtlpExporter();
});

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

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnostic, context) =>
    {
        diagnostic.Set("RequestId", context.TraceIdentifier);
        diagnostic.Set("UserId", context.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
    };
});
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment() ||
    app.Configuration.GetValue("Database:ApplyMigrations", false))
{
    await using var scope = app.Services.CreateAsyncScope();

    var dbContext =
        scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();

    await dbContext.Database.MigrateAsync();

    if (app.Environment.IsDevelopment()) app.MapOpenApi();
}

app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/api/status", () =>
{
    return Results.Ok(new { status = "ok" });
});

app.MapAuthenticationEndpoints();
app.MapMarketEndpoints();
app.MapWatchlistEndpoints();
app.MapOrderEndpoints();
app.MapPortfolioEndpoints();
app.MapRiskAnalyticsEndpoints();
app.MapChartLayoutEndpoints();
app.MapCompetitionEndpoints();
app.MapEngagementEndpoints();
app.MapLeaderboardEndpoints();
app.MapHub<MarketHub>("/hubs/market");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponseAsync
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthResponseAsync
});
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync(JsonSerializer.Serialize(new
    {
        status = report.Status.ToString().ToLowerInvariant(),
        checks = report.Entries.ToDictionary(entry => entry.Key,
            entry => entry.Value.Status.ToString().ToLowerInvariant())
    }));
}
