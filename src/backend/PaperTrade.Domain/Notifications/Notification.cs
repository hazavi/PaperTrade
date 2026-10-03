using PaperTrade.Domain.Users;

namespace PaperTrade.Domain.Notifications;

public sealed class Notification
{
    private Notification() { }

    public Notification(Guid id, Guid userId, string title,
        string message, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Notification ID cannot be empty.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Id = id;
        UserId = userId;
        Title = title.Trim();
        Message = message.Trim();
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public User User { get; private set; } = null!;

    public void MarkRead() => IsRead = true;
}
