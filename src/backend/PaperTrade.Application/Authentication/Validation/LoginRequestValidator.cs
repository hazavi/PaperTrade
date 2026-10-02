using FluentValidation;

namespace PaperTrade.Application.Authentication.Validation;

public sealed class LoginRequestValidator
    : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
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
            .MaximumLength(128)
            .WithMessage("Password must be 128 characters or fewer.");
    }
}
