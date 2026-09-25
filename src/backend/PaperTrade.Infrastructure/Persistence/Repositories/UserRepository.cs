using Microsoft.EntityFrameworkCore;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Users;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(PaperTradeDbContext dbContext)
    : IUserRepository
{
    public Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.Email == email,
            cancellationToken);
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Email == email,
                cancellationToken);
    }

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Include(user => user.Portfolio)
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }

    public void Add(User user)
    {
        dbContext.Users.Add(user);
    }
}