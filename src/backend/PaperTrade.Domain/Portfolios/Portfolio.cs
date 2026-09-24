using PaperTrade.Domain.Users;

namespace PaperTrade.Domain.Portfolios;

public sealed class Portfolio
{
    private Portfolio()
    {
    }

    public Portfolio(
        Guid id,
        Guid userId,
        string name,
        decimal cashBalance,
        decimal initialBalance,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Portfolio ID cannot be empty.", nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (cashBalance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cashBalance),
                "Cash balance cannot be negative.");
        }

        if (initialBalance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialBalance),
                "Initial balance cannot be negative.");
        }

        Id = id;
        UserId = userId;
        Name = name;
        CashBalance = cashBalance;
        InitialBalance = initialBalance;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal CashBalance { get; private set; }

    public decimal InitialBalance { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public User User { get; private set; } = null!;
}