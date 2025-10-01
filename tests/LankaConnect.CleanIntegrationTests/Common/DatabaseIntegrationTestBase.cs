using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Communications;
using Xunit;
using System.Data.Common;

namespace LankaConnect.CleanIntegrationTests.Common;

/// <summary>
/// Base class for database integration tests with proper EF Core DI setup
/// Implements architect's solution for resolving dependency injection issues
/// </summary>
public abstract class DatabaseIntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _postgreSqlContainer;
    private IServiceScope? _scope;
    private ServiceProvider? _serviceProvider;
    
    // Protected properties for test classes
    protected AppDbContext DbContext { get; private set; } = null!;
    protected IUnitOfWork UnitOfWork { get; private set; } = null!;
    protected IUserRepository UserRepository { get; private set; } = null!;
    protected IEmailTemplateRepository EmailTemplateRepository { get; private set; } = null!;
    protected IEmailMessageRepository EmailMessageRepository { get; private set; } = null!;
    protected IUserEmailPreferencesRepository UserEmailPreferencesRepository { get; private set; } = null!;
    protected IEmailStatusRepository EmailStatusRepository { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        // Set up TestContainers PostgreSQL
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithDatabase("lankaconnect_integration_test")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithCleanUp(true)
            .Build();
            
        await _postgreSqlContainer.StartAsync();

        // Create complete service provider with all EF Core dependencies
        var services = new ServiceCollection();
        
        // Add logging (fixes the "Unable to resolve ILoggerFactory" error)
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise in tests
        });

        // Create configuration for connection string
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgreSqlContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = "", // Empty for tests
                ["ASPNETCORE_ENVIRONMENT"] = "Testing"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);

        // Add Infrastructure services (uses our existing DI registration)
        services.AddInfrastructure(configuration);

        // Build service provider
        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();

        // Get services from DI container (properly resolved with all dependencies)
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        UnitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        UserRepository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
        EmailTemplateRepository = _scope.ServiceProvider.GetRequiredService<IEmailTemplateRepository>();
        EmailMessageRepository = _scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();
        UserEmailPreferencesRepository = _scope.ServiceProvider.GetRequiredService<IUserEmailPreferencesRepository>();
        EmailStatusRepository = _scope.ServiceProvider.GetRequiredService<IEmailStatusRepository>();

        // Ensure database is created (for TestContainers, we use EnsureCreated instead of migrations)
        await DbContext.Database.EnsureCreatedAsync();
    }

    public virtual async Task DisposeAsync()
    {
        // Clean up in reverse order
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }
        
        _scope?.Dispose();
        _serviceProvider?.Dispose();
        
        if (_postgreSqlContainer != null)
        {
            await _postgreSqlContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Cleans all data from database tables for test isolation
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        try
        {
            // Clean in proper order respecting foreign key constraints
            var tables = new[]
            {
                "EmailStatuses",
                "UserEmailPreferences", 
                "EmailMessages",
                "EmailTemplates",
                "Replies",
                "ForumTopics", 
                "Registrations",
                "Events",
                "Businesses",
                "Users"
            };

            foreach (var table in tables)
            {
                try
                {
                    var sql = $"TRUNCATE TABLE \"{table}\" RESTART IDENTITY CASCADE";
                    await DbContext.Database.ExecuteSqlRawAsync(sql);
                }
                catch (Exception)
                {
                    // Table might not exist in this test scenario, ignore
                }
            }
            
            // Clear change tracker
            DbContext.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            // For debugging - in a real scenario you might want to log this
            throw new InvalidOperationException($"Failed to clean database: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a new transaction scope for testing transaction behavior
    /// </summary>
    protected async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
    {
        return await DbContext.Database.BeginTransactionAsync().ConfigureAwait(false);
    }
}