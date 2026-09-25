using FluentValidation.TestHelper;
using PaperTrade.Application.Authentication;
using PaperTrade.Application.Authentication.Validation;

namespace PaperTrade.UnitTests.Authentication;

public sealed class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        var request = new RegisterRequest(
            "trader@example.com",
            "a-long-passphrase",
            "Paper Trader");

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    public void Validate_WithInvalidEmail_HasEmailError(string email)
    {
        var request = new RegisterRequest(
            email,
            "a-long-passphrase",
            "Paper Trader");

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(value => value.Email);
    }

    [Fact]
    public void Validate_WithShortPassword_HasPasswordError()
    {
        var request = new RegisterRequest(
            "trader@example.com",
            "too-short",
            "Paper Trader");

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(value => value.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Validate_WithInvalidDisplayName_HasDisplayNameError(
        string displayName)
    {
        var request = new RegisterRequest(
            "trader@example.com",
            "a-long-passphrase",
            displayName);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(value => value.DisplayName);
    }
}