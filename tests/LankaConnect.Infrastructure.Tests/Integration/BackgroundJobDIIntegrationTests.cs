using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;
using LankaConnect.Infrastructure;
using LankaConnect.Application;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Application.Communications.BackgroundJobs;

namespace LankaConnect.Infrastructure.Tests.Integration;

/// <summary>
/// Phase 6A.61 RCA: Integration tests to verify background jobs are properly registered in DI container
///
/// WHY THIS TEST EXISTS:
/// EventNotificationEmailJob was NOT registered in DI, causing Hangfire to fail silently when
/// attempting to instantiate the job. This test ensures all background jobs can be resolved.
///
/// WHAT IT TESTS:
/// - All Hangfire background jobs are registered in DI container
/// - Jobs can be instantiated by service provider (Hangfire's mechanism)
/// - Dependencies are properly wired
/// </summary>
public class BackgroundJobDIIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;

    public BackgroundJobDIIntegrationTests()
    {
        var services = new ServiceCollection();

        // Build minimal configuration for DI setup
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = "localhost:6379",
                ["EmailSettings:Provider"] = "Azure",
                ["EmailSettings:AzureConnectionString"] = "test-connection-string",
                ["EmailSettings:AzureSenderAddress"] = "test@test.com",
                ["EmailSettings:FromEmail"] = "test@test.com",
                ["EmailSettings:FromName"] = "Test",
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            })
            .Build();

        // Register infrastructure and application services (same as production)
        services.AddInfrastructure(configuration);
        services.AddApplication();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void EventNotificationEmailJob_ShouldBeRegisteredInDI()
    {
        // Act - Attempt to resolve the job (this is what Hangfire does)
        var job = _serviceProvider.GetRequiredService<EventNotificationEmailJob>();

        // Assert
        job.Should().NotBeNull("EventNotificationEmailJob must be registered in DI for Hangfire to execute it");
    }

    [Fact]
    public void NewsletterEmailJob_ShouldBeRegisteredInDI()
    {
        // Act
        var job = _serviceProvider.GetRequiredService<NewsletterEmailJob>();

        // Assert
        job.Should().NotBeNull("NewsletterEmailJob must be registered in DI for Hangfire to execute it");
    }

    [Fact]
    public void AllBackgroundJobs_ShouldBeResolvable()
    {
        // Arrange - List of all background job types
        var backgroundJobTypes = new[]
        {
            typeof(EventNotificationEmailJob),
            typeof(NewsletterEmailJob)
        };

        // Act & Assert - All jobs must be resolvable
        foreach (var jobType in backgroundJobTypes)
        {
            var job = _serviceProvider.GetRequiredService(jobType);
            job.Should().NotBeNull($"{jobType.Name} must be registered in DI container");
        }
    }
}
