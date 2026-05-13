using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LankaConnect.BuildingBlocks.Infrastructure.Tests;

public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task ProcessBatch_PendingMessages_DispatchedAndMarkedProcessed()
    {
        var (db, dispatcher, provider) = OutboxTestSetup.Build();
        db.OutboxMessages.Add(OutboxMessage.Create("Module.EventA, Module", "{\"x\":1}", DateTime.UtcNow));
        db.OutboxMessages.Add(OutboxMessage.Create("Module.EventB, Module", "{\"x\":2}", DateTime.UtcNow));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var processor = new OutboxProcessor<OutboxTestDbContext>(provider, NullLogger<OutboxProcessor<OutboxTestDbContext>>.Instance);
        await processor.ProcessBatchOnceAsync();

        dispatcher.Dispatched.Should().HaveCount(2);
        dispatcher.Dispatched.Should().Contain(d => d.EventType == "Module.EventA, Module");
        dispatcher.Dispatched.Should().Contain(d => d.EventType == "Module.EventB, Module");

        var processed = await db.OutboxMessages.AsNoTracking().ToListAsync();
        processed.Should().AllSatisfy(m => m.ProcessedAt.Should().NotBeNull());
    }

    [Fact]
    public async Task ProcessBatch_EmptyOutbox_DispatcherNotCalled()
    {
        var (_, dispatcher, provider) = OutboxTestSetup.Build();
        var processor = new OutboxProcessor<OutboxTestDbContext>(provider, NullLogger<OutboxProcessor<OutboxTestDbContext>>.Instance);

        await processor.ProcessBatchOnceAsync();

        dispatcher.Dispatched.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBatch_OnlyProcessesPending_SkipsAlreadyProcessed()
    {
        var (db, dispatcher, provider) = OutboxTestSetup.Build();
        var already = OutboxMessage.Create("Module.OldEvent, Module", "{}", DateTime.UtcNow.AddMinutes(-10));
        already.MarkProcessed(DateTime.UtcNow.AddMinutes(-9));
        var pending = OutboxMessage.Create("Module.NewEvent, Module", "{}", DateTime.UtcNow);
        db.OutboxMessages.AddRange(already, pending);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var processor = new OutboxProcessor<OutboxTestDbContext>(provider, NullLogger<OutboxProcessor<OutboxTestDbContext>>.Instance);
        await processor.ProcessBatchOnceAsync();

        dispatcher.Dispatched.Should().ContainSingle()
            .Which.EventType.Should().Be("Module.NewEvent, Module");
    }

    [Fact]
    public async Task ProcessBatch_DispatcherThrows_RecordsFailureAndKeepsRow()
    {
        var (db, dispatcher, provider) = OutboxTestSetup.Build();
        dispatcher.OnDispatch = (_, _) => throw new InvalidOperationException("boom");
        db.OutboxMessages.Add(OutboxMessage.Create("Module.EventA, Module", "{}", DateTime.UtcNow));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var processor = new OutboxProcessor<OutboxTestDbContext>(provider, NullLogger<OutboxProcessor<OutboxTestDbContext>>.Instance);
        await processor.ProcessBatchOnceAsync();

        var rows = await db.OutboxMessages.AsNoTracking().ToListAsync();
        rows.Should().ContainSingle();
        rows[0].ProcessedAt.Should().BeNull();
        rows[0].RetryCount.Should().Be(1);
        rows[0].LastError.Should().Contain("boom");
    }

    [Fact]
    public async Task ProcessBatch_DispatcherFailsRepeatedly_DeadLettersAfterMaxRetries()
    {
        var (db, dispatcher, provider) = OutboxTestSetup.Build();
        dispatcher.OnDispatch = (_, _) => throw new InvalidOperationException("repeated");
        db.OutboxMessages.Add(OutboxMessage.Create("Module.EventA, Module", "{}", DateTime.UtcNow));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var processor = new OutboxProcessor<OutboxTestDbContext>(provider, NullLogger<OutboxProcessor<OutboxTestDbContext>>.Instance);

        // Drive ticks until the row dead-letters
        for (var tick = 0; tick < OutboxMessage.MaxRetries + 1; tick++)
        {
            await processor.ProcessBatchOnceAsync();
        }

        var stillInOutbox = await db.OutboxMessages.AsNoTracking().ToListAsync();
        var deadLetters = await db.DeadLetterMessages.AsNoTracking().ToListAsync();

        stillInOutbox.Should().BeEmpty();
        deadLetters.Should().HaveCount(1);
        deadLetters[0].EventType.Should().Be("Module.EventA, Module");
        deadLetters[0].RetryCount.Should().Be(OutboxMessage.MaxRetries);
        deadLetters[0].LastError.Should().Contain("repeated");
    }

    [Fact]
    public async Task ProcessBatch_OrderedByOccurredAt_OldestFirst()
    {
        var (db, dispatcher, provider) = OutboxTestSetup.Build();
        var newest = OutboxMessage.Create("Module.New, Module", "{}", DateTime.UtcNow);
        var oldest = OutboxMessage.Create("Module.Old, Module", "{}", DateTime.UtcNow.AddHours(-1));
        var middle = OutboxMessage.Create("Module.Mid, Module", "{}", DateTime.UtcNow.AddMinutes(-30));
        db.OutboxMessages.AddRange(newest, oldest, middle);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var processor = new OutboxProcessor<OutboxTestDbContext>(provider, NullLogger<OutboxProcessor<OutboxTestDbContext>>.Instance);
        await processor.ProcessBatchOnceAsync();

        // Dispatcher should have seen oldest first
        dispatcher.Dispatched.Select(d => d.EventType).Should().Equal(
            "Module.Old, Module",
            "Module.Mid, Module",
            "Module.New, Module");
    }
}
