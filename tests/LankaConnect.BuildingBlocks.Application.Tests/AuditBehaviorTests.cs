using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Application.Behaviors;
using LankaConnect.BuildingBlocks.Application.Tests.Fakes;

namespace LankaConnect.BuildingBlocks.Application.Tests;

public sealed class AuditBehaviorTests
{
    private sealed record SampleCommand(string Payload) : ICommand<string>;

    [Fact]
    public async Task Handle_Success_RecordsSuccessEntry()
    {
        var audit = new FakeAuditLogger();
        var actor = new FakeCurrentActor("user-123");
        var behavior = new AuditBehavior<SampleCommand, string>(audit, actor, NullLog.For<AuditBehavior<SampleCommand, string>>());

        var result = await behavior.Handle(new SampleCommand("x"), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        audit.Entries.Should().HaveCount(1);
        var entry = audit.Entries[0];
        entry.OperationName.Should().Be(nameof(SampleCommand));
        entry.ActorId.Should().Be("user-123");
        entry.Outcome.Should().Be("Success");
        entry.DetailsJson.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Failure_RecordsFailureEntryAndRethrows()
    {
        var audit = new FakeAuditLogger();
        var actor = new FakeCurrentActor("user-123");
        var behavior = new AuditBehavior<SampleCommand, string>(audit, actor, NullLog.For<AuditBehavior<SampleCommand, string>>());
        var inner = new InvalidOperationException("boom");

        Func<Task> act = () => behavior.Handle(new SampleCommand("x"), () => throw inner, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        audit.Entries.Should().HaveCount(1);
        var entry = audit.Entries[0];
        entry.Outcome.Should().Be("Failure");
        entry.DetailsJson.Should().Contain("InvalidOperationException");
        // Exception MESSAGE should never appear in audit details (PII risk)
        entry.DetailsJson.Should().NotContain("boom");
    }

    [Fact]
    public async Task Handle_AnonymousActor_RecordsNullActorId()
    {
        var audit = new FakeAuditLogger();
        var actor = new FakeCurrentActor(null);
        var behavior = new AuditBehavior<SampleCommand, string>(audit, actor, NullLog.For<AuditBehavior<SampleCommand, string>>());

        await behavior.Handle(new SampleCommand("x"), () => Task.FromResult("ok"), CancellationToken.None);

        audit.Entries[0].ActorId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AuditWriteFails_DoesNotPropagate()
    {
        var audit = new FakeAuditLogger { ThrowOnLog = new InvalidOperationException("audit storage down") };
        var actor = new FakeCurrentActor("user-123");
        var behavior = new AuditBehavior<SampleCommand, string>(audit, actor, NullLog.For<AuditBehavior<SampleCommand, string>>());

        // Even though audit logging throws, the user-facing operation must complete.
        var result = await behavior.Handle(new SampleCommand("x"), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_AuditWriteFails_OnFailurePath_StillRethrowsOriginalException()
    {
        var audit = new FakeAuditLogger { ThrowOnLog = new InvalidOperationException("audit storage down") };
        var actor = new FakeCurrentActor("user-123");
        var behavior = new AuditBehavior<SampleCommand, string>(audit, actor, NullLog.For<AuditBehavior<SampleCommand, string>>());
        var inner = new ApplicationException("original");

        Func<Task> act = () => behavior.Handle(new SampleCommand("x"), () => throw inner, CancellationToken.None);

        // The original handler exception propagates; the audit-write failure is swallowed.
        await act.Should().ThrowAsync<ApplicationException>().WithMessage("original");
    }

    [Fact]
    public async Task Handle_NullNext_Throws()
    {
        var behavior = new AuditBehavior<SampleCommand, string>(
            new FakeAuditLogger(),
            new FakeCurrentActor(null),
            NullLog.For<AuditBehavior<SampleCommand, string>>());

        Func<Task> act = () => behavior.Handle(new SampleCommand("x"), null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
