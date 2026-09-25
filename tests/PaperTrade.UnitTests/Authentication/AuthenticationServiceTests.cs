using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Abstractions.Security;
using PaperTrade.Application.Authentication;
using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.Users;

namespace PaperTrade.UnitTests.Authentication;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithNewEmail_CreatesUserAndPortfolio()
    {
        var users = new FakeUserRepository();
        var portfolios = new FakePortfolioRepository();
        var unitOfWork = new FakeUnitOfWork();
        var passwordHasher = new FakePasswordHasher();

        var service = new AuthenticationService(
            users,
            portfolios,
            unitOfWork,
            passwordHasher);

        var request = new RegisterRequest(
            "  Trader@Example.COM ",
            "a-long-passphrase",
            "  Paper Trader  ");

        var result = await service.RegisterAsync(
            request,
            CancellationToken.None);

        Assert.Equal(RegistrationStatus.Success, result.Status);
        Assert.NotNull(result.User);

        Assert.NotNull(users.AddedUser);
        Assert.Equal("trader@example.com", users.AddedUser.Email);
        Assert.Equal("hashed::a-long-passphrase", users.AddedUser.PasswordHash);
        Assert.Equal("Paper Trader", users.AddedUser.DisplayName);

        Assert.NotNull(portfolios.AddedPortfolio);
        Assert.Equal(users.AddedUser.Id, portfolios.AddedPortfolio.UserId);
        Assert.Equal("Paper Portfolio", portfolios.AddedPortfolio.Name);
        Assert.Equal(100_000m, portfolios.AddedPortfolio.CashBalance);
        Assert.Equal(100_000m, portfolios.AddedPortfolio.InitialBalance);

        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_DoesNotCreateRecords()
    {
        var users = new FakeUserRepository
        {
            EmailExists = true
        };

        var portfolios = new FakePortfolioRepository();
        var unitOfWork = new FakeUnitOfWork();

        var service = new AuthenticationService(
            users,
            portfolios,
            unitOfWork,
            new FakePasswordHasher());

        var result = await service.RegisterAsync(
            new RegisterRequest(
                "trader@example.com",
                "a-long-passphrase",
                "Paper Trader"),
            CancellationToken.None);

        Assert.Equal(
            RegistrationStatus.EmailAlreadyExists,
            result.Status);

        Assert.Null(result.User);
        Assert.Null(users.AddedUser);
        Assert.Null(portfolios.AddedPortfolio);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task AuthenticateAsync_WithCorrectPassword_ReturnsUser()
    {
        var storedUser = CreateStoredUser();

        var users = new FakeUserRepository
        {
            UserToReturn = storedUser
        };

        var service = new AuthenticationService(
            users,
            new FakePortfolioRepository(),
            new FakeUnitOfWork(),
            new FakePasswordHasher());

        var result = await service.AuthenticateAsync(
            new LoginRequest(
                " TRADER@EXAMPLE.COM ",
                "correct-password"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(storedUser.Id, result.Id);
        Assert.Equal(storedUser.Email, result.Email);
        Assert.Equal(storedUser.DisplayName, result.DisplayName);
        Assert.Equal("trader@example.com", users.LastRequestedEmail);
    }

    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_ReturnsNull()
    {
        var users = new FakeUserRepository
        {
            UserToReturn = CreateStoredUser()
        };

        var service = new AuthenticationService(
            users,
            new FakePortfolioRepository(),
            new FakeUnitOfWork(),
            new FakePasswordHasher());

        var result = await service.AuthenticateAsync(
            new LoginRequest(
                "trader@example.com",
                "wrong-password"),
            CancellationToken.None);

        Assert.Null(result);
    }

    private static User CreateStoredUser()
    {
        return new User(
            Guid.NewGuid(),
            "trader@example.com",
            "hashed::correct-password",
            "Paper Trader",
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public bool EmailExists { get; init; }

        public User? UserToReturn { get; init; }

        public User? AddedUser { get; private set; }

        public string? LastRequestedEmail { get; private set; }

        public Task<bool> EmailExistsAsync(
            string email,
            CancellationToken cancellationToken)
        {
            LastRequestedEmail = email;
            return Task.FromResult(EmailExists);
        }

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken)
        {
            LastRequestedEmail = email;
            return Task.FromResult(UserToReturn);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = UserToReturn?.Id == id
                ? UserToReturn
                : null;

            return Task.FromResult(result);
        }

        public void Add(User user)
        {
            AddedUser = user;
        }
    }

    private sealed class FakePortfolioRepository : IPortfolioRepository
    {
        public Portfolio? AddedPortfolio { get; private set; }

        public void Add(Portfolio portfolio)
        {
            AddedPortfolio = portfolio;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            SaveChangesCallCount++;
            return Task.FromResult(2);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"hashed::{password}";
        }

        public bool Verify(string passwordHash, string password)
        {
            return passwordHash == Hash(password);
        }
    }
}