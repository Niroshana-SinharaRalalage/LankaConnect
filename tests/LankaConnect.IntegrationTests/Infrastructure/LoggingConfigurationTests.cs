using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using FluentAssertions;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;
using System.Diagnostics;
using System.Net.Http;

namespace LankaConnect.IntegrationTests.Infrastructure;

[Collection("Logging")]
public class LoggingConfigurationTests : IDisposable
{
    private readonly TestServer _testServer;
    private readonly HttpClient _httpClient;
    private readonly IDisposable _testCorrelatorContext;

    public LoggingConfigurationTests()
    {
        _testCorrelatorContext = TestCorrelator.CreateContext();
        
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Serilog:MinimumLevel:Default"] = "Information",
                ["Serilog:MinimumLevel:Override:Microsoft"] = "Warning",
                ["Serilog:MinimumLevel:Override:System"] = "Warning",
                ["Serilog:WriteTo:0:Name"] = "TestCorrelator",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=testdb;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = "localhost:6379"
            })
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddConfiguration(configuration);
        
        // Configure Serilog
        builder.Host.UseSerilog((context, config) =>
        {
            config
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.TestCorrelator()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "LankaConnect.Tests");
        });

        var app = builder.Build();
        
        // Add some test endpoints
        app.MapGet("/test-log", (ILogger<LoggingConfigurationTests> logger) =>
        {
            logger.LogInformation("Test log message from endpoint");
            return Results.Ok("Logged");
        });

        app.MapGet("/test-error", (ILogger<LoggingConfigurationTests> logger) =>
        {
            logger.LogError("Test error message");
            throw new InvalidOperationException("Test exception");
        });

        _testServer = new TestServer(app.Services.GetRequiredService<IWebHostBuilder>());
        _httpClient = _testServer.CreateClient();
    }

    public void Dispose()
    {
        _testCorrelatorContext?.Dispose();
        _httpClient?.Dispose();
        _testServer?.Dispose();
    }

    [Fact]
    public void SerilogConfiguration_IsSetupCorrectly()
    {
        // Arrange & Act
        var logger = Log.ForContext<LoggingConfigurationTests>();
        
        logger.Information("Test information message");
        logger.Warning("Test warning message");
        logger.Error("Test error message");

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().HaveCountGreaterThan(0);
        
        var infoEvent = logEvents.FirstOrDefault(e => e.Level == LogEventLevel.Information);
        infoEvent.Should().NotBeNull();
        infoEvent!.MessageTemplate.Text.Should().Contain("Test information message");
    }

    [Fact]
    public void LogLevel_Filtering_WorksCorrectly()
    {
        // Arrange & Act
        var logger = Log.ForContext<LoggingConfigurationTests>();
        
        logger.Verbose("This should be filtered out");
        logger.Debug("This should be filtered out");
        logger.Information("This should appear");
        logger.Warning("This should appear");
        logger.Error("This should appear");

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        
        logEvents.Should().NotContain(e => e.Level == LogEventLevel.Verbose);
        logEvents.Should().NotContain(e => e.Level == LogEventLevel.Debug);
        logEvents.Should().Contain(e => e.Level == LogEventLevel.Information);
        logEvents.Should().Contain(e => e.Level == LogEventLevel.Warning);
        logEvents.Should().Contain(e => e.Level == LogEventLevel.Error);
    }

    [Fact]
    public void StructuredLogging_WorksCorrectly()
    {
        // Arrange & Act
        var logger = Log.ForContext<LoggingConfigurationTests>();
        var userId = 12345;
        var action = "UserLogin";
        
        logger.Information("User {UserId} performed {Action} at {Timestamp}",
            userId, action, DateTime.UtcNow);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        var structuredEvent = logEvents.FirstOrDefault(e => 
            e.Properties.ContainsKey("UserId") && 
            e.Properties.ContainsKey("Action"));
        
        structuredEvent.Should().NotBeNull();
        structuredEvent!.Properties["UserId"].ToString().Should().Contain(userId.ToString());
        structuredEvent.Properties["Action"].ToString().Should().Contain(action);
    }

    [Fact]
    public void LogEnrichment_AddsExpectedProperties()
    {
        // Arrange & Act
        var logger = Log.ForContext<LoggingConfigurationTests>();
        logger.Information("Test message with enrichment");

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        var enrichedEvent = logEvents.FirstOrDefault();
        
        enrichedEvent.Should().NotBeNull();
        enrichedEvent!.Properties.Should().ContainKey("Application");
        enrichedEvent.Properties["Application"].ToString().Should().Contain("LankaConnect.Tests");
    }

    [Fact]
    public void ExceptionLogging_CapturesFullDetails()
    {
        // Arrange & Act
        var logger = Log.ForContext<LoggingConfigurationTests>();
        var exception = new InvalidOperationException("Test exception message");
        
        logger.Error(exception, "An error occurred while processing {Operation}", "TestOperation");

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        var errorEvent = logEvents.FirstOrDefault(e => e.Level == LogEventLevel.Error);
        
        errorEvent.Should().NotBeNull();
        errorEvent!.Exception.Should().NotBeNull();
        errorEvent.Exception!.Message.Should().Be("Test exception message");
        errorEvent.Properties.Should().ContainKey("Operation");
    }

    [Fact]
    public async Task RequestLogging_WorksInWebContext()
    {
        // Act
        var response = await _httpClient.GetAsync("/test-log");

        // Assert
        response.Should().BeSuccessful();
        
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        logEvents.Should().Contain(e => e.MessageTemplate.Text.Contains("Test log message from endpoint"));
    }

    [Fact]
    public void LogContext_PreservesScope()
    {
        // Arrange & Act
        using (LogContext.PushProperty("OperationId", "12345"))
        using (LogContext.PushProperty("UserId", "user-123"))
        {
            var logger = Log.ForContext<LoggingConfigurationTests>();
            logger.Information("Operation started");
            logger.Information("Operation completed");
        }

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        var scopedEvents = logEvents.Where(e => 
            e.Properties.ContainsKey("OperationId") && 
            e.Properties.ContainsKey("UserId")).ToList();
        
        scopedEvents.Should().HaveCount(2);
        scopedEvents.Should().AllSatisfy(e =>
        {
            e.Properties["OperationId"].ToString().Should().Contain("12345");
            e.Properties["UserId"].ToString().Should().Contain("user-123");
        });
    }

    [Fact]
    public void PerformanceLogging_MeasuresDuration()
    {
        // Arrange & Act
        var logger = Log.ForContext<LoggingConfigurationTests>();
        
        var stopwatch = Stopwatch.StartNew();
        logger.Information("Starting Test operation");
        Thread.Sleep(100); // Simulate work
        stopwatch.Stop();
        logger.Information("Completed Test operation in {Duration}ms", stopwatch.ElapsedMilliseconds);

        // Assert
        var logEvents = TestCorrelator.GetLogEventsFromCurrentContext().ToList();
        var timedEvent = logEvents.FirstOrDefault(e => 
            e.MessageTemplate.Text.Contains("completed"));
        
        timedEvent.Should().NotBeNull();
        timedEvent!.Properties.Should().ContainKey("Elapsed");
    }
}