using FluentValidation;

namespace PaperTrade.Application.Watchlists.Validation;

public sealed class AddWatchlistItemRequestValidator
    : AbstractValidator<AddWatchlistItemRequest>
{
    public AddWatchlistItemRequestValidator()
    {
        RuleFor(request => request.Symbol)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Symbol is required.")
            .MaximumLength(32)
            .WithMessage("Symbol must be 32 characters or fewer.")
            .Matches("^[A-Za-z0-9.-]+$")
            .WithMessage("Symbol contains unsupported characters.");
    }
}
