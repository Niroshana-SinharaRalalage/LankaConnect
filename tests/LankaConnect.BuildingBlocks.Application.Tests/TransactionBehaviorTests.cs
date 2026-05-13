using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.BuildingBlocks.Application.Behaviors;
using LankaConnect.BuildingBlocks.Application.Tests.Fakes;

namespace LankaConnect.BuildingBlocks.Application.Tests;

public sealed class TransactionBehaviorTests
{
    private sealed record SampleCommand(int X) : ICommand<int>;

    [Fact]
    public async Task Handle_SuccessfulHandler_BeginsAndCommits()
    {
        var uow = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<SampleCommand, int>(uow, NullLog.For<TransactionBehavior<SampleCommand, int>>());

        var result = await behavior.Handle(new SampleCommand(7), () => Task.FromResult(42), CancellationToken.None);

        result.Should().Be(42);
        uow.Calls.Should().Equal("Begin", "Commit");
    }

    [Fact]
    public async Task Handle_HandlerThrows_RollsBackAndRethrows()
    {
        var uow = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<SampleCommand, int>(uow, NullLog.For<TransactionBehavior<SampleCommand, int>>());
        var inner = new InvalidOperationException("boom");

        Func<Task> act = () => behavior.Handle(new SampleCommand(7), () => throw inner, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        uow.Calls.Should().Equal("Begin", "Rollback");
    }

    [Fact]
    public async Task Handle_RollbackAlsoThrows_RethrowsOriginalHandlerException()
    {
        var uow = new FakeUnitOfWork { ThrowOnRollback = new InvalidOperationException("rollback failed") };
        var behavior = new TransactionBehavior<SampleCommand, int>(uow, NullLog.For<TransactionBehavior<SampleCommand, int>>());
        var inner = new ApplicationException("original cause");

        Func<Task> act = () => behavior.Handle(new SampleCommand(7), () => throw inner, CancellationToken.None);

        // The original handler exception must propagate; the rollback failure is logged but swallowed.
        await act.Should().ThrowAsync<ApplicationException>().WithMessage("original cause");
        uow.Calls.Should().Equal("Begin", "Rollback");
    }

    [Fact]
    public async Task Handle_NullNext_Throws()
    {
        var uow = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<SampleCommand, int>(uow, NullLog.For<TransactionBehavior<SampleCommand, int>>());

        Func<Task> act = () => behavior.Handle(new SampleCommand(7), null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
