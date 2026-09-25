using PaperTrade.Domain.Users;

namespace PaperTrade.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    void Add(User user);
}