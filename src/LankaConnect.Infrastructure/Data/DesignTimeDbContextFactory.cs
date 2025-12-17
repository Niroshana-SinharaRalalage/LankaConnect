using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace LankaConnect.Infrastructure.Data;

/// <summary>
/// Factory for creating DbContext instances at design-time (for migrations, scaffolding, etc.)
/// Connection String Priority (highest to lowest):
/// 1. Environment variable: ConnectionStrings__DefaultConnection
/// 2. Command-line argument: --connection "connection-string"
/// 3. appsettings.json
/// 4. Fallback to localhost (development only)
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        string? connectionString = null;

        // Priority 1: Check environment variable first (for production migrations)
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("✓ Using connection string from environment variable: ConnectionStrings__DefaultConnection");
        }
        else
        {
            // Priority 2: Check command-line arguments for --connection flag
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--connection" && !string.IsNullOrEmpty(args[i + 1]))
                {
                    connectionString = args[i + 1];
                    Console.WriteLine("✓ Using connection string from command-line argument: --connection");
                    break;
                }
            }
        }

        // Priority 3: If still null, try configuration files
        if (string.IsNullOrEmpty(connectionString))
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../LankaConnect.API"))
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("✓ Using connection string from appsettings.json");
            }
        }

        // Priority 4: Fallback to localhost for local development
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=LankaConnectDB;Username=lankaconnect;Password=dev_password_123;Include Error Detail=true;Pooling=true;MinPoolSize=2;MaxPoolSize=20;ConnectionLifetime=300;CommandTimeout=30";
            Console.WriteLine("⚠ Using fallback connection string for localhost (development)");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("LankaConnect.Infrastructure");
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });

        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();

        // Create mock dependencies for design-time operations (migrations)
        // Actual dependencies are injected by DI at runtime
        var mockPublisher = new NullPublisher();
        var mockLogger = new NullLogger<AppDbContext>();

        return new AppDbContext(optionsBuilder.Options, mockPublisher, mockLogger);
    }

    // Null implementation of IPublisher for design-time operations
    private class NullPublisher : MediatR.IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : MediatR.INotification
        {
            return Task.CompletedTask;
        }
    }

    // Null logger for design-time operations
    private class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}