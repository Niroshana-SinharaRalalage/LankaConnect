using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;
using LankaConnect.Modules.Notifications.Contracts;
using DomainEnum = LankaConnect.Modules.Notifications.Domain.Enums.NotificationType;

namespace LankaConnect.Modules.Notifications.Application.Tests.Contracts;

/// <summary>
/// Pin the shape of the Notifications.Contracts wire-format ABI so a module
/// extraction or domain refactor doesn't silently break consumers.
/// </summary>
public class NotificationsContractsShapeTests
{
    [Fact]
    public void INotificationDispatcher_is_an_interface()
    {
        typeof(INotificationDispatcher).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void INotificationDispatcher_NotifyAsync_takes_primitive_parameters_and_NotificationKind()
    {
        var method = typeof(INotificationDispatcher).GetMethod(nameof(INotificationDispatcher.NotifyAsync));

        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(7);

        parameters[0].ParameterType.Should().Be<Guid>();   // userId
        parameters[1].ParameterType.Should().Be<string>(); // title
        parameters[2].ParameterType.Should().Be<string>(); // message
        parameters[3].ParameterType.Should().Be<NotificationKind>();
        // Nullable reference type annotations collapse to plain `string` at runtime.
        parameters[4].ParameterType.Should().Be<string>(); // relatedEntityId (string?)
        parameters[5].ParameterType.Should().Be<string>(); // relatedEntityType (string?)
        parameters[6].ParameterType.Should().Be<CancellationToken>();

        // Cancellation token defaults
        parameters[6].HasDefaultValue.Should().BeTrue();
    }

    [Fact]
    public void INotificationDispatcher_NotifyAsync_returns_Task_not_Task_of_T()
    {
        var method = typeof(INotificationDispatcher).GetMethod(nameof(INotificationDispatcher.NotifyAsync));
        method!.ReturnType.Should().Be<Task>();
    }

    [Fact]
    public void NotificationKind_mirrors_domain_NotificationType_ordinal_values()
    {
        // Every Contracts NotificationKind member must have an identically-named
        // Domain NotificationType member with the same numeric value. Decoupled
        // types are fine — but the wire format must stay aligned with domain
        // until a deliberate V2 break.
        foreach (NotificationKind k in Enum.GetValues<NotificationKind>())
        {
            var name = k.ToString();
            DomainEnum.TryParse(name, out DomainEnum domainEquivalent)
                .Should().BeTrue($"Domain NotificationType.{name} must exist");
            ((int)k).Should().Be((int)domainEquivalent, $"ordinal mismatch for {name}");
        }
    }

    [Fact]
    public void NotificationCreatedIntegrationEventV1_inherits_IntegrationEventBase_and_marker()
    {
        typeof(IntegrationEventBase).IsAssignableFrom(typeof(NotificationCreatedIntegrationEventV1))
            .Should().BeTrue();
        typeof(IIntegrationEventV1).IsAssignableFrom(typeof(NotificationCreatedIntegrationEventV1))
            .Should().BeTrue();
    }

    [Fact]
    public void NotificationCreatedIntegrationEventV1_carries_primitives_and_kind_only()
    {
        var ev = new NotificationCreatedIntegrationEventV1
        {
            NotificationId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Hello",
            Message = "World",
            Kind = NotificationKind.System,
        };

        ev.NotificationId.Should().NotBe(Guid.Empty);
        ev.UserId.Should().NotBe(Guid.Empty);
        ev.Title.Should().Be("Hello");
        ev.Message.Should().Be("World");
        ev.Kind.Should().Be(NotificationKind.System);
        ev.RelatedEntityId.Should().BeNull();
        ev.RelatedEntityType.Should().BeNull();
        ev.EventId.Should().NotBe(Guid.Empty);
        ev.OccurredOnUtc.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
        ev.Version.Should().Be(1);
    }
}
