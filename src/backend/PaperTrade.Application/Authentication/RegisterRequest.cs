namespace PaperTrade.Application.Authentication;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName);