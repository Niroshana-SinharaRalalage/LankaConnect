using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Notifications.Domain.Enums;

namespace LankaConnect.Modules.Notifications.Domain;

/// <summary>
/// Notification entity for the in-app notification surface.
/// Originated in Phase 6A.6; moved into the Notifications module Domain layer
/// during Phase A W3.2 (2026-06-02); migrated to <see cref="Entity{TId}"/> +
/// <see cref="IAuditable"/> in W3A pilot (2026-06-05) per ADR-007.
/// </summary>
/// <remarks>
/// Audit fields (<see cref="CreatedAt"/>, <see cref="CreatedBy"/>,
/// <see cref="UpdatedAt"/>, <see cref="UpdatedBy"/>) are populated by
/// <c>BaseDbContext.AuditableInterceptor</c> on SaveChanges — domain code
/// treats them as read-only. No <c>MarkAsUpdated()</c> calls; the interceptor
/// stamps UpdatedAt/UpdatedBy automatically when EF detects state changes.
/// </remarks>
public class Notification : Entity<Guid>, IAuditable
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

    // IAuditable — interceptor-populated; treat as read-only from domain code.
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // EF Core constructor.
    private Notification() : base()
    {
        Title = null!;
        Message = null!;
    }

    private Notification(
        Guid id,
        Guid userId,
        string title,
        string message,
        NotificationType type,
        string? relatedEntityId,
        string? relatedEntityType)
        : base(id)
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
            return Result<Notification>.Failure(NotificationErrors.UserIdRequired);

        if (string.IsNullOrWhiteSpace(title))
            return Result<Notification>.Failure(NotificationErrors.TitleRequired);

        if (title.Length > 200)
            return Result<Notification>.Failure(NotificationErrors.TitleTooLong);

        if (string.IsNullOrWhiteSpace(message))
            return Result<Notification>.Failure(NotificationErrors.MessageRequired);

        if (message.Length > 1000)
            return Result<Notification>.Failure(NotificationErrors.MessageTooLong);

        var notification = new Notification(
            Guid.NewGuid(),
            userId,
            title,
            message,
            type,
            relatedEntityId,
            relatedEntityType);
        return Result<Notification>.Success(notification);
    }

    /// <summary>Mark the notification as read.</summary>
    public Result MarkAsRead()
    {
        if (IsRead)
            return Result.Failure(NotificationErrors.AlreadyRead);

        IsRead = true;
        ReadAt = DateTime.UtcNow;
        // No MarkAsUpdated() — AuditableInterceptor stamps UpdatedAt/UpdatedBy
        // when the change tracker reports EntityState.Modified.

        return Result.Success();
    }

    /// <summary>Mark the notification as unread (for testing or admin purposes).</summary>
    public Result MarkAsUnread()
    {
        if (!IsRead)
            return Result.Failure(NotificationErrors.AlreadyUnread);

        IsRead = false;
        ReadAt = null;

        return Result.Success();
    }
}

/// <summary>
/// Typed error codes for <see cref="Notification"/> domain failures. Codes follow
/// the <c>Notification.&lt;Subject&gt;</c> convention so they remain stable
/// across i18n message changes (per ADR-001).
/// </summary>
internal static class NotificationErrors
{
    public static readonly Error UserIdRequired =
        new("Notification.UserIdRequired", "User ID is required");

    public static readonly Error TitleRequired =
        new("Notification.TitleRequired", "Title is required");

    public static readonly Error TitleTooLong =
        new("Notification.TitleTooLong", "Title cannot exceed 200 characters");

    public static readonly Error MessageRequired =
        new("Notification.MessageRequired", "Message is required");

    public static readonly Error MessageTooLong =
        new("Notification.MessageTooLong", "Message cannot exceed 1000 characters");

    public static readonly Error AlreadyRead =
        new("Notification.AlreadyRead", "Notification is already marked as read");

    public static readonly Error AlreadyUnread =
        new("Notification.AlreadyUnread", "Notification is already unread");
}
