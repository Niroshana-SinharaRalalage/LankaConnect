using LankaConnect.Modules.Communications.Domain.Entities;
// Wave 8.5.e (2026-07-19): Email VO promoted from
// LankaConnect.Products.LankaEvents.Domain.ValueObjects.Email to
// LankaConnect.SharedKernel.Contact.Email in Wave 8.5-cleanup 2026-07-18
// (commit d13e2b0b, ExtractabilityAudit GAP-6). User.Create now consumes the
// SharedKernel VO. The Communications.Domain.ValueObjects.Email lives as its
// own primitive for the EmailMessage aggregate — alias the pan-platform Email
// as UserEmail here so the two remain unambiguous at call sites in this file.
using UserEmail = LankaConnect.SharedKernel.Contact.Email;
using LankaConnect.SharedKernel.Money;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Notifications.Domain;
using LankaConnect.Modules.Notifications.Domain.Enums;

namespace LankaConnect.Domain.Tests.Common;

/// <summary>
/// Gap G1.b (testing-discipline backfill, 2026-06-08): verifies that each
/// IAuditable aggregate root sets CreatedAt + Id correctly on construction
/// via its public factory method. Coverage for the
/// <c>CreatedAt = DateTime.MinValue</c> regression class that shipped
/// silently between W3D and today's first successful deploy.
/// </summary>
/// <remarks>
/// Per CLAUDE.md Â§13.1 trigger T2 (mutator touching IAuditable). Each test
/// invokes the canonical factory with minimum valid inputs, asserts the
/// factory returned success, then asserts the audit fields.
///
/// Existing coverage already exists for Collection, Sponsor, EventAnalytics,
/// TicketScanLog, and TierAssignment in
/// <c>tests/LankaConnect.Application.Tests</c>. These tests cover the 5
/// remaining aggregates that didn't have CreatedAt-specific tests:
/// User, EmailGroup, Notification, PhotoAlbum, Form.
///
/// Event aggregate is intentionally NOT in this file â€” its factory takes
/// 9 parameters (EventTitle, EventDescription, etc.) and dedicated coverage
/// belongs in the Application.Tests project alongside the existing
/// EventTests fixture. Tracked separately for G1.b completeness.
/// </remarks>
public sealed class IAuditableAggregateRoundTripTests
{
    private static readonly TimeSpan AuditFreshness = TimeSpan.FromSeconds(5);

    [Fact]
    public void User_Create_Has_FreshAuditFields()
    {
        var emailResult = UserEmail.Create("test+g1b@lankaconnect.app");
        emailResult.IsSuccess.Should().BeTrue();
        var before = DateTime.UtcNow;

        var userResult = User.Create(emailResult.Value, firstName: "Test", lastName: "G1B");

        userResult.IsSuccess.Should().BeTrue(because: userResult.Error?.ToString());
        var user = userResult.Value;
        user.Id.Should().NotBe(Guid.Empty);
        user.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        user.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
        user.UpdatedAt.Should().BeNull(because: "freshly-created entity has not been mutated.");
    }

    [Fact]
    public void EmailGroup_Create_Has_FreshAuditFields()
    {
        var before = DateTime.UtcNow;

        var result = EmailGroup.Create(
            name: "Test Group",
            ownerId: Guid.NewGuid(),
            emailAddresses: "a@x.com, b@y.com",
            description: "G1.b smoke test");

        result.IsSuccess.Should().BeTrue(because: result.Error);
        var group = result.Value;
        group.Id.Should().NotBe(Guid.Empty);
        group.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        group.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
        group.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Notification_Create_Has_FreshAuditFields()
    {
        var before = DateTime.UtcNow;

        var result = Notification.Create(
            userId: Guid.NewGuid(),
            title: "Test notification",
            message: "G1.b smoke test message",
            type: NotificationType.System);

        result.IsSuccess.Should().BeTrue();
        var notification = result.Value;
        notification.Id.Should().NotBe(Guid.Empty);
        notification.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        notification.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
        notification.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void PhotoAlbum_Create_Has_FreshAuditFields()
    {
        var before = DateTime.UtcNow;

        var result = PhotoAlbum.Create(
            eventId: Guid.NewGuid(),
            organizerId: Guid.NewGuid(),
            eventTitle: "Test Event",
            name: "Test Album",
            description: "G1.b smoke test album");

        result.IsSuccess.Should().BeTrue(because: result.Error);
        var album = result.Value;
        album.Id.Should().NotBe(Guid.Empty);
        album.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        album.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
        album.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void EventForm_Create_Has_FreshAuditFields()
    {
        var before = DateTime.UtcNow;

        var result = Form.Create(
            eventId: Guid.NewGuid(),
            title: "Test Form",
            description: "G1.b smoke test form");

        result.IsSuccess.Should().BeTrue(because: result.Error);
        var form = result.Value;
        form.Id.Should().NotBe(Guid.Empty);
        form.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        form.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
        form.UpdatedAt.Should().BeNull();
    }
}
