using LankaConnect.Domain.Common;
using LankaConnect.Modules.Notifications.Domain.Enums;

namespace LankaConnect.Modules.Notifications.Domain;

/// <summary>
/// Notification entity for the in-app notification surface.
/// Originated in Phase 6A.6; moved into the Notifications module Domain layer
/// during Phase A W3.2 (2026-06-02) without behavior change.
/// </summary>
/// <remarks>
/// Temporarily derives from <c>LankaConnect.Domain.Common.BaseEntity</c> and
/// uses <c>LankaConnect.Domain.Common.Result</c> while the BuildingBlocks
/// elevation of these primitives is pending. Cut the legacy edge after
/// BuildingBlocks owns BaseEntity + Result + IRepository&lt;T&gt; (planned W4/W5).
/// </remarks>
public class Notification : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    // Optional metadata for linking to related entities.
    public string? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }

    // EF Core constructor.
    private Notification()
    {
        Title = null!;
        Message = null!;
    }

    private Notification(
        Guid userId,
        string title,
        string message,
        NotificationType type,
        string? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        IsRead = false;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = relatedEntityType;
    }

    /// <summary>Factory method to create a new notification.</summary>
    public static Result<Notification> Create(
        Guid userId,
        string title,
        string message,
        NotificationType type,
        string? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        if (userId == Guid.Empty)
            return Result<Notification>.Failure("User ID is required");

        if (string.IsNullOrWhiteSpace(title))
            return Result<Notification>.Failure("Title is required");

        if (title.Length > 200)
            return Result<Notification>.Failure("Title cannot exceed 200 characters");

        if (string.IsNullOrWhiteSpace(message))
            return Result<Notification>.Failure("Message is required");

        if (message.Length > 1000)
            return Result<Notification>.Failure("Message cannot exceed 1000 characters");

        var notification = new Notification(userId, title, message, type, relatedEntityId, relatedEntityType);
        return Result<Notification>.Success(notification);
    }

    /// <summary>Mark the notification as read.</summary>
    public Result MarkAsRead()
    {
        if (IsRead)
            return Result.Failure("Notification is already marked as read");

        IsRead = true;
        ReadAt = DateTime.UtcNow;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>Mark the notification as unread (for testing or admin purposes).</summary>
    public Result MarkAsUnread()
    {
        if (!IsRead)
            return Result.Failure("Notification is already unread");

        IsRead = false;
        ReadAt = null;
        MarkAsUpdated();

        return Result.Success();
    }
}
