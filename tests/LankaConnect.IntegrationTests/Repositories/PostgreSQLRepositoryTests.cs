using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.IntegrationTests.Common;
using LankaConnect.TestUtilities.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LankaConnect.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for PostgreSQL database operations using proper EF Core DI setup
/// </summary>
public class PostgreSQLRepositoryTests : DatabaseIntegrationTestBase
{

    [Fact]
    public async Task CreateAndRetrieveUser_AgainstPostgreSQL_ShouldWork()
    {
        // Arrange
        var email = EmailTestDataBuilder.CreateValidEmail($"test-{Guid.NewGuid()}@example.com");
        var user = User.Create(email, "Integration", "Test").Value;
        
        try
        {
            // Act - Create
            await UserRepository.AddAsync(user);
            await UnitOfWork.CommitAsync();

            // Act - Retrieve in separate scope to ensure persistence
            var retrievedUser = await ExecuteInSeparateScope(async serviceProvider =>
            {
                var userRepo = serviceProvider.GetRequiredService<IUserRepository>();
                return await userRepo.GetByEmailAsync(email);
            });

            // Assert
            Assert.NotNull(retrievedUser);
            Assert.Equal(email.Value, retrievedUser.Email.Value);
            Assert.Equal("Integration", retrievedUser.FirstName);
            Assert.Equal("Test", retrievedUser.LastName);
            Assert.True(retrievedUser.IsActive);
            Assert.True(retrievedUser.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        }
        finally
        {
            // Cleanup using clean database method
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task UserRepository_DatabaseConnection_ShouldWork()
    {
        // Act & Assert - Simply test that we can connect and query
        var userCount = await UserRepository.CountAsync();
        
        // Should not throw an exception and should return a non-negative count
        Assert.True(userCount >= 0);
    }

    [Fact]
    public async Task UserRepository_CRUD_Operations_ShouldWork()
    {
        // Arrange
        var email = EmailTestDataBuilder.CreateValidEmail($"crud-test-{Guid.NewGuid()}@example.com");
        var user = User.Create(email, "CRUD", "Test").Value;

        try
        {
            // Act - Create
            await UserRepository.AddAsync(user);
            await UnitOfWork.CommitAsync();
            var userId = user.Id;

            // Act - Read
            var retrievedUser = await UserRepository.GetByIdAsync(userId);
            Assert.NotNull(retrievedUser);
            Assert.Equal("CRUD", retrievedUser.FirstName);

            // Act - Update
            retrievedUser.UpdateProfile("Updated", "Name", null, null);
            await UnitOfWork.CommitAsync();

            // Verify update in separate scope
            var updatedUser = await ExecuteInSeparateScope(async serviceProvider =>
            {
                var userRepo = serviceProvider.GetRequiredService<IUserRepository>();
                return await userRepo.GetByIdAsync(userId);
            });

            Assert.NotNull(updatedUser);
            Assert.Equal("Updated", updatedUser.FirstName);
            Assert.Equal("Name", updatedUser.LastName);

            // Act - Delete (soft delete)
            updatedUser.Deactivate();
            await UnitOfWork.CommitAsync();

            // Verify soft delete
            var deactivatedUser = await UserRepository.GetByIdAsync(userId);
            Assert.NotNull(deactivatedUser);
            Assert.False(deactivatedUser.IsActive);
        }
        finally
        {
            // Cleanup
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task EmailTemplateRepository_BasicOperations_ShouldWork()
    {
        // This test validates that the EmailTemplateRepository DI issue is resolved
        try
        {
            // Act - Simple query to test DI resolution
            var templateCount = await EmailTemplateRepository.CountAsync();
            
            // Should not throw DI exceptions
            Assert.True(templateCount >= 0);
        }
        finally
        {
            await CleanDatabase();
        }
    }

    [Fact]
    public async Task AllRepositories_DependencyInjection_ShouldResolveCorrectly()
    {
        // This test specifically validates that all repositories can be resolved
        // without dependency injection failures
        
        // Assert - All repositories should be properly resolved
        Assert.NotNull(UserRepository);
        Assert.NotNull(BusinessRepository);
        Assert.NotNull(EventRepository);
        Assert.NotNull(RegistrationRepository);
        Assert.NotNull(ForumTopicRepository);
        Assert.NotNull(ReplyRepository);
        Assert.NotNull(EmailTemplateRepository);
        Assert.NotNull(EmailMessageRepository);
        Assert.NotNull(UserEmailPreferencesRepository);
        Assert.NotNull(EmailStatusRepository);
        Assert.NotNull(UnitOfWork);
        
        // Act & Assert - Basic operations should not throw DI exceptions
        var userCount = await UserRepository.CountAsync();
        var businessCount = await BusinessRepository.CountAsync();
        var eventCount = await EventRepository.CountAsync();
        var templateCount = await EmailTemplateRepository.CountAsync();
        var messageCount = await EmailMessageRepository.CountAsync();
        
        Assert.True(userCount >= 0);
        Assert.True(businessCount >= 0);
        Assert.True(eventCount >= 0);
        Assert.True(templateCount >= 0);
        Assert.True(messageCount >= 0);
    }
}