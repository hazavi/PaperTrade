using PaperTrade.Domain.Portfolios;

namespace PaperTrade.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    public User(
        Guid id,
        string email,
        string passwordHash,
        string displayName,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public Portfolio? Portfolio { get; private set; }
}