using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Users.ValueObjects;

namespace LankaConnect.IntegrationTests;

/// <summary>
/// Minimal test to validate the dependency injection fix for repository tests
/// This specifically tests the scenario that was failing: "Unable to resolve ILoggerFactory"
/// </summary>
public class RepositoryDIValidationTest
{
    [Fact]
    public void ServiceCollection_WithProperEFCoreSetup_ShouldResolveRepositoriesWithoutDIErrors()
    {
        // Arrange - Create proper service collection with all EF Core dependencies
        var services = new ServiceCollection();
        
        // Add logging (this was missing and causing the DI failure)
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = "",
                ["ASPNETCORE_ENVIRONMENT"] = "Test"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);

        // Add EF Core with proper setup using Infrastructure's DI registration
        services.AddInfrastructure(configuration);

        // Override with in-memory database for this test
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDB_{Guid.NewGuid()}");
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }, ServiceLifetime.Scoped);

        // Act & Assert - All these should resolve without "Unable to resolve ILoggerFactory" errors
        using var serviceProvider = services.BuildServiceProvider();
        
        // The original error occurred when trying to resolve repositories
        // These should all work now with proper DI setup
        var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
        var emailTemplateRepository = serviceProvider.GetRequiredService<IEmailTemplateRepository>();
        var emailMessageRepository = serviceProvider.GetRequiredService<IEmailMessageRepository>();
        var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        
        // Assert - All dependencies were resolved successfully
        Assert.NotNull(userRepository);
        Assert.NotNull(emailTemplateRepository);
        Assert.NotNull(emailMessageRepository);
        Assert.NotNull(unitOfWork);
        Assert.NotNull(dbContext);
    }

    [Fact]
    public async Task EmailTemplateRepository_WithProperDISetup_ShouldCreateWithoutDIErrors()
    {
        // This test specifically reproduces the scenario that was failing
        
        // Arrange - Create proper service collection (this is the fix)
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole());
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["ASPNETCORE_ENVIRONMENT"] = "Test"
            })
            .Build();
        
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseInMemoryDatabase($"EmailTemplateTest_{Guid.NewGuid()}");
        }, ServiceLifetime.Scoped);

        using var serviceProvider = services.BuildServiceProvider();
        
        // Act - This was previously failing with DI errors
        using var scope = serviceProvider.CreateScope();
        var emailTemplateRepository = scope.ServiceProvider.GetRequiredService<IEmailTemplateRepository>();
        
        // Simple operation that would trigger the DI resolution issues
        var count = await emailTemplateRepository.CountAsync();
        
        // Assert - No DI exceptions were thrown
        Assert.True(count >= 0);
    }
}