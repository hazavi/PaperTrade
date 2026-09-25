using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Abstractions.Security;
using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.Users;

namespace PaperTrade.Application.Authentication;

public sealed class AuthenticationService(
    IUserRepository userRepository,
    IPortfolioRepository portfolioRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher)
    : IAuthenticationService
{
    private const string DefaultPortfolioName = "Paper Portfolio";
    private const decimal StartingBalance = 100_000m;

    public async Task<RegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        if (await userRepository.EmailExistsAsync(
                email,
                cancellationToken))
        {
            return new RegistrationResult(
                RegistrationStatus.EmailAlreadyExists,
                null);
        }

        var userId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var passwordHash = passwordHasher.Hash(request.Password);

        var user = new User(
            userId,
            email,
            passwordHash,
            request.DisplayName.Trim(),
            createdAt);

        var portfolio = new Portfolio(
            Guid.NewGuid(),
            userId,
            DefaultPortfolioName,
            StartingBalance,
            StartingBalance,
            createdAt);

        userRepository.Add(user);
        portfolioRepository.Add(portfolio);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegistrationResult(
            RegistrationStatus.Success,
            MapUser(user));
    }

    public async Task<AuthUser?> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        var user = await userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var passwordIsValid = passwordHasher.Verify(
            user.PasswordHash,
            request.Password);

        return passwordIsValid
            ? MapUser(user)
            : null;
    }

    public async Task<AuthUser?> GetUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            userId,
            cancellationToken);

        return user is null
            ? null
            : MapUser(user);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static AuthUser MapUser(User user)
    {
        return new AuthUser(
            user.Id,
            user.Email,
            user.DisplayName);
    }
}