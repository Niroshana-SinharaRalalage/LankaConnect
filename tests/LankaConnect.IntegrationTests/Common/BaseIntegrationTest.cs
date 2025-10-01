using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Application.Common.Interfaces;
using Testcontainers.PostgreSql;

namespace LankaConnect.IntegrationTests.Common;

public abstract class BaseIntegrationTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("LankaConnectTestDB")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .WithCleanUp(true)
        .Build();

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient HttpClient { get; private set; } = null!;
    protected AppDbContext DbContext { get; private set; } = null!;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected IEmailService _emailService { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

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

                    // Add test database with proper EF Core setup
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
                });
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override connection string for tests
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString()
                    });
                });
            });

        HttpClient = Factory.CreateClient();

        // Get DbContext for direct database operations and migrate
        ServiceProvider = Factory.Services;
        using (var scope = ServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
        }
        
        // Create a long-lived scope for test operations
        var testScope = ServiceProvider.CreateScope();
        DbContext = testScope.ServiceProvider.GetRequiredService<AppDbContext>();
        _emailService = testScope.ServiceProvider.GetRequiredService<IEmailService>();
    }

    public async Task DisposeAsync()
    {
        DbContext?.Dispose();
        HttpClient?.Dispose();
        if (Factory != null) await Factory.DisposeAsync();
        await _postgresContainer.StopAsync();
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
}