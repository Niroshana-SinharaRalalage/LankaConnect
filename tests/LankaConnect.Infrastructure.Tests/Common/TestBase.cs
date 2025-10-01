using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Infrastructure.Data;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.InMemory;

namespace LankaConnect.Infrastructure.Tests.Common;

/// <summary>
/// Base class for infrastructure tests with shared setup and utilities
/// Provides common test infrastructure including database context management
/// </summary>
public abstract class TestBase : IDisposable
{
    private bool _disposed;

    protected TestBase()
    {
        // Configure Serilog with InMemory sink for testing
        Log.Logger = new LoggerConfiguration()
            .WriteTo.InMemory()
            .CreateLogger();
    }

    /// <summary>
    /// Creates an in-memory SQLite database context for testing
    /// Each test gets a fresh isolated database
    /// </summary>
    protected virtual AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource=:memory:")
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        var context = new AppDbContext(options);
        context.Database.OpenConnection(); // Keep SQLite connection open
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Creates an in-memory EF Core database context (faster but limited SQL features)
    /// </summary>
    protected virtual AppDbContext CreateInMemoryEfContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    /// <summary>
    /// Gets logged messages from Serilog InMemory sink for verification
    /// </summary>
    protected IEnumerable<InMemoryLogEvent> GetLoggedMessages()
    {
        return InMemorySink.Instance.LogEvents;
    }

    /// <summary>
    /// Clears all logged messages
    /// </summary>
    protected void ClearLoggedMessages()
    {
        InMemorySink.Instance.LogEvents.Clear();
    }

    public virtual void Dispose()
    {
        if (!_disposed)
        {
            Log.CloseAndFlush();
            _disposed = true;
        }
        
        GC.SuppressFinalize(this);
    }
}