using FluentValidation;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Trading.Validation;

public sealed class CreateOrderRequestValidator
    : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.Symbol)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[A-Za-z0-9./-]+$")
            .WithMessage("Enter a valid symbol.");
        RuleFor(request => request.Symbol)
            .Must(symbol => symbol is null || !symbol.Contains('/') || SupportedPairs.Create(symbol) is not null)
            .WithMessage("This market pair is not supported.");

        RuleFor(request => request.Side)
            .Must(side =>
                string.Equals(side, "buy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(side, "sell", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Side must be buy or sell.");

        RuleFor(request => request.Type)
            .Must(type => new[] { "market", "limit", "stop", "bracket" }
                .Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Type must be market, limit, stop, or bracket.");

        RuleFor(request => request.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000m)
            .PrecisionScale(18, 6, false);
        RuleFor(request => request)
            .Must(request => request.Type?.ToLowerInvariant() switch
            {
                "limit" or "stop" => request.Price is > 0 &&
                    request.TakeProfit is null && request.StopLoss is null,
                "bracket" => request.Side?.Equals("buy", StringComparison.OrdinalIgnoreCase) == true &&
                    request.Price is null && request.TakeProfit is > 0 && request.StopLoss is > 0,
                "market" => request.Price is null && request.TakeProfit is null && request.StopLoss is null,
                _ => true
            })
            .WithMessage("Provide the required prices for the selected order type.");
        RuleFor(request => request.ExpiresAt)
            .Must(value => value is null || value > DateTimeOffset.UtcNow &&
                value <= DateTimeOffset.UtcNow.AddDays(30))
            .WithMessage("Expiration must be within 30 days.");
    }
}
