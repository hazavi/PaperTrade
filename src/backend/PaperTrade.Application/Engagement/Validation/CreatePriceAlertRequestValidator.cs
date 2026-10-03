using FluentValidation;

namespace PaperTrade.Application.Engagement.Validation;

public sealed class CreatePriceAlertRequestValidator
    : AbstractValidator<CreatePriceAlertRequest>
{
    public CreatePriceAlertRequestValidator()
    {
        RuleFor(request => request.Symbol).NotEmpty().MaximumLength(32)
            .Matches("^[A-Za-z0-9.-]+$").WithMessage("Enter a valid symbol.");
        RuleFor(request => request.Direction)
            .Must(direction =>
                string.Equals(direction, "above", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(direction, "below", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Direction must be above or below.");
        RuleFor(request => request.TargetPrice).GreaterThan(0).PrecisionScale(18, 6, false);
    }
}
