var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "Frontend";

var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(frontendCorsPolicy);

app.MapGet("/api/status", () =>
{
    return Results.Ok(new { status = "ok" });
});

app.Run();