using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.IntegrationTests.Fakes;

namespace LankaConnect.IntegrationTests.Common;

/// <summary>
/// Base class for Web API integration tests using docker-compose PostgreSQL
/// Uses WebApplicationFactory for testing controllers and HTTP endpoints
/// </summary>
public abstract class DockerComposeWebApiTestBase : IAsyncLifetime
{
    // Connection string for docker-compose PostgreSQL
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=LankaConnectDB_Test;Username=lankaconnect;Password=dev_password_123";

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient HttpClient { get; private set; } = null!;
    protected AppDbContext DbContext { get; private set; } = null!;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected IEmailService _emailService { get; private set; } = null!;
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;
    private IServiceScope? _testScope;

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (dbContextDescriptor != null)
                        services.Remove(dbContextDescriptor);

                    var appDbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(AppDbContext));
                    if (appDbContextDescriptor != null)
                        services.Remove(appDbContextDescriptor);

                    // Add test database with docker-compose PostgreSQL
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

                    // Replace IEntraExternalIdService with fake for deterministic testing
                    var entraServiceDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IEntraExternalIdService));
                    if (entraServiceDescriptor != null)
                        services.Remove(entraServiceDescriptor);

                    services.AddSingleton<IEntraExternalIdService, FakeEntraExternalIdService>();
                });

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override connection string for tests
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                        ["ConnectionStrings:Redis"] = "localhost:6379,password=dev_redis_123",
                        ["ASPNETCORE_ENVIRONMENT"] = "Test"
                    });
                });
            });

        HttpClient = Factory.CreateClient();

        // Get DbContext and run migrations
        ServiceProvider = Factory.Services;
        await EnsureDatabaseCreatedAsync();

        // Create a long-lived scope for test operations
        _testScope = ServiceProvider.CreateScope();
        DbContext = _testScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _emailService = _testScope.ServiceProvider.GetRequiredService<IEmailService>();

        // Start transaction for test isolation
        _transaction = await DbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        // Rollback transaction to clean up test data
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }

        _testScope?.Dispose();
        DbContext?.Dispose();
        HttpClient?.Dispose();
        if (Factory != null)
            await Factory.DisposeAsync();
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

    protected async Task<T> ExecuteDbContextAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(context);
    }

    protected async Task ExecuteDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(context);
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
}
