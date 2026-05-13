using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Application.Behaviors;
using LankaConnect.BuildingBlocks.Application.Tests.Fakes;

namespace LankaConnect.BuildingBlocks.Application.Tests;

public sealed class OutboxBehaviorTests
{
    private sealed record SampleCommand : ICommand<int>;
    private sealed record IntegrationEventA(string Detail);
    private sealed record IntegrationEventB(int Number);

    [Fact]
    public async Task Handle_NoBufferedEvents_DoesNotTouchOutbox()
    {
        var buffer = new FakeIntegrationEventBuffer();
        var outbox = new FakeOutbox();
        var behavior = new OutboxBehavior<SampleCommand, int>(buffer, outbox, NullLog.For<OutboxBehavior<SampleCommand, int>>());

        var result = await behavior.Handle(new SampleCommand(), () => Task.FromResult(7), CancellationToken.None);

        result.Should().Be(7);
        outbox.Enqueued.Should().BeEmpty();
        // Buffer is still drained once to check
        buffer.DrainCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_BufferedEvents_EnqueueEachToOutbox()
    {
        var evtA = new IntegrationEventA("first");
        var evtB = new IntegrationEventB(42);
        var buffer = new FakeIntegrationEventBuffer(evtA, evtB);
        var outbox = new FakeOutbox();
        var behavior = new OutboxBehavior<SampleCommand, int>(buffer, outbox, NullLog.For<OutboxBehavior<SampleCommand, int>>());

        await behavior.Handle(new SampleCommand(), () => Task.FromResult(0), CancellationToken.None);

        outbox.Enqueued.Should().HaveCount(2);
        outbox.Enqueued[0].Should().Be(evtA);
        outbox.Enqueued[1].Should().Be(evtB);
    }

    [Fact]
    public async Task Handle_HandlerThrows_DoesNotDrainEvents()
    {
        // If the handler throws, the outbox enqueue MUST NOT happen — the
        // events would otherwise be published despite the state change failing.
        var buffer = new FakeIntegrationEventBuffer(new IntegrationEventA("never"));
        var outbox = new FakeOutbox();
        var behavior = new OutboxBehavior<SampleCommand, int>(buffer, outbox, NullLog.For<OutboxBehavior<SampleCommand, int>>());

        Func<Task> act = () => behavior.Handle(
            new SampleCommand(),
            () => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        outbox.Enqueued.Should().BeEmpty();
        buffer.DrainCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NullNext_Throws()
    {
        var behavior = new OutboxBehavior<SampleCommand, int>(
            new FakeIntegrationEventBuffer(),
            new FakeOutbox(),
            NullLog.For<OutboxBehavior<SampleCommand, int>>());

        Func<Task> act = () => behavior.Handle(new SampleCommand(), null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
