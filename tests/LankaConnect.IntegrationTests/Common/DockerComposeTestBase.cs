using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Communications;

namespace LankaConnect.IntegrationTests.Common;

/// <summary>
/// Base class for database integration tests using docker-compose PostgreSQL
/// Uses transaction rollback for fast test isolation
/// </summary>
public abstract class DockerComposeTestBase : IAsyncLifetime
{
    // Connection string for docker-compose PostgreSQL
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=LankaConnectDB_Test;Username=lankaconnect;Password=dev_password_123";

    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected AppDbContext DbContext { get; private set; } = null!;
    protected IServiceScope TestScope { get; private set; } = null!;
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;

    // Repository instances for tests
    protected IUserRepository UserRepository { get; private set; } = null!;
    protected IBusinessRepository BusinessRepository { get; private set; } = null!;
    protected IEventRepository EventRepository { get; private set; } = null!;
    protected IRegistrationRepository RegistrationRepository { get; private set; } = null!;
    protected IForumTopicRepository ForumTopicRepository { get; private set; } = null!;
    protected IReplyRepository ReplyRepository { get; private set; } = null!;
    protected IEmailTemplateRepository EmailTemplateRepository { get; private set; } = null!;
    protected IEmailMessageRepository EmailMessageRepository { get; private set; } = null!;
    protected IUserEmailPreferencesRepository UserEmailPreferencesRepository { get; private set; } = null!;
    protected IEmailStatusRepository EmailStatusRepository { get; private set; } = null!;
    protected IUnitOfWork UnitOfWork { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Create proper service collection with all EF Core dependencies
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise
        });

        // Add configuration pointing to docker-compose PostgreSQL
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["ConnectionStrings:Redis"] = "localhost:6379,password=dev_redis_123",
                ["ASPNETCORE_ENVIRONMENT"] = "Test",
                ["DatabaseSettings:EnableDetailedErrors"] = "true",
                ["DatabaseSettings:EnableSensitiveDataLogging"] = "false"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Add Infrastructure services (repositories, DbContext, etc.)
        services.AddInfrastructure(configuration);

        // Remove existing DbContext registrations (which have retry strategy enabled)
        var dbContextDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
        if (dbContextDescriptor != null)
            services.Remove(dbContextDescriptor);

        var appDbContextDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(AppDbContext));
        if (appDbContextDescriptor != null)
            services.Remove(appDbContextDescriptor);

        // Override DbContext to use test connection string without retry strategy
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(TestConnectionString, npgsqlOptions =>
            {
                // Disable retry strategy to support test transactions
                npgsqlOptions.EnableRetryOnFailure(0);
            });
            options.EnableDetailedErrors();
            options.LogTo(Console.WriteLine, LogLevel.Warning);
        }, ServiceLifetime.Scoped);

        // Ensure IApplicationDbContext is properly registered
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        // Build service provider
        ServiceProvider = services.BuildServiceProvider();

        // Ensure database exists and run migrations (only once per test run)
        await EnsureDatabaseCreatedAsync();

        // Create test scope for the duration of tests
        TestScope = ServiceProvider.CreateScope();
        DbContext = TestScope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Start transaction for test isolation
        _transaction = await DbContext.Database.BeginTransactionAsync();

        // Initialize repositories
        UserRepository = TestScope.ServiceProvider.GetRequiredService<IUserRepository>();
        BusinessRepository = TestScope.ServiceProvider.GetRequiredService<IBusinessRepository>();
        EventRepository = TestScope.ServiceProvider.GetRequiredService<IEventRepository>();
        RegistrationRepository = TestScope.ServiceProvider.GetRequiredService<IRegistrationRepository>();
        ForumTopicRepository = TestScope.ServiceProvider.GetRequiredService<IForumTopicRepository>();
        ReplyRepository = TestScope.ServiceProvider.GetRequiredService<IReplyRepository>();
        EmailTemplateRepository = TestScope.ServiceProvider.GetRequiredService<IEmailTemplateRepository>();
        EmailMessageRepository = TestScope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();
        UserEmailPreferencesRepository = TestScope.ServiceProvider.GetRequiredService<IUserEmailPreferencesRepository>();
        EmailStatusRepository = TestScope.ServiceProvider.GetRequiredService<IEmailStatusRepository>();
        UnitOfWork = TestScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    public async Task DisposeAsync()
    {
        // Rollback transaction to clean up test data
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }

        TestScope?.Dispose();

        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Ensure test database exists and migrations are applied
    /// </summary>
    private async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            // Check if database exists and create if needed
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to connect to docker-compose PostgreSQL. " +
                "Ensure docker-compose is running: 'docker-compose up -d postgres'. " +
                $"Connection string: {TestConnectionString}", ex);
        }
    }

    /// <summary>
    /// Execute database operations in a separate scope for isolation
    /// </summary>
    protected async Task<T> ExecuteInSeparateScope<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = ServiceProvider.CreateScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Execute database operations in a separate scope for isolation
    /// </summary>
    protected async Task ExecuteInSeparateScope(Func<IServiceProvider, Task> action)
    {
        using var scope = ServiceProvider.CreateScope();
        await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Commit current transaction and start a new one (for tests that need to verify committed data)
    /// </summary>
    protected async Task CommitAndBeginNewTransaction()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
        }
        _transaction = await DbContext.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Clean database between tests - NOT NEEDED with transaction isolation, but kept for compatibility
    /// With transaction-based testing, data is automatically cleaned up via rollback
    /// </summary>
    protected Task CleanDatabase()
    {
        // No-op: Transaction rollback handles cleanup automatically
        // This method exists only for backward compatibility with existing test code
        return Task.CompletedTask;
    }
}
