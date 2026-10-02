using FluentValidation;

namespace PaperTrade.Application.Authentication.Validation;

public sealed class RegisterRequestValidator
    : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(320)
            .WithMessage("Email must be 320 characters or fewer.")
            .EmailAddress()
            .WithMessage("Enter a valid email address.");

        RuleFor(request => request.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(12)
            .WithMessage("Password must be at least 12 characters.")
            .MaximumLength(128)
            .WithMessage("Password must be 128 characters or fewer.");

        RuleFor(request => request.DisplayName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Display name is required.")
            .MinimumLength(2)
            .WithMessage("Display name must be at least 2 characters.")
            .MaximumLength(100)
            .WithMessage(
                "Display name must be 100 characters or fewer.");
    }
}
