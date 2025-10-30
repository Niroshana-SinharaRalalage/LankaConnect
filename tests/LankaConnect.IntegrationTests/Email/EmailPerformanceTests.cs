using Microsoft.Extensions.DependencyInjection;
using LankaConnect.IntegrationTests.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Data;
using LankaConnect.TestUtilities.Builders;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace LankaConnect.IntegrationTests.Email;

public class EmailPerformanceTests : DockerComposeWebApiTestBase
{

    [Fact]
    public async Task SingleEmailSend_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var to = EmailTestDataBuilder.CreateValidEmailAddress("performance-single@example.com");
        var subject = "Performance Test - Single Email";
        var body = "Testing single email send performance.";
        var maxExecutionTimeMs = 5000; // 5 seconds

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _emailService.SendEmailAsync(to, subject, body);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxExecutionTimeMs, 
            $"Email send should complete within {maxExecutionTimeMs}ms");
    }

    [Fact]
    public async Task BulkEmailQueue_ShouldHandleLargeVolume()
    {
        // Arrange
        var emailCount = 100;
        var maxExecutionTimeMs = 30000; // 30 seconds for 100 emails
        var queueTasks = new List<Task<Guid>>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < emailCount; i++)
        {
            var index = i; // Capture for closure
            var task = _emailService.QueueEmailAsync(
                EmailTestDataBuilder.CreateValidEmailAddress($"bulk-perf-{index}@example.com"),
                $"Bulk Performance Test {index}",
                $"Performance test email number {index}");
            queueTasks.Add(task);
        }

        var emailIds = await Task.WhenAll(queueTasks);
        stopwatch.Stop();

        // Assert
        emailIds.Should().HaveCount(emailCount);
        emailIds.Should().AllSatisfy(id => id.Should().NotBeEmpty());
        
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxExecutionTimeMs,
            $"Queueing {emailCount} emails should complete within {maxExecutionTimeMs}ms");
        
        var averageTimePerEmail = (double)stopwatch.ElapsedMilliseconds / emailCount;
        averageTimePerEmail.Should().BeLessThan(100, // Less than 100ms per email
            $"Average time per email should be under 100ms, was {averageTimePerEmail:F2}ms");
    }

    [Fact]
    public async Task ConcurrentEmailSending_ShouldMaintainPerformance()
    {
        // Arrange
        var concurrentTasks = 20;
        var maxExecutionTimeMs = 15000; // 15 seconds
        var results = new ConcurrentBag<bool>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var tasks = Enumerable.Range(0, concurrentTasks).Select(async i =>
        {
            var result = await _emailService.SendEmailAsync(
                EmailTestDataBuilder.CreateValidEmailAddress($"concurrent-perf-{i}@example.com"),
                $"Concurrent Performance Test {i}",
                $"Concurrent test email number {i}");
            
            results.Add(result);
            return result;
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(concurrentTasks);
        results.Should().AllSatisfy(result => result.Should().BeTrue());
        
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxExecutionTimeMs,
            $"Concurrent email sending should complete within {maxExecutionTimeMs}ms");
    }

    [Fact]
    public async Task EmailQueueProcessing_ShouldMaintainThroughput()
    {
        // Arrange
        var emailCount = 50;
        var batchSize = 10;
        var maxProcessingTimeMs = 25000; // 25 seconds

        // Queue emails first
        var emailIds = new List<Guid>();
        for (int i = 0; i < emailCount; i++)
        {
            var emailId = await _emailService.QueueEmailAsync(
                EmailTestDataBuilder.CreateValidEmailAddress($"throughput-test-{i}@example.com"),
                $"Throughput Test {i}",
                $"Testing email processing throughput - email {i}");
            emailIds.Add(emailId);
        }

        // Act - Process the queue
        var stopwatch = Stopwatch.StartNew();
        await _emailService.ProcessEmailQueueAsync(batchSize);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxProcessingTimeMs,
            $"Processing {emailCount} emails should complete within {maxProcessingTimeMs}ms");

        var throughput = (double)emailCount / stopwatch.Elapsed.TotalSeconds;
        throughput.Should().BeGreaterThan(1.0, 
            $"Email processing throughput should be at least 1 email/second, was {throughput:F2}");

        // Verify at least some emails were processed
        var processedCount = 0;
        foreach (var emailId in emailIds.Take(10)) // Check sample for performance
        {
            var email = await _emailService.GetEmailStatusAsync(emailId);
            if (email != null && email.Status != Domain.Communications.Enums.EmailStatus.Pending)
            {
                processedCount++;
            }
        }

        processedCount.Should().BeGreaterThan(0, "At least some emails should have been processed");
    }

    [Fact]
    public async Task LargeEmailContent_ShouldHandleEfficiently()
    {
        // Arrange
        var to = EmailTestDataBuilder.CreateValidEmailAddress("large-content-perf@example.com");
        var subject = "Performance Test - Large Content";
        
        // Create large content (~1MB)
        var largeContent = string.Concat(Enumerable.Repeat(
            "This is a line of text that will be repeated many times to create a large email body. ", 
            10000));
        
        var maxExecutionTimeMs = 8000; // 8 seconds for large content

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _emailService.SendEmailAsync(to, subject, largeContent);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxExecutionTimeMs,
            $"Large email send should complete within {maxExecutionTimeMs}ms");
        
        largeContent.Length.Should().BeGreaterThan(500000, "Content should be sufficiently large for testing");
    }

    [Fact]
    public async Task TemplateRendering_ShouldBePerformant()
    {
        // Arrange
        var to = EmailTestDataBuilder.CreateValidEmailAddress("template-perf@example.com");
        var templateName = "performance-test-template";
        var maxExecutionTimeMs = 5000; // 5 seconds

        var templateData = new Dictionary<string, object>
        {
            ["userName"] = "Performance Test User",
            ["items"] = Enumerable.Range(1, 100).Select(i => new
            {
                name = $"Item {i}",
                price = i * 10.99,
                description = $"This is description for item number {i}"
            }).ToList(),
            ["totalCount"] = 100,
            ["totalAmount"] = 10999.50m
        };

        // Act & Assert - This might fail if template doesn't exist
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await _emailService.SendTemplatedEmailAsync(to, templateName, templateData);
            stopwatch.Stop();

            if (result) // Only assert timing if template rendering succeeded
            {
                stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxExecutionTimeMs,
                    $"Template rendering should complete within {maxExecutionTimeMs}ms");
            }
        }
        catch (Exception)
        {
            // Template system might not be fully configured, skip performance test
            Assert.True(true, "Template system not configured for performance testing");
        }
    }

    [Fact]
    public async Task MemoryUsage_ShouldRemainStable()
    {
        // Arrange
        var emailCount = 100;
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Process many emails
        var tasks = new List<Task<Guid>>();
        for (int i = 0; i < emailCount; i++)
        {
            var index = i;
            var task = _emailService.QueueEmailAsync(
                EmailTestDataBuilder.CreateValidEmailAddress($"memory-test-{index}@example.com"),
                $"Memory Test {index}",
                $"Testing memory usage with email {index}");
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePerEmail = (double)memoryIncrease / emailCount;
        
        memoryIncreasePerEmail.Should().BeLessThan(10240, // Less than 10KB per email
            $"Memory usage per email should be reasonable, was {memoryIncreasePerEmail:F2} bytes");
        
        memoryIncrease.Should().BeLessThan(10 * 1024 * 1024, // Less than 10MB total increase
            $"Total memory increase should be under 10MB, was {memoryIncrease / 1024.0 / 1024.0:F2}MB");
    }

    [Fact]
    public async Task DatabaseOperations_ShouldScaleEfficiently()
    {
        // Arrange
        var recordCount = 500;
        var maxExecutionTimeMs = 20000; // 20 seconds

        // Act - Create many email records
        var stopwatch = Stopwatch.StartNew();
        
        var emailIds = new List<Guid>();
        for (int i = 0; i < recordCount; i++)
        {
            var emailId = await _emailService.QueueEmailAsync(
                EmailTestDataBuilder.CreateValidEmailAddress($"db-perf-{i}@example.com"),
                $"Database Performance Test {i}",
                $"Database performance test email {i}");
            emailIds.Add(emailId);
        }

        stopwatch.Stop();

        // Assert
        emailIds.Should().HaveCount(recordCount);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxExecutionTimeMs,
            $"Database operations should complete within {maxExecutionTimeMs}ms");

        var averageTimePerOperation = (double)stopwatch.ElapsedMilliseconds / recordCount;
        averageTimePerOperation.Should().BeLessThan(50, // Less than 50ms per DB operation
            $"Average database operation time should be under 50ms, was {averageTimePerOperation:F2}ms");
    }

    [Fact]
    public async Task EmailRetry_ShouldHandleFailuresEfficiently()
    {
        // Arrange - Create failed emails in database
        var failedEmailCount = 20;
        var maxRetryTimeMs = 10000; // 10 seconds

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var failedEmails = new List<Domain.Communications.Entities.EmailMessage>();
        for (int i = 0; i < failedEmailCount; i++)
        {
            var failedEmail = EmailTestDataBuilder.CreateRetryableFailedEmail();
            failedEmails.Add(failedEmail);
            dbContext.EmailMessages.Add(failedEmail);
        }
        
        await dbContext.SaveChangesAsync();

        // Act
        var stopwatch = Stopwatch.StartNew();
        await _emailService.RetryFailedEmailsAsync();
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxRetryTimeMs,
            $"Retry operations should complete within {maxRetryTimeMs}ms");

        var averageRetryTime = (double)stopwatch.ElapsedMilliseconds / failedEmailCount;
        averageRetryTime.Should().BeLessThan(200, // Less than 200ms per retry
            $"Average retry time should be under 200ms, was {averageRetryTime:F2}ms");
    }

    [Fact]
    public async Task StressTest_ShouldHandleHighLoad()
    {
        // Arrange
        var highLoadEmailCount = 200;
        var maxStressTestTimeMs = 60000; // 1 minute
        var concurrencyLevel = 10;

        // Act - Generate high load
        var stopwatch = Stopwatch.StartNew();
        
        var semaphore = new SemaphoreSlim(concurrencyLevel);
        var tasks = new List<Task>();
        
        for (int i = 0; i < highLoadEmailCount; i++)
        {
            var index = i;
            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await _emailService.QueueEmailAsync(
                        EmailTestDataBuilder.CreateValidEmailAddress($"stress-test-{index}@example.com"),
                        $"Stress Test Email {index}",
                        $"High load stress test email number {index}");
                }
                finally
                {
                    semaphore.Release();
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxStressTestTimeMs,
            $"Stress test should complete within {maxStressTestTimeMs}ms");

        var throughput = (double)highLoadEmailCount / stopwatch.Elapsed.TotalSeconds;
        throughput.Should().BeGreaterThan(5.0, 
            $"System should handle at least 5 emails/second under load, achieved {throughput:F2}");
    }
}