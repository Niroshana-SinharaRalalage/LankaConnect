using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace LankaConnect.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Build configuration from appsettings files
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../LankaConnect.API"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Get connection string from configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback connection string matching docker-compose configuration
            connectionString = "Host=localhost;Port=5432;Database=LankaConnectDB;Username=lankaconnect;Password=dev_password_123;Include Error Detail=true;Pooling=true;MinPoolSize=2;MaxPoolSize=20;ConnectionLifetime=300;CommandTimeout=30";
        }

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("LankaConnect.Infrastructure");
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });

        // Enable detailed errors and sensitive data logging for design-time
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();
        
        return new AppDbContext(optionsBuilder.Options);
    }
}