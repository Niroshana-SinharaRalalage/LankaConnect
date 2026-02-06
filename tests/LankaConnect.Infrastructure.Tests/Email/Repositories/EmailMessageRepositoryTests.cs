using Microsoft.EntityFrameworkCore;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Tests.LankaConnect.Domain.Tests.TestHelpers;

namespace LankaConnect.Infrastructure.Tests.Email.Repositories;

public class EmailMessageRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<AppDbContext> _dbContextOptions;
    private AppDbContext _context;
    private EmailMessageRepository _repository;

    public EmailMessageRepositoryTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(_dbContextOptions);
        _repository = new EmailMessageRepository(_context);
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_WithValidEmailMessage_ShouldAddToDatabase()
    {
        // Arrange
        var emailMessage = EmailTestDataBuilder.CreateBasicEmailMessage();

        // Act
        await _repository.AddAsync(emailMessage);
        await _context.SaveChangesAsync();

        // Assert
        var savedEmail = await _context.EmailMessages.FirstOrDefaultAsync();
        savedEmail.Should().NotBeNull();
        savedEmail!.Subject.Should().Be(emailMessage.Subject);
        savedEmail.Body.Should().Be(emailMessage.Body);
        savedEmail.Status.Should().Be(emailMessage.Status);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnEmailMessage()
    {
        // Arrange
        var emailMessage = EmailTestDataBuilder.CreateBasicEmailMessage();
        await _repository.AddAsync(emailMessage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(emailMessage.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(emailMessage.Id);
        result.Subject.Should().Be(emailMessage.Subject);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingEmailsAsync_WithPendingEmails_ShouldReturnPendingOnly()
    {
        // Arrange
        var pendingEmails = EmailTestDataBuilder.CreatePendingEmails(3);
        var sentEmail = EmailTestDataBuilder.CreateSentEmail();
        var failedEmail = EmailTestDataBuilder.CreateFailedEmail();

        foreach (var email in pendingEmails)
            await _repository.AddAsync(email);
        
        await _repository.AddAsync(sentEmail);
        await _repository.AddAsync(failedEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingEmailsAsync(10);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(email => 
            email.Status.Should().Be(EmailDeliveryStatus.Pending));
    }

    [Fact]
    public async Task GetPendingEmailsAsync_WithBatchSize_ShouldRespectLimit()
    {
        // Arrange
        var pendingEmails = EmailTestDataBuilder.CreatePendingEmails(10);
        
        foreach (var email in pendingEmails)
            await _repository.AddAsync(email);
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingEmailsAsync(5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPendingEmailsAsync_ShouldOrderByPriorityAndCreatedDate()
    {
        // Arrange
        var lowPriorityEmail = EmailTestDataBuilder.CreateLowPriorityEmail();
        var highPriorityEmail = EmailTestDataBuilder.CreateHighPriorityEmail();
        var normalEmail = EmailTestDataBuilder.CreateBasicEmailMessage();

        // Add in random order
        await _repository.AddAsync(lowPriorityEmail);
        await _repository.AddAsync(normalEmail);
        await _repository.AddAsync(highPriorityEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPendingEmailsAsync(10);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(3);
        resultList[0].Priority.Should().Be(1); // Highest priority first
        resultList[1].Priority.Should().Be(3); // Normal priority
        resultList[2].Priority.Should().Be(5); // Lowest priority last
    }

    [Fact]
    public async Task GetRetryableFailedEmailsAsync_WithRetryableEmails_ShouldReturnOnlyRetryable()
    {
        // Arrange
        var retryableEmail1 = EmailTestDataBuilder.CreateRetryableFailedEmail();
        var retryableEmail2 = EmailTestDataBuilder.CreateRetryableFailedEmail();
        var maxAttemptsEmail = EmailTestDataBuilder.CreateFailedEmailWithMultipleAttempts(5); // Exceeded max attempts
        var futureRetryEmail = EmailTestDataBuilder.CreateFailedEmail("Error", DateTime.UtcNow.AddHours(1)); // Future retry time

        await _repository.AddAsync(retryableEmail1);
        await _repository.AddAsync(retryableEmail2);
        await _repository.AddAsync(maxAttemptsEmail);
        await _repository.AddAsync(futureRetryEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRetryableFailedEmailsAsync(3); // Max 3 attempts

        // Assert
        result.Should().HaveCount(2); // Only the retryable ones
        result.Should().AllSatisfy(email =>
        {
            email.Status.Should().Be(EmailDeliveryStatus.Failed);
            email.AttemptCount.Should().BeLessThan(3);
            email.NextRetryAt.Should().BeBefore(DateTime.UtcNow);
        });
    }

    [Fact]
    public async Task GetEmailsByStatusAsync_WithSpecificStatus_ShouldReturnMatchingEmails()
    {
        // Arrange
        var sentEmails = new List<EmailMessage>
        {
            EmailTestDataBuilder.CreateSentEmail(),
            EmailTestDataBuilder.CreateSentEmail()
        };
        
        var pendingEmail = EmailTestDataBuilder.CreateBasicEmailMessage();
        var deliveredEmail = EmailTestDataBuilder.CreateDeliveredEmail();

        foreach (var email in sentEmails)
            await _repository.AddAsync(email);
        
        await _repository.AddAsync(pendingEmail);
        await _repository.AddAsync(deliveredEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsByStatusAsync(EmailDeliveryStatus.Sent);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(email => 
            email.Status.Should().Be(EmailDeliveryStatus.Sent));
    }

    [Fact]
    public async Task GetEmailsByRecipientAsync_WithSpecificRecipient_ShouldReturnMatchingEmails()
    {
        // Arrange
        var recipient = "test@example.com";
        var emailsForRecipient = new List<EmailMessage>
        {
            EmailTestDataBuilder.CreateBasicEmailMessage(
                EmailTestDataBuilder.CreateValidEmailAddress(recipient)),
            EmailTestDataBuilder.CreateBasicEmailMessage(
                EmailTestDataBuilder.CreateValidEmailAddress(recipient))
        };
        
        var emailForDifferentRecipient = EmailTestDataBuilder.CreateBasicEmailMessage(
            EmailTestDataBuilder.CreateValidEmailAddress("other@example.com"));

        foreach (var email in emailsForRecipient)
            await _repository.AddAsync(email);
        
        await _repository.AddAsync(emailForDifferentRecipient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsByRecipientAsync(recipient);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(email => 
            email.To.Address.Should().Be(recipient));
    }

    [Fact]
    public async Task GetEmailsSentBetweenAsync_WithDateRange_ShouldReturnEmailsInRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-2);
        var endDate = DateTime.UtcNow.AddDays(-1);

        var emailInRange1 = EmailTestDataBuilder.CreateSentEmail();
        var emailInRange2 = EmailTestDataBuilder.CreateSentEmail();
        var emailOutOfRange = EmailTestDataBuilder.CreateSentEmail();

        // Manually set SentAt dates (this would normally be done by the domain method)
        SetEmailSentDate(emailInRange1, startDate.AddHours(1));
        SetEmailSentDate(emailInRange2, endDate.AddHours(-1));
        SetEmailSentDate(emailOutOfRange, DateTime.UtcNow); // Today

        await _repository.AddAsync(emailInRange1);
        await _repository.AddAsync(emailInRange2);
        await _repository.AddAsync(emailOutOfRange);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsSentBetweenAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(email =>
        {
            email.SentAt.Should().BeOnOrAfter(startDate);
            email.SentAt.Should().BeOnOrBefore(endDate);
        });
    }

    [Fact]
    public async Task GetEmailMetricsAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var emails = EmailTestDataBuilder.CreateMixedStatusEmails();
        
        foreach (var email in emails)
            await _repository.AddAsync(email);
        
        await _context.SaveChangesAsync();

        // Act
        var totalCount = await _repository.GetTotalEmailCountAsync();
        var pendingCount = await _repository.GetEmailCountByStatusAsync(EmailDeliveryStatus.Pending);
        var sentCount = await _repository.GetEmailCountByStatusAsync(EmailDeliveryStatus.Sent);
        var deliveredCount = await _repository.GetEmailCountByStatusAsync(EmailDeliveryStatus.Delivered);
        var failedCount = await _repository.GetEmailCountByStatusAsync(EmailDeliveryStatus.Failed);

        // Assert
        totalCount.Should().Be(emails.Count);
        pendingCount.Should().BeGreaterThan(0);
        sentCount.Should().BeGreaterThan(0);
        deliveredCount.Should().BeGreaterThan(0);
        failedCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEmail_ShouldUpdateFields()
    {
        // Arrange
        var emailMessage = EmailTestDataBuilder.CreateBasicEmailMessage();
        await _repository.AddAsync(emailMessage);
        await _context.SaveChangesAsync();

        // Act - Simulate marking as sent
        emailMessage.MarkAsSent("msg-12345");
        await _repository.UpdateAsync(emailMessage);
        await _context.SaveChangesAsync();

        // Assert
        var updatedEmail = await _repository.GetByIdAsync(emailMessage.Id);
        updatedEmail.Should().NotBeNull();
        updatedEmail!.Status.Should().Be(EmailDeliveryStatus.Sent);
        updatedEmail.MessageId.Should().Be("msg-12345");
        updatedEmail.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEmail_ShouldRemoveFromDatabase()
    {
        // Arrange
        var emailMessage = EmailTestDataBuilder.CreateBasicEmailMessage();
        await _repository.AddAsync(emailMessage);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(emailMessage);
        await _context.SaveChangesAsync();

        // Assert
        var deletedEmail = await _repository.GetByIdAsync(emailMessage.Id);
        deletedEmail.Should().BeNull();
    }

    [Fact]
    public async Task GetEmailsWithTemplateAsync_WithSpecificTemplate_ShouldReturnMatchingEmails()
    {
        // Arrange
        var templateName = "welcome-email";
        var templatedEmails = new List<EmailMessage>
        {
            EmailTestDataBuilder.CreateTemplatedEmail(templateName: templateName),
            EmailTestDataBuilder.CreateTemplatedEmail(templateName: templateName)
        };
        
        var differentTemplateEmail = EmailTestDataBuilder.CreateTemplatedEmail(templateName: "template-password-reset");
        var nonTemplatedEmail = EmailTestDataBuilder.CreateBasicEmailMessage();

        foreach (var email in templatedEmails)
            await _repository.AddAsync(email);
        
        await _repository.AddAsync(differentTemplateEmail);
        await _repository.AddAsync(nonTemplatedEmail);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEmailsWithTemplateAsync(templateName);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(email => 
            email.TemplateName.Should().Be(templateName));
    }

    [Fact]
    public async Task GetEmailsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var emails = EmailTestDataBuilder.CreateEmailBatch(10);
        
        foreach (var email in emails)
            await _repository.AddAsync(email);
        
        await _context.SaveChangesAsync();

        // Act
        var firstPage = await _repository.GetEmailsAsync(pageNumber: 1, pageSize: 3);
        var secondPage = await _repository.GetEmailsAsync(pageNumber: 2, pageSize: 3);

        // Assert
        firstPage.Should().HaveCount(3);
        secondPage.Should().HaveCount(3);
        
        // Ensure no overlap between pages
        var firstPageIds = firstPage.Select(e => e.Id);
        var secondPageIds = secondPage.Select(e => e.Id);
        firstPageIds.Should().NotIntersectWith(secondPageIds);
    }

    // Helper method to set SentAt date for testing purposes
    private static void SetEmailSentDate(EmailMessage email, DateTime sentAt)
    {
        // This would normally be done through the domain method
        // For testing, we need to access the private setter
        // In a real implementation, you might need to use reflection or add a test-only method
        email.MarkAsSent($"msg-{Guid.NewGuid():N}");
        
        // Use reflection to set the SentAt property for testing
        var sentAtProperty = typeof(EmailMessage).GetProperty("SentAt");
        if (sentAtProperty != null && sentAtProperty.CanWrite)
        {
            sentAtProperty.SetValue(email, sentAt);
        }
    }
}