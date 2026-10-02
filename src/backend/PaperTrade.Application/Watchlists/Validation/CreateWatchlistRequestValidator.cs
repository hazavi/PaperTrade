using FluentValidation;

namespace PaperTrade.Application.Watchlists.Validation;

public sealed class CreateWatchlistRequestValidator
    : AbstractValidator<CreateWatchlistRequest>
{
    public CreateWatchlistRequestValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Watchlist name is required.")
            .MaximumLength(100)
            .WithMessage(
                "Watchlist name must be 100 characters or fewer.");
    }
}
