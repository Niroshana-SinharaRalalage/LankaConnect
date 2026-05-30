using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;

namespace LankaConnect.BuildingBlocks.Contracts.Tests.IntegrationEvents;

/// <summary>
/// These tests pin the SHAPE of the <see cref="IIntegrationEventDispatcher"/>
/// contract — they don't exercise behavior (no concrete implementation lives
/// in Contracts by design). Behavior tests for the AllInOne / Service Bus
/// implementations live alongside those implementations in BuildingBlocks.Infrastructure
/// + the relevant module Application tests.
/// </summary>
public class IntegrationEventDispatcherContractTests
{
    [Fact]
    public void Contract_is_an_interface()
    {
        typeof(IIntegrationEventDispatcher).IsInterface.Should().BeTrue();
    }

    [Fact]
    public void PublishAsync_takes_typed_IntegrationEventBase_not_raw_object()
    {
        var method = typeof(IIntegrationEventDispatcher).GetMethod("PublishAsync");

        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be<IntegrationEventBase>();
        parameters[1].ParameterType.Should().Be<CancellationToken>();
    }

    [Fact]
    public void PublishAsync_returns_Task_not_Task_of_T()
    {
        var method = typeof(IIntegrationEventDispatcher).GetMethod("PublishAsync");

        method!.ReturnType.Should().Be<Task>();
    }

    [Fact]
    public void PublishAsync_cancellation_token_has_default_value()
    {
        var method = typeof(IIntegrationEventDispatcher).GetMethod("PublishAsync");
        var ctParameter = method!.GetParameters()[1];

        ctParameter.HasDefaultValue.Should().BeTrue();
    }

    // -- Fake dispatcher to prove the contract can be implemented -----------

    private sealed record TestEvent(string Name) : IntegrationEventBase, IIntegrationEventV1;

    private sealed class CapturingDispatcher : IIntegrationEventDispatcher
    {
        public List<IntegrationEventBase> Captured { get; } = new();

        public Task PublishAsync(IntegrationEventBase integrationEvent, CancellationToken cancellationToken = default)
        {
            Captured.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Implementing_the_contract_with_a_fake_works_end_to_end()
    {
        IIntegrationEventDispatcher dispatcher = new CapturingDispatcher();
        var ev = new TestEvent("hello");

        await dispatcher.PublishAsync(ev);

        ((CapturingDispatcher)dispatcher).Captured.Should().ContainSingle().Which.Should().BeSameAs(ev);
    }
}
