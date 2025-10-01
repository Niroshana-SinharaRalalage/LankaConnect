using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Infrastructure.Data;
using LankaConnect.TestUtilities.Builders;
using Email = LankaConnect.Domain.Users.ValueObjects.Email;

namespace LankaConnect.IntegrationTests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AppDbContext _context;
    private readonly IUserRepository _userRepository;

    public UserRepositoryTests()
    {
        var services = new ServiceCollection();
        
        // Use in-memory database for testing
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        
        services.AddScoped<IUserRepository, LankaConnect.Infrastructure.Data.Repositories.UserRepository>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AppDbContext>();
        _userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var email = EmailTestDataBuilder.CreateValidEmail("test@example.com");
        var user = User.Create(email, "John", "Doe").Value;
        
        await _context.Users.AddAsync(user);
        await _context.CommitAsync();

        // Act
        var result = await _userRepository.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email.Value, result.Email.Value);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var email = EmailTestDataBuilder.CreateValidEmail("nonexistent@example.com");

        // Act
        var result = await _userRepository.GetByEmailAsync(email);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsWithEmailAsync_WhenUserExists_ShouldReturnTrue()
    {
        // Arrange
        var email = EmailTestDataBuilder.CreateValidEmail("exists@example.com");
        var user = User.Create(email, "Jane", "Smith").Value;
        
        await _context.Users.AddAsync(user);
        await _context.CommitAsync();

        // Act
        var result = await _userRepository.ExistsWithEmailAsync(email);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsWithEmailAsync_WhenUserDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var email = EmailTestDataBuilder.CreateValidEmail("notexists@example.com");

        // Act
        var result = await _userRepository.ExistsWithEmailAsync(email);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetActiveUsersAsync_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        var activeUser = User.Create(EmailTestDataBuilder.CreateValidEmail("active@example.com"), "Active", "User").Value;
        var inactiveUser = User.Create(EmailTestDataBuilder.CreateValidEmail("inactive@example.com"), "Inactive", "User").Value;
        inactiveUser.Deactivate();

        await _context.Users.AddRangeAsync(activeUser, inactiveUser);
        await _context.CommitAsync();

        // Act
        var result = await _userRepository.GetActiveUsersAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Active", result.First().FirstName);
    }

    public void Dispose()
    {
        _context.Dispose();
        _serviceProvider.Dispose();
    }
}