using LankaConnect.Modules.Notifications.Domain;
using LankaConnect.Modules.Notifications.Domain.Enums;

namespace LankaConnect.Modules.Notifications.Domain.Tests;

/// <summary>
/// Wave4.9.1.8 (2026-06-08): per-mutator behavior tests for the
/// Notification aggregate.
/// </summary>
/// <remarks>
/// IMPORTANT: <see cref="Notification"/> is the W3A pilot for the
/// <c>BB.Entity&lt;TId&gt; + IAuditable</c> pattern. Its <c>UpdatedAt</c>
/// is stamped by <c>BaseDbContext.AuditableInterceptor</c> at
/// SaveChanges - NOT by direct domain assignment. Therefore these
/// tests do NOT assert UpdatedAt advancement (as Wave4.9.1.6/7 do for
/// PhotoAlbum/Form); they assert mutator state-transition correctness
/// only. The interceptor's UpdatedAt stamping is covered by the existing
/// integration tests + the IAuditable-Create coverage in
/// <c>tests/LankaConnect.Domain.Tests/Common/IAuditableAggregateRoundTripTests.Notification_Create_Has_FreshAuditFields</c>.
///
/// Per CLAUDE.md §13.1 trigger T2 (state-transition mutator).
/// Notifications.Domain.Tests project was missing all test files before
/// this commit - this is the first test file in it.
/// </remarks>
public sealed class NotificationBehaviorTests
{
    private static Notification NewUnreadNotification()
    {
        var result = Notification.Create(
            userId: Guid.NewGuid(),
            title: "Wave4.9.1.8 Smoke",
            message: "Behavior coverage test message",
            type: NotificationType.System);
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public void Create_Sets_IsRead_False_And_ReadAt_Null()
    {
        var n = NewUnreadNotification();

        n.IsRead.Should().BeFalse(because: "freshly-created notification is unread");
        n.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Create_Sets_CreatedAt_To_UtcNow()
    {
        var before = DateTime.UtcNow;
        var n = NewUnreadNotification();

        n.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        n.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
        n.UpdatedAt.Should().BeNull(
            because: "fresh-create has no UpdatedAt assignment; the AuditableInterceptor stamps it on the first SaveChanges that follows a domain mutation.");
    }

    [Fact]
    public void Create_With_EmptyUserId_Fails()
    {
        var result = Notification.Create(
            userId: Guid.Empty,
            title: "T", message: "M", type: NotificationType.System);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.UserIdRequired");
    }

    [Fact]
    public void Create_With_TooLongTitle_Fails()
    {
        var result = Notification.Create(
            userId: Guid.NewGuid(),
            title: new string('x', 201),
            message: "M",
            type: NotificationType.System);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.TitleTooLong");
    }

    [Fact]
    public void Create_With_TooLongMessage_Fails()
    {
        var result = Notification.Create(
            userId: Guid.NewGuid(),
            title: "T",
            message: new string('x', 1001),
            type: NotificationType.System);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.MessageTooLong");
    }

    [Fact]
    public void MarkAsRead_From_Unread_Succeeds_And_Sets_ReadAt()
    {
        var n = NewUnreadNotification();
        var before = DateTime.UtcNow;

        var result = n.MarkAsRead();

        result.IsSuccess.Should().BeTrue();
        n.IsRead.Should().BeTrue();
        n.ReadAt.Should().NotBeNull();
        n.ReadAt!.Value.Should().BeOnOrAfter(before.AddSeconds(-1));
        n.ReadAt!.Value.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void MarkAsRead_From_Read_Fails_With_Typed_Error()
    {
        var n = NewUnreadNotification();
        n.MarkAsRead().IsSuccess.Should().BeTrue();

        var result = n.MarkAsRead();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.AlreadyRead");
    }

    [Fact]
    public void MarkAsUnread_From_Read_Succeeds_And_Clears_ReadAt()
    {
        var n = NewUnreadNotification();
        n.MarkAsRead().IsSuccess.Should().BeTrue();

        var result = n.MarkAsUnread();

        result.IsSuccess.Should().BeTrue();
        n.IsRead.Should().BeFalse();
        n.ReadAt.Should().BeNull(
            because: "MarkAsUnread is the inverse of MarkAsRead - the ReadAt timestamp must clear so re-marking re-stamps it.");
    }

    [Fact]
    public void MarkAsUnread_From_Unread_Fails_With_Typed_Error()
    {
        var n = NewUnreadNotification();

        var result = n.MarkAsUnread();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.AlreadyUnread");
    }

    [Fact]
    public void Read_Unread_Read_Cycle_Re_Stamps_ReadAt()
    {
        var n = NewUnreadNotification();
        n.MarkAsRead().IsSuccess.Should().BeTrue();
        var firstReadAt = n.ReadAt!.Value;
        n.MarkAsUnread().IsSuccess.Should().BeTrue();
        Thread.Sleep(20);

        n.MarkAsRead().IsSuccess.Should().BeTrue();

        n.IsRead.Should().BeTrue();
        n.ReadAt.Should().NotBeNull();
        n.ReadAt!.Value.Should().BeAfter(firstReadAt,
            because: "re-marking a notification as read after unread should produce a fresh ReadAt timestamp, not retain the old one.");
    }
}
