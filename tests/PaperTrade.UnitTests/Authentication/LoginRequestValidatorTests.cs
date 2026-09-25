using FluentValidation.TestHelper;
using PaperTrade.Application.Authentication;
using PaperTrade.Application.Authentication.Validation;

namespace PaperTrade.UnitTests.Authentication;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        var request = new LoginRequest(
            "trader@example.com",
            "a-long-passphrase");

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithInvalidEmail_HasEmailError()
    {
        var request = new LoginRequest(
            "not-an-email",
            "a-long-passphrase");

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(value => value.Email);
    }

    [Fact]
    public void Validate_WithEmptyPassword_HasPasswordError()
    {
        var request = new LoginRequest(
            "trader@example.com",
            string.Empty);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(value => value.Password);
    }
}