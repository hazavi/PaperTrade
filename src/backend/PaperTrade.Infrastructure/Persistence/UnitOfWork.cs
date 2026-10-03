using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Authentication;
using System.Data;

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

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
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
