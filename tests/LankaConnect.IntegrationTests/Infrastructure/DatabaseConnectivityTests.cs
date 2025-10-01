using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;
using FluentAssertions;
using LankaConnect.Infrastructure.Data;
using Testcontainers.PostgreSql;
using System.Data.Common;

namespace LankaConnect.IntegrationTests.Infrastructure;

[Collection("Database")]
public class DatabaseConnectivityTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("LankaConnectTest")
        .WithUsername("testuser")
        .WithPassword("testpass123")
        .WithCleanUp(true)
        .Build();

    private IServiceProvider? _serviceProvider;
    private AppDbContext? _dbContext;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));
        
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null)
            await _dbContext.DisposeAsync();
        
        if (_serviceProvider != null && _serviceProvider is IDisposable disposable)
            disposable.Dispose();
            
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Database_CanConnect_Successfully()
    {
        // Arrange & Act
        var canConnect = await _dbContext!.Database.CanConnectAsync();
        
        // Assert
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task Database_HasCorrectTables_AfterMigration()
    {
        // Act
        var tableNames = new List<string>();
        await using var connection = _dbContext!.Database.GetDbConnection();
        await connection.OpenAsync();
        
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_type = 'BASE TABLE'
            ORDER BY table_name";
        
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tableNames.Add(reader.GetString(0));
        }
        
        // Assert
        tableNames.Should().NotBeEmpty();
        // Add expected table names here as they are created
        // tableNames.Should().Contain("Users");
        // tableNames.Should().Contain("Businesses");
    }

    [Fact]
    public async Task Database_CanExecuteTransaction_Successfully()
    {
        // Act & Assert
        await using var transaction = await _dbContext!.Database.BeginTransactionAsync();
        
        // Perform some database operation (when entities are available)
        // For now, just verify transaction works
        var transactionId = transaction.TransactionId;
        transactionId.Should().NotBeEmpty();
        
        await transaction.CommitAsync();
    }

    [Fact]
    public async Task Database_ConnectionPool_WorksCorrectly()
    {
        // Arrange
        const int connectionCount = 10;
        var connectionTasks = new List<Task<bool>>();

        // Act
        for (int i = 0; i < connectionCount; i++)
        {
            connectionTasks.Add(Task.Run(async () =>
            {
                using var scope = _serviceProvider!.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await dbContext.Database.CanConnectAsync();
            }));
        }

        var results = await Task.WhenAll(connectionTasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().BeTrue());
    }

    [Fact]
    public async Task Database_HandlesConnectionFailure_Gracefully()
    {
        // Arrange
        var invalidConnectionString = "Host=invalid;Database=invalid;Username=invalid;Password=invalid";
        
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(invalidConnectionString));
        
        using var serviceProvider = services.BuildServiceProvider();
        using var invalidDbContext = serviceProvider.GetRequiredService<AppDbContext>();

        // Act & Assert
        await Assert.ThrowsAsync<DbException>(async () =>
            await invalidDbContext.Database.CanConnectAsync());
    }

    [Fact]
    public async Task Database_CommandTimeout_WorksCorrectly()
    {
        // Act
        _dbContext!.Database.SetCommandTimeout(30);
        var canConnect = await _dbContext.Database.CanConnectAsync();
        
        // Assert
        canConnect.Should().BeTrue();
        _dbContext.Database.GetCommandTimeout().Should().Be(30);
    }
}