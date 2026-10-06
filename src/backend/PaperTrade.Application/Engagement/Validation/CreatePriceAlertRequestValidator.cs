using FluentValidation;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Engagement.Validation;

public sealed class CreatePriceAlertRequestValidator
    : AbstractValidator<CreatePriceAlertRequest>
{
    public CreatePriceAlertRequestValidator()
    {
        RuleFor(request => request.Symbol).NotEmpty().MaximumLength(32)
            .Matches("^[A-Za-z0-9./-]+$").WithMessage("Enter a valid symbol.");
        RuleFor(request => request.Symbol)
            .Must(symbol => symbol is null || !symbol.Contains('/') || SupportedPairs.Create(symbol) is not null)
            .WithMessage("This market pair is not supported.");
        RuleFor(request => request.Direction)
            .Must(direction =>
                string.Equals(direction, "above", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(direction, "below", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Direction must be above or below.");
        RuleFor(request => request.TargetPrice).GreaterThan(0).PrecisionScale(18, 6, false);
    }
}
