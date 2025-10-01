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
using Testcontainers.PostgreSql;

namespace LankaConnect.IntegrationTests.Common;

/// <summary>
/// Base class for database integration tests with proper EF Core dependency injection setup
/// </summary>
public abstract class DatabaseIntegrationTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("LankaConnectTestDB")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .WithCleanUp(true)
        .Build();

    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected AppDbContext DbContext { get; private set; } = null!;
    protected IServiceScope TestScope { get; private set; } = null!;

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
        await _postgresContainer.StartAsync();

        // Create proper service collection with all EF Core dependencies
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = "",
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["DatabaseSettings:EnableDetailedErrors"] = "true",
                ["DatabaseSettings:EnableSensitiveDataLogging"] = "true"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);

        // Add EF Core with proper setup using Infrastructure's DI registration
        services.AddInfrastructure(configuration);

        // Override the connection string to use test container
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(_postgresContainer.GetConnectionString());
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
            options.LogTo(Console.WriteLine, LogLevel.Information);
        }, ServiceLifetime.Scoped);

        // Ensure IApplicationDbContext is properly registered
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<AppDbContext>());

        // Build service provider
        ServiceProvider = services.BuildServiceProvider();

        // Create database and run migrations
        using (var scope = ServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        // Create test scope for the duration of tests
        TestScope = ServiceProvider.CreateScope();
        DbContext = TestScope.ServiceProvider.GetRequiredService<AppDbContext>();

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
        TestScope?.Dispose();
        
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
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
    /// Clean database between tests to ensure isolation
    /// </summary>
    protected async Task CleanDatabase()
    {
        // Clean in correct order to respect foreign key constraints
        await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"EmailStatuses\", \"UserEmailPreferences\", \"EmailMessages\", \"EmailTemplates\", \"Replies\", \"ForumTopics\", \"Registrations\", \"Events\", \"Businesses\", \"Users\" RESTART IDENTITY CASCADE");
        await DbContext.SaveChangesAsync();
    }
}