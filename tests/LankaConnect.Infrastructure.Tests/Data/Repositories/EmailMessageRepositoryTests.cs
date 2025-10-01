using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.Tests.LankaConnect.Domain.Tests.TestHelpers;
using System.Data.Common;

namespace LankaConnect.Infrastructure.Tests.Data.Repositories;

/// <summary>
/// Comprehensive TDD tests for EmailMessageRepository
/// Demonstrates Red-Green-Refactor cycle with integration testing
/// </summary>
public class EmailMessageRepositoryTests : IDisposable
{
    private DbConnection _connection;
    private AppDbContext _context;
    private EmailMessageRepository _repository;

    public EmailMessageRepositoryTests()
    {
        // Use SQLite in-memory database for fast, isolated tests
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _repository = new EmailMessageRepository(_context);
    }

    private async Task SetUpAsync()
    {
        // Clean database before each test
        await CleanDatabaseAsync();
    }

    private async Task TearDownAsync()
    {
        // Clear change tracker to ensure fresh queries
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context?.Dispose();
        _connection?.Dispose();
    }

    #region TDD Examples - Red-Green-Refactor Cycle

    public class GetQueuedEmailsAsyncTests : EmailMessageRepositoryTests
    {
        [Fact]
        public async Task GetQueuedEmailsAsync_WhenNoEmailsExist_ReturnsEmptyList()
        {
            // Arrange - Empty database (handled by SetUp)

            // Act
            var result = await _repository.GetQueuedEmailsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetQueuedEmailsAsync_WhenNoQueuedEmailsExist_ReturnsEmptyList()
        {
            // Arrange
            var sentEmail = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Sent)
                .Build();
            await AddEntityAsync(sentEmail);

            // Act
            var result = await _repository.GetQueuedEmailsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetQueuedEmailsAsync_WhenQueuedEmailsExist_ReturnsOnlyQueuedEmails()
        {
            // Arrange
            var queuedEmail1 = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithPriority(EmailPriority.Normal)
                .Build();

            var queuedEmail2 = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithPriority(EmailPriority.High)
                .Build();

            var sentEmail = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Sent)
                .Build();

            await AddEntitiesAsync(queuedEmail1, queuedEmail2, sentEmail);

            // Act
            var result = await _repository.GetQueuedEmailsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(e => e.Status.Should().Be(EmailStatus.Queued));
        }

        [Fact]
        public async Task GetQueuedEmailsAsync_WithPriorityOrdering_ReturnsEmailsInCorrectOrder()
        {
            // Arrange
            var lowPriorityEmail = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithPriority(EmailPriority.Low)
                .WithCreatedAt(DateTime.UtcNow.AddMinutes(-30))
                .Build();

            var highPriorityEmail = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithPriority(EmailPriority.High)
                .WithCreatedAt(DateTime.UtcNow.AddMinutes(-10))
                .Build();

            var normalPriorityOlder = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithPriority(EmailPriority.Normal)
                .WithCreatedAt(DateTime.UtcNow.AddMinutes(-20))
                .Build();

            var normalPriorityNewer = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithPriority(EmailPriority.Normal)
                .WithCreatedAt(DateTime.UtcNow.AddMinutes(-5))
                .Build();

            await AddEntitiesAsync(lowPriorityEmail, highPriorityEmail, normalPriorityOlder, normalPriorityNewer);

            // Act
            var result = await _repository.GetQueuedEmailsAsync();

            // Assert
            result.Should().HaveCount(4);
            
            // Should be ordered by Priority first (High, Normal, Low), then by CreatedAt (older first)
            result[0].Should().Be(highPriorityEmail); // High priority
            result[1].Should().Be(normalPriorityOlder); // Normal priority, older
            result[2].Should().Be(normalPriorityNewer); // Normal priority, newer
            result[3].Should().Be(lowPriorityEmail); // Low priority
        }

        [Fact]
        public async Task GetQueuedEmailsAsync_WithBatchSize_ReturnsLimitedResults()
        {
            // Arrange
            var emails = Enumerable.Range(1, 10)
                .Select(i => EmailTestDataBuilder.Create()
                    .WithStatus(EmailStatus.Queued)
                    .Build())
                .ToList();

            await AddEntitiesAsync(emails.ToArray());

            // Act
            var result = await _repository.GetQueuedEmailsAsync(batchSize: 3);

            // Assert
            result.Should().HaveCount(3);
        }
    }

    public class CreateEmailMessageAsyncTests : EmailMessageRepositoryTests
    {
        [Fact]
        public async Task CreateEmailMessageAsync_WithValidEmail_ReturnsSuccessResult()
        {
            // Arrange
            var emailMessage = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithType(EmailType.Verification)
                .Build();

            // Act
            var result = await _repository.CreateEmailMessageAsync(emailMessage);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(emailMessage);

            // Verify persisted to database
            var retrieved = await _repository.GetByIdAsync(emailMessage.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Id.Should().Be(emailMessage.Id);
        }

        [Fact]
        public async Task CreateEmailMessageAsync_WithDuplicateId_ReturnsFailureResult()
        {
            // Arrange
            var emailId = Guid.NewGuid();
            var firstEmail = EmailTestDataBuilder.Create()
                .WithId(emailId)
                .WithStatus(EmailStatus.Queued)
                .Build();

            var duplicateEmail = EmailTestDataBuilder.Create()
                .WithId(emailId) // Same ID
                .WithStatus(EmailStatus.Queued)
                .Build();

            await AddEntityAsync(firstEmail);

            // Act
            var result = await _repository.CreateEmailMessageAsync(duplicateEmail);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("already exists");
        }
    }

    public class MarkAsProcessingAsyncTests : EmailMessageRepositoryTests
    {
        [Fact]
        public async Task MarkAsProcessingAsync_WithQueuedEmails_MarksThemAsSending()
        {
            // Arrange
            var queuedEmail1 = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .Build();

            var queuedEmail2 = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .Build();

            var sentEmail = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Sent)
                .Build();

            await AddEntitiesAsync(queuedEmail1, queuedEmail2, sentEmail);

            var emailIds = new[] { queuedEmail1.Id, queuedEmail2.Id, sentEmail.Id };

            // Act
            var result = await _repository.MarkAsProcessingAsync(emailIds);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(2); // Only queued emails should be marked as sending

            // Verify status changes - should transition from Queued to Sending
            var updated1 = await _repository.GetByIdAsync(queuedEmail1.Id);
            var updated2 = await _repository.GetByIdAsync(queuedEmail2.Id);
            var unchanged = await _repository.GetByIdAsync(sentEmail.Id);

            updated1!.Status.Should().Be(EmailStatus.Sending); // Domain transition: Queued → Sending
            updated2!.Status.Should().Be(EmailStatus.Sending); // Domain transition: Queued → Sending
            unchanged!.Status.Should().Be(EmailStatus.Sent); // Should remain unchanged (invalid transition)
        }

        [Fact]
        public async Task MarkAsProcessingAsync_WithEmptyList_ReturnsZero()
        {
            // Act
            var result = await _repository.MarkAsProcessingAsync(Array.Empty<Guid>());

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
        }

        [Fact]
        public async Task MarkAsProcessingAsync_WithNonExistentIds_ReturnsZero()
        {
            // Arrange
            var nonExistentIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

            // Act
            var result = await _repository.MarkAsProcessingAsync(nonExistentIds);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(0);
        }
    }

    public class GetEmailQueueStatsAsyncTests : EmailMessageRepositoryTests
    {
        [Fact]
        public async Task GetEmailQueueStatsAsync_WithVariousStatuses_ReturnsCorrectCounts()
        {
            // Arrange
            await AddEntityAsync(EmailTestDataBuilder.Create().WithStatus(EmailStatus.Queued).Build());
            await AddEntityAsync(EmailTestDataBuilder.Create().WithStatus(EmailStatus.Queued).Build());
            await AddEntityAsync(EmailTestDataBuilder.Create().WithStatus(EmailStatus.Sent).Build());
            await AddEntityAsync(EmailTestDataBuilder.Create().WithStatus(EmailStatus.Failed).Build());
            await AddEntityAsync(EmailTestDataBuilder.Create().WithStatus(EmailStatus.Sending).Build());

            // Act
            var stats = await _repository.GetEmailQueueStatsAsync();

            // Assert
            stats.Queued.Should().Be(2);
            stats.Sent.Should().Be(1);
            stats.Failed.Should().Be(1);
            stats.Sending.Should().Be(1);
        }

        [Fact]
        public async Task GetEmailQueueStatsAsync_WithNoEmails_ReturnsZeroCounts()
        {
            // Act
            var stats = await _repository.GetEmailQueueStatsAsync();

            // Assert
            stats.Queued.Should().Be(0);
            stats.Sent.Should().Be(0);
            stats.Failed.Should().Be(0);
            stats.Sending.Should().Be(0);
        }
    }

    public class GetStatusCountsAsyncTests : EmailMessageRepositoryTests
    {
        [Fact]
        public async Task GetStatusCountsAsync_WithVariousStatuses_ReturnsCorrectDictionary()
        {
            // Arrange
            await AddEntityAsync(EmailTestDataBuilder.Create().WithStatus(EmailStatus.Queued).Build());
            await AddEntityAsync(EmailTestDataBuilder.Create().WithStatus(EmailStatus.Queued).Build());
            await AddEntityAsync(EmailTestDataBuilder.Create().WithStatus(EmailStatus.Sent).Build());

            // Act
            var result = await _repository.GetStatusCountsAsync();

            // Assert
            result.Should().HaveCount(2);
            result[EmailStatus.Queued].Should().Be(2);
            result[EmailStatus.Sent].Should().Be(1);
        }

        [Fact]
        public async Task GetStatusCountsAsync_WithDateFilter_ReturnsFilteredCounts()
        {
            // Arrange
            var cutoffDate = DateTime.UtcNow.AddDays(-1);
            
            var oldEmail = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithCreatedAt(DateTime.UtcNow.AddDays(-2))
                .Build();

            var newEmail = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithCreatedAt(DateTime.UtcNow.AddHours(-1))
                .Build();

            await AddEntitiesAsync(oldEmail, newEmail);

            // Act
            var result = await _repository.GetStatusCountsAsync(fromDate: cutoffDate);

            // Assert
            result.Should().HaveCount(1);
            result[EmailStatus.Queued].Should().Be(1); // Only the new email
        }
    }

    #endregion

    #region Integration Tests

    public class IntegrationTests : EmailMessageRepositoryTests
    {
        [Fact]
        public async Task EmailMessage_FullLifecycleTest_WorksCorrectly()
        {
            // Arrange - Create email message
            var emailMessage = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .WithType(EmailType.Welcome)
                .WithPriority(EmailPriority.High)
                .Build();

            // Act & Assert - Create
            var createResult = await _repository.CreateEmailMessageAsync(emailMessage);
            createResult.IsSuccess.Should().BeTrue();

            // Act & Assert - Retrieve queued emails
            var queuedEmails = await _repository.GetQueuedEmailsAsync();
            queuedEmails.Should().HaveCount(1);
            queuedEmails.First().Id.Should().Be(emailMessage.Id);

            // Act & Assert - Mark as processing
            var markResult = await _repository.MarkAsProcessingAsync(new[] { emailMessage.Id });
            markResult.IsSuccess.Should().BeTrue();
            markResult.Value.Should().Be(1);

            // Act & Assert - Verify status change
            var updated = await _repository.GetByIdAsync(emailMessage.Id);
            updated!.Status.Should().Be(EmailStatus.Sending);

            // Act & Assert - No longer appears in queued emails
            var queuedAfterProcessing = await _repository.GetQueuedEmailsAsync();
            queuedAfterProcessing.Should().BeEmpty();
        }

        [Fact]
        public async Task EmailMessage_ConcurrentAccess_HandlesGracefully()
        {
            // Arrange
            var emailMessage = EmailTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .Build();

            await AddEntityAsync(emailMessage);

            // Simulate concurrent access with separate contexts
            using var context2 = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options);
            var repository2 = new EmailMessageRepository(context2);

            // Act - Both repositories try to mark the same email as processing
            var task1 = _repository.MarkAsProcessingAsync(new[] { emailMessage.Id });
            var task2 = repository2.MarkAsProcessingAsync(new[] { emailMessage.Id });

            var results = await Task.WhenAll(task1, task2);

            // Assert - At least one should succeed, exactly one should process the email
            var successfulResults = results.Where(r => r.IsSuccess).ToList();
            var totalProcessed = successfulResults.Sum(r => r.Value);

            successfulResults.Should().NotBeEmpty(); // At least one should succeed
            totalProcessed.Should().Be(1); // Exactly one email should be processed
        }
    }

    #endregion

    #region Performance Tests

    public class PerformanceTests : EmailMessageRepositoryTests
    {
        [Fact]
        public async Task GetQueuedEmailsAsync_WithLargeDataset_PerformsEfficiently()
        {
            // Arrange
            const int emailCount = 1000;
            var emails = Enumerable.Range(1, emailCount)
                .Select(i => EmailTestDataBuilder.Create()
                    .WithStatus(i <= 500 ? EmailStatus.Queued : EmailStatus.Sent)
                    .WithPriority(EmailPriority.Normal)
                    .Build())
                .ToArray();

            await AddEntitiesAsync(emails);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _repository.GetQueuedEmailsAsync(batchSize: 50);
            stopwatch.Stop();

            // Assert
            result.Should().HaveCount(50);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should be fast with proper indexing
            
            // Verify correct data
            result.Should().AllSatisfy(e => e.Status.Should().Be(EmailStatus.Queued));
        }
    }

    #endregion

    #region Test Helpers

    private async Task<T> AddEntityAsync<T>(T entity) where T : BaseEntity
    {
        _context.Set<T>().Add(entity);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear(); // Ensure fresh queries
        return entity;
    }

    private async Task AddEntitiesAsync<T>(params T[] entities) where T : BaseEntity
    {
        _context.Set<T>().AddRange(entities);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task CleanDatabaseAsync()
    {
        // Clean all tables
        _context.EmailMessages.RemoveRange(_context.EmailMessages);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    #endregion
}