using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Authentication;

namespace PaperTrade.Infrastructure.Persistence;

internal sealed class UnitOfWork(PaperTradeDbContext dbContext)
    : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsDuplicateEmailViolation(exception))
        {
            throw new DuplicateEmailException(exception);
        }
    }

    private static bool IsDuplicateEmailViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_users_email"
        };
    }
}
