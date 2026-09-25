namespace PaperTrade.Application.Authentication;

public enum RegistrationStatus
{
    Success,
    EmailAlreadyExists
}

public sealed record RegistrationResult(
    RegistrationStatus Status,
    AuthUser? User);