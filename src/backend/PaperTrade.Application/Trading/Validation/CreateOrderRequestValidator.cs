using FluentValidation;

namespace PaperTrade.Application.Trading.Validation;

public sealed class CreateOrderRequestValidator
    : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.Symbol)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[A-Za-z0-9.-]+$")
            .WithMessage("Enter a valid symbol.");

        RuleFor(request => request.Side)
            .Must(side =>
                string.Equals(side, "buy", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(side, "sell", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Side must be buy or sell.");

        RuleFor(request => request.Type)
            .Must(type => string.Equals(type, "market", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only market orders are supported.");

        RuleFor(request => request.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000m)
            .PrecisionScale(18, 6, false);
    }
}
