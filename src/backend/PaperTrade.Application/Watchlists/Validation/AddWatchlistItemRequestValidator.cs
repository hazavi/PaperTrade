using FluentValidation;
using PaperTrade.Domain.Instruments;

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
            .Matches("^[A-Za-z0-9./-]+$")
            .WithMessage("Symbol contains unsupported characters.");
        RuleFor(request => request.Symbol)
            .Must(symbol => symbol is null || !symbol.Contains('/') || SupportedPairs.Create(symbol) is not null)
            .WithMessage("This market pair is not supported.");
    }
}
