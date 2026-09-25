namespace PaperTrade.Application.Authentication;

public sealed record AuthUser(
    Guid Id,
    string Email,
    string DisplayName);