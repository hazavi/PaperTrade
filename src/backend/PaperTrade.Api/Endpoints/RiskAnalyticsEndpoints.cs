using System.Globalization;
using System.Security.Claims;
using System.Text;
using PaperTrade.Api.Extensions;
using PaperTrade.Application.Portfolios;
using PaperTrade.Application.Trading;

namespace PaperTrade.Api.Endpoints;

public static class RiskAnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapRiskAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics").RequireAuthorization().WithTags("Risk and analytics");
        group.MapGet("/risk-limits", async (ClaimsPrincipal user, RiskAnalyticsService service, CancellationToken token) =>
            user.TryGetUserId(out var id) ? await Found(service.GetLimitsAsync(id, token)) : Results.Unauthorized());
        group.MapPut("/risk-limits", async (RiskLimitsDto limits, ClaimsPrincipal user, RiskAnalyticsService service, CancellationToken token) =>
        {
            if (!user.TryGetUserId(out var id)) return Results.Unauthorized();
            if (!Valid(limits.MaxDailyLossPercent) || !Valid(limits.MaxPositionConcentrationPercent))
                return Results.BadRequest(new { error = "Limits must be between 0 and 100 percent." });
            return await Found(service.SetLimitsAsync(id, limits, token));
        });
        group.MapPost("/size", async (SizeRequest request, ClaimsPrincipal user, RiskAnalyticsService service, CancellationToken token) =>
        {
            if (!user.TryGetUserId(out var id)) return Results.Unauthorized();
            try { return await Found(service.CalculateSizeAsync(id, request, token)); }
            catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        });
        group.MapGet("/performance", async (ClaimsPrincipal user, RiskAnalyticsService service, CancellationToken token) =>
            user.TryGetUserId(out var id) ? await Found(service.GetPerformanceAsync(id, token)) : Results.Unauthorized());
        group.MapGet("/journal", async (ClaimsPrincipal user, RiskAnalyticsService service, CancellationToken token) =>
            user.TryGetUserId(out var id) ? await Found(service.GetJournalAsync(id, token)) : Results.Unauthorized());
        group.MapPut("/journal/{orderId:guid}", async (Guid orderId, JournalNoteRequest request,
            ClaimsPrincipal user, RiskAnalyticsService service, CancellationToken token) =>
        {
            if (!user.TryGetUserId(out var id)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.Note) || request.Note.Length > 4000)
                return Results.BadRequest(new { error = "Note must be 1 to 4000 characters." });
            return await Found(service.SaveJournalAsync(id, orderId, request.Note, token));
        });
        group.MapDelete("/journal/{orderId:guid}", async (Guid orderId, ClaimsPrincipal user,
            RiskAnalyticsService service, CancellationToken token) =>
        {
            if (!user.TryGetUserId(out var id)) return Results.Unauthorized();
            return await service.DeleteJournalAsync(id, orderId, token) ? Results.NoContent() : Results.NotFound();
        });
        group.MapGet("/export/{kind}", ExportAsync);
        return app;
    }

    private static bool Valid(decimal? value) => value is null ||
        value is > 0 and <= 100 && decimal.Round(value.Value, 2) == value.Value;
    private static async Task<IResult> Found<T>(Task<T?> task) => await task is { } value ? Results.Ok(value) : Results.NotFound();
    private sealed record JournalNoteRequest(string Note);

    private static async Task<IResult> ExportAsync(string kind, ClaimsPrincipal user,
        RiskAnalyticsService analytics, ITradingService trading, CancellationToken token)
    {
        if (!user.TryGetUserId(out var id)) return Results.Unauthorized();
        var rows = new List<string[]>();
        switch (kind.ToLowerInvariant())
        {
            case "orders":
                rows.Add(["id", "symbol", "side", "type", "quantity", "status", "created_at", "executed_at"]);
                rows.AddRange((await trading.GetOrdersAsync(id, token)).Select(x => new[] { x.Id.ToString(), x.Symbol,
                    x.Side, x.Type, N(x.Quantity), x.Status, D(x.CreatedAt), D(x.ExecutedAt) }));
                break;
            case "executions":
                rows.Add(["id", "order_id", "symbol", "side", "quantity", "price", "value_usd", "fee_usd", "realized_pnl_usd", "executed_at"]);
                rows.AddRange((await trading.GetExecutionsAsync(id, token)).Select(x => new[] { x.Id.ToString(),
                    x.OrderId.ToString(), x.Symbol, x.Side, N(x.Quantity), N(x.Price), N(x.TotalValue),
                    N(x.Fee), N(x.RealizedPnl), D(x.ExecutedAt) }));
                break;
            case "portfolio":
                var performance = await analytics.GetPerformanceAsync(id, token);
                if (performance is null) return Results.NotFound();
                rows.Add(["recorded_at", "equity_usd", "cash_usd", "realized_pnl_usd", "unrealized_pnl_usd", "drawdown_percent"]);
                rows.AddRange(performance.History.Select(x => new[] { D(x.RecordedAt), N(x.Equity), N(x.Cash),
                    N(x.RealizedPnl), N(x.UnrealizedPnl), N(x.DrawdownPercent) }));
                break;
            case "journal":
                var journal = await analytics.GetJournalAsync(id, token);
                if (journal is null) return Results.NotFound();
                rows.Add(["id", "order_id", "note", "created_at", "updated_at"]);
                rows.AddRange(journal.Select(x => new[] { x.Id.ToString(), x.OrderId.ToString(), x.Note,
                    D(x.CreatedAt), D(x.UpdatedAt) }));
                break;
            default: return Results.NotFound();
        }
        var csv = new StringBuilder();
        foreach (var row in rows) csv.AppendLine(string.Join(",", row.Select(Csv)));
        return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"papertrade-{kind}.csv");
    }

    private static string N(decimal x) => x.ToString(CultureInfo.InvariantCulture);
    private static string D(DateTimeOffset? x) => x?.ToString("O", CultureInfo.InvariantCulture) ?? "";
    private static string Csv(string value)
    {
        // Prefix spreadsheet formulas so an exported note stays plain text when opened in Excel.
        var leading = value.TrimStart();
        if (leading.Length > 0 && "=+-@\t\r".Contains(leading[0]) &&
            !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
