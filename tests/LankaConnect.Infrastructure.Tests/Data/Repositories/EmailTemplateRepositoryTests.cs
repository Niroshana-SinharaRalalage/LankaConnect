using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Infrastructure.Tests.Data.Repositories;

/// <summary>
/// Comprehensive test suite for EmailTemplateRepository
/// Follows TDD principles with AAA (Arrange-Act-Assert) pattern
/// Tests all repository methods including error conditions and edge cases
/// </summary>
public class EmailTemplateRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly EmailTemplateRepository _repository;

    public EmailTemplateRepositoryTests()
    {
        // Arrange: Set up in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new AppDbContext(options);
        _repository = new EmailTemplateRepository(_context);

        // Ensure database is created
        _context.Database.EnsureCreated();
    }

    #region GetTemplatesAsync Tests

    [Fact]
    public async Task GetTemplatesAsync_WithNoFilters_ReturnsAllTemplates()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        
        // Act
        var result = await _repository.GetTemplatesAsync();
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(templates.Count);
        result.Should().BeInAscendingOrder(t => t.Category.Value)
               .And.ThenBeInAscendingOrder(t => t.Name);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithCategoryFilter_ReturnsFilteredTemplates()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var transactionalCategory = EmailTemplateCategory.Create("Transactional").Value;
        var expectedCount = templates.Count(t => t.Category.Value == transactionalCategory.Value);
        
        // Act
        var result = await _repository.GetTemplatesAsync(category: transactionalCategory);
        
        // Assert
        result.Should().HaveCount(expectedCount);
        result.Should().AllSatisfy(t => t.Category.Value.Should().Be(transactionalCategory.Value));
    }

    [Fact]
    public async Task GetTemplatesAsync_WithEmailTypeFilter_ReturnsFilteredTemplates()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var emailType = EmailType.Welcome;
        var expectedCount = templates.Count(t => t.Type == emailType);
        
        // Act
        var result = await _repository.GetTemplatesAsync(emailType: emailType);
        
        // Assert
        result.Should().HaveCount(expectedCount);
        result.Should().AllSatisfy(t => t.Type.Should().Be(emailType));
    }

    [Fact]
    public async Task GetTemplatesAsync_WithActiveFilter_ReturnsOnlyActiveTemplates()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var expectedCount = templates.Count(t => t.IsActive);
        
        // Act
        var result = await _repository.GetTemplatesAsync(isActive: true);
        
        // Assert
        result.Should().HaveCount(expectedCount);
        result.Should().AllSatisfy(t => t.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetTemplatesAsync_WithSearchTerm_ReturnsMatchingTemplates()
    {
        // Arrange
        await CreateTestTemplatesAsync();
        var searchTerm = "welcome";
        
        // Act
        var result = await _repository.GetTemplatesAsync(searchTerm: searchTerm);
        
        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(t => 
            t.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            t.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetTemplatesAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        await CreateTestTemplatesAsync();
        var pageNumber = 2;
        var pageSize = 2;
        
        // Act
        var result = await _repository.GetTemplatesAsync(pageNumber: pageNumber, pageSize: pageSize);
        
        // Assert
        result.Should().HaveCountLessOrEqualTo(pageSize);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithCombinedFilters_ReturnsCorrectResults()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var transactionalCategory = EmailTemplateCategory.Create("Transactional").Value;
        var emailType = EmailType.Welcome;
        
        // Act
        var result = await _repository.GetTemplatesAsync(
            category: transactionalCategory,
            emailType: emailType,
            isActive: true);
        
        // Assert
        result.Should().OnlyContain(t => 
            t.Category.Value == transactionalCategory.Value &&
            t.Type == emailType &&
            t.IsActive);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithInvalidSearchTerm_ReturnsEmptyList()
    {
        // Arrange
        await CreateTestTemplatesAsync();
        var invalidSearchTerm = "nonexistent-template-xyz123";
        
        // Act
        var result = await _repository.GetTemplatesAsync(searchTerm: invalidSearchTerm);
        
        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetTemplatesCountAsync Tests

    [Fact]
    public async Task GetTemplatesCountAsync_WithNoFilters_ReturnsCorrectCount()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        
        // Act
        var count = await _repository.GetTemplatesCountAsync();
        
        // Assert
        count.Should().Be(templates.Count);
    }

    [Fact]
    public async Task GetTemplatesCountAsync_WithFilters_ReturnsFilteredCount()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var activeCount = templates.Count(t => t.IsActive);
        
        // Act
        var count = await _repository.GetTemplatesCountAsync(isActive: true);
        
        // Assert
        count.Should().Be(activeCount);
    }

    #endregion

    #region GetCategoryCountsAsync Tests

    [Fact]
    public async Task GetCategoryCountsAsync_WithNoFilters_ReturnsAllCategories()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var expectedCategories = templates
            .GroupBy(t => t.Category.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Act
        var result = await _repository.GetCategoryCountsAsync();
        
        // Assert
        result.Should().HaveCount(expectedCategories.Count);
        foreach (var kvp in result)
        {
            expectedCategories.Should().ContainKey(kvp.Key.Value);
            expectedCategories[kvp.Key.Value].Should().Be(kvp.Value);
        }
    }

    [Fact]
    public async Task GetCategoryCountsAsync_WithActiveFilter_ReturnsOnlyActiveCounts()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var activeTemplates = templates.Where(t => t.IsActive);
        var expectedCategories = activeTemplates
            .GroupBy(t => t.Category.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Act
        var result = await _repository.GetCategoryCountsAsync(isActive: true);
        
        // Assert
        result.Should().HaveCount(expectedCategories.Count);
    }

    #endregion

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_WithValidName_ReturnsTemplate()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var templateName = templates.First().Name;
        
        // Act
        var result = await _repository.GetByNameAsync(templateName);
        
        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(templateName);
    }

    [Fact]
    public async Task GetByNameAsync_WithInvalidName_ReturnsNull()
    {
        // Arrange
        await CreateTestTemplatesAsync();
        var invalidName = "nonexistent-template";
        
        // Act
        var result = await _repository.GetByNameAsync(invalidName);
        
        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetByNameAsync_WithInvalidInput_ReturnsNull(string? invalidName)
    {
        // Arrange
        await CreateTestTemplatesAsync();
        
        // Act
        var result = await _repository.GetByNameAsync(invalidName!);
        
        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByEmailTypeAsync Tests

    [Fact]
    public async Task GetByEmailTypeAsync_WithValidType_ReturnsMatchingTemplates()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var emailType = EmailType.Welcome;
        var expectedCount = templates.Count(t => t.Type == emailType);
        
        // Act
        var result = await _repository.GetByEmailTypeAsync(emailType);
        
        // Assert
        result.Should().HaveCount(expectedCount);
        result.Should().AllSatisfy(t => t.Type.Should().Be(emailType));
        result.Should().BeInAscendingOrder(t => t.Name);
    }

    [Fact]
    public async Task GetByEmailTypeAsync_WithActiveFilter_ReturnsOnlyActive()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var emailType = EmailType.EmailVerification;
        var expectedCount = templates.Count(t => t.Type == emailType && t.IsActive);
        
        // Act
        var result = await _repository.GetByEmailTypeAsync(emailType, isActive: true);
        
        // Assert
        result.Should().HaveCount(expectedCount);
        result.Should().AllSatisfy(t => 
        {
            t.Type.Should().Be(emailType);
            t.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task GetByEmailTypeAsync_WithNonexistentType_ReturnsEmptyList()
    {
        // Arrange
        await CreateTestTemplatesAsync();
        var unusedType = EmailType.Marketing; // Assuming no marketing templates in test data
        
        // Act
        var result = await _repository.GetByEmailTypeAsync(unusedType);
        
        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidTemplate_UpdatesSuccessfully()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var template = templates.First();
        var originalVersion = template.Version;
        var newDescription = "Updated description";
        
        // Act
        template.UpdateDescription(newDescription);
        await _repository.UpdateAsync(template);
        
        // Assert
        var updatedTemplate = await _repository.GetByIdAsync(template.Id);
        updatedTemplate.Should().NotBeNull();
        updatedTemplate!.Description.Should().Be(newDescription);
        updatedTemplate.Version.Should().BeGreaterThan(originalVersion);
        updatedTemplate.UpdatedAt.Should().BeAfter(template.CreatedAt);
    }

    #endregion

    #region Inherited Repository Tests

    [Fact]
    public async Task AddAsync_WithValidTemplate_AddsSuccessfully()
    {
        // Arrange
        var template = CreateValidEmailTemplate("test-template");
        
        // Act
        await _repository.AddAsync(template);
        await _context.SaveChangesAsync();
        
        // Assert
        var addedTemplate = await _repository.GetByIdAsync(template.Id);
        addedTemplate.Should().NotBeNull();
        addedTemplate!.Id.Should().Be(template.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsTemplate()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var templateId = templates.First().Id;
        
        // Act
        var result = await _repository.GetByIdAsync(templateId);
        
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(templateId);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        await CreateTestTemplatesAsync();
        var invalidId = Guid.NewGuid();
        
        // Act
        var result = await _repository.GetByIdAsync(invalidId);
        
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithValidTemplate_DeletesSuccessfully()
    {
        // Arrange
        var templates = await CreateTestTemplatesAsync();
        var template = templates.First();
        
        // Act
        _repository.Delete(template);
        await _context.SaveChangesAsync();
        
        // Assert
        var deletedTemplate = await _repository.GetByIdAsync(template.Id);
        deletedTemplate.Should().BeNull();
    }

    #endregion

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task GetTemplatesAsync_WithCancelledToken_ThrowsOperationCancelledException()
    {
        // Arrange
        await CreateTestTemplatesAsync();
        var cancellationToken = new CancellationToken(true);
        
        // Act & Assert
        await FluentActions
            .Invoking(() => _repository.GetTemplatesAsync(cancellationToken: cancellationToken))
            .Should()
            .ThrowAsync<OperationCancelledException>();
    }

    [Fact]
    public async Task GetTemplatesAsync_WithZeroPageSize_ReturnsEmptyList()
    {
        // Arrange
        await CreateTestTemplatesAsync();
        
        // Act
        var result = await _repository.GetTemplatesAsync(pageSize: 0);
        
        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTemplatesAsync_WithNegativePageNumber_ReturnsEmptyList()
    {
        // Arrange
        await CreateTestTemplatesAsync();
        
        // Act
        var result = await _repository.GetTemplatesAsync(pageNumber: -1);
        
        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task GetTemplatesAsync_WithLargeDataset_PerformsWithinReasonableTime()
    {
        // Arrange
        var largeTemplateSet = await CreateLargeTestDatasetAsync(100);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act
        var result = await _repository.GetTemplatesAsync(pageSize: 20);
        
        // Assert
        stopwatch.Stop();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
        result.Should().HaveCount(20);
    }

    #endregion

    #region Private Helper Methods

    private async Task<List<EmailTemplate>> CreateTestTemplatesAsync()
    {
        var templates = new List<EmailTemplate>
        {
            CreateValidEmailTemplate("welcome-email", EmailType.Welcome, "Transactional", true),
            CreateValidEmailTemplate("password-reset", EmailType.PasswordReset, "Transactional", true),
            CreateValidEmailTemplate("email-verification", EmailType.EmailVerification, "Transactional", false),
            CreateValidEmailTemplate("business-notification", EmailType.BusinessNotification, "Marketing", true),
            CreateValidEmailTemplate("welcome-premium", EmailType.Welcome, "Marketing", false)
        };

        foreach (var template in templates)
        {
            await _repository.AddAsync(template);
        }
        
        await _context.SaveChangesAsync();
        return templates;
    }

    private async Task<List<EmailTemplate>> CreateLargeTestDatasetAsync(int count)
    {
        var templates = new List<EmailTemplate>();
        var types = new[] { EmailType.Welcome, EmailType.PasswordReset, EmailType.EmailVerification };
        var categories = new[] { "Transactional", "Marketing", "System" };

        for (int i = 0; i < count; i++)
        {
            var type = types[i % types.Length];
            var category = categories[i % categories.Length];
            var isActive = i % 2 == 0;
            
            templates.Add(CreateValidEmailTemplate($"template-{i}", type, category, isActive));
        }

        foreach (var template in templates)
        {
            await _repository.AddAsync(template);
        }
        
        await _context.SaveChangesAsync();
        return templates;
    }

    private static EmailTemplate CreateValidEmailTemplate(
        string name, 
        EmailType type = EmailType.Transactional,
        string categoryValue = "Transactional",
        bool isActive = true)
    {
        var category = EmailTemplateCategory.Create(categoryValue).Value;
        var content = TemplateContent.Create(
            subject: $"Subject for {name}",
            htmlBody: $"<h1>HTML Body for {name}</h1>",
            plainTextBody: $"Plain text body for {name}"
        ).Value;

        var templateResult = EmailTemplate.Create(
            name: name,
            description: $"Description for {name}",
            category: category,
            type: type,
            content: content,
            isActive: isActive
        );

        return templateResult.Value;
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        _context.Dispose();
    }

    #endregion
}