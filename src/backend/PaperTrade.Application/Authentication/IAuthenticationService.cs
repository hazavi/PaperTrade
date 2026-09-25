namespace PaperTrade.Application.Authentication;

public interface IAuthenticationService
{
    Task<RegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<AuthUser?> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<AuthUser?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
}