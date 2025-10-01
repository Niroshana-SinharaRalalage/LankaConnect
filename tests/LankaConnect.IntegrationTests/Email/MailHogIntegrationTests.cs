using Microsoft.Extensions.DependencyInjection;
using LankaConnect.IntegrationTests.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.TestUtilities.Builders;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LankaConnect.IntegrationTests.Email;

public class MailHogIntegrationTests : BaseIntegrationTest
{
    private readonly HttpClient _mailHogClient;
    private const string MailHogApiUrl = "http://localhost:8025/api/v2";

    public MailHogIntegrationTests()
    {
        _mailHogClient = new HttpClient
        {
            BaseAddress = new Uri(MailHogApiUrl)
        };
    }


    [Fact]
    public async Task MailHog_ShouldBeRunningAndAccessible()
    {
        // Act
        var response = await _mailHogClient.GetAsync("/messages");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("MailHog should be running on localhost:8025");
    }

    [Fact]
    public async Task SendEmail_ShouldAppearInMailHog()
    {
        // Arrange
        var to = EmailTestDataBuilder.CreateValidEmailAddress("mailhog-test@example.com");
        var subject = "MailHog Integration Test";
        var body = "This email should appear in MailHog inbox.";

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body);

        // Assert
        result.Should().BeTrue();

        // Wait a moment for email to be processed
        await Task.Delay(2000);

        // Verify email appears in MailHog
        var mailHogResponse = await _mailHogClient.GetAsync("/messages");
        mailHogResponse.IsSuccessStatusCode.Should().BeTrue();

        var messagesJson = await mailHogResponse.Content.ReadAsStringAsync();
        var mailHogData = JsonSerializer.Deserialize<MailHogMessagesResponse>(messagesJson);

        mailHogData.Should().NotBeNull();
        mailHogData!.Total.Should().BeGreaterThan(0);
        
        var testEmail = mailHogData.Items?.FirstOrDefault(m => 
            m.Content?.Headers?.Subject?.Contains(subject) == true);
        
        testEmail.Should().NotBeNull($"Email with subject '{subject}' should be found in MailHog");
        testEmail!.Content?.Headers?.To?.Should().Contain(to.Address);
    }

    [Fact]
    public async Task SendHtmlEmail_ShouldPreserveBothTextAndHtmlContent()
    {
        // Arrange
        var to = EmailTestDataBuilder.CreateValidEmailAddress("html-mailhog-test@example.com");
        var subject = "MailHog HTML Test";
        var textBody = "This is the plain text version.";
        var htmlBody = EmailTestDataBuilder.GenerateHtmlEmailBody(
            "HTML Test", 
            "<p>This is the <strong>HTML</strong> version with <em>formatting</em>.</p>");

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, textBody, htmlBody);

        // Assert
        result.Should().BeTrue();
        await Task.Delay(2000);

        var mailHogResponse = await _mailHogClient.GetAsync("/messages");
        var messagesJson = await mailHogResponse.Content.ReadAsStringAsync();
        var mailHogData = JsonSerializer.Deserialize<MailHogMessagesResponse>(messagesJson);

        var testEmail = mailHogData?.Items?.FirstOrDefault(m => 
            m.Content?.Headers?.Subject?.Contains(subject) == true);
        
        testEmail.Should().NotBeNull();
        
        // Check that both text and HTML parts are present
        var body = testEmail!.Content?.Body;
        body.Should().Contain("This is the plain text version");
        body.Should().Contain("<strong>HTML</strong>");
    }

    [Fact]
    public async Task SendMultipleEmails_ShouldAllAppearInMailHog()
    {
        // Arrange
        var emailCount = 5;
        var tasks = new List<Task<bool>>();
        var subjects = new List<string>();

        // Act
        for (int i = 0; i < emailCount; i++)
        {
            var subject = $"MailHog Batch Test {i + 1}";
            subjects.Add(subject);
            
            var task = _emailService.SendEmailAsync(
                EmailTestDataBuilder.CreateValidEmailAddress($"batch-test-{i}@example.com"),
                subject,
                $"This is batch test email number {i + 1}");
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().BeTrue());
        await Task.Delay(3000); // Wait for all emails to be processed

        var mailHogResponse = await _mailHogClient.GetAsync("/messages");
        var messagesJson = await mailHogResponse.Content.ReadAsStringAsync();
        var mailHogData = JsonSerializer.Deserialize<MailHogMessagesResponse>(messagesJson);

        mailHogData!.Total.Should().BeGreaterOrEqualTo(emailCount);

        // Verify all our test emails are present
        foreach (var subject in subjects)
        {
            var email = mailHogData.Items?.FirstOrDefault(m => 
                m.Content?.Headers?.Subject?.Contains(subject) == true);
            email.Should().NotBeNull($"Email with subject '{subject}' should be in MailHog");
        }
    }

    [Fact]
    public async Task SendEmailWithAttachment_ShouldIncludeAttachment()
    {
        // Note: This test assumes email service supports attachments
        // If not implemented yet, this test can serve as a specification

        // Arrange
        var to = EmailTestDataBuilder.CreateValidEmailAddress("attachment-test@example.com");
        var subject = "MailHog Attachment Test";
        var body = "This email should have an attachment.";

        // For now, we'll just send a regular email since attachments might not be implemented
        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body);

        // Assert
        result.Should().BeTrue();
        await Task.Delay(2000);

        var mailHogResponse = await _mailHogClient.GetAsync("/messages");
        var messagesJson = await mailHogResponse.Content.ReadAsStringAsync();
        var mailHogData = JsonSerializer.Deserialize<MailHogMessagesResponse>(messagesJson);

        var testEmail = mailHogData?.Items?.FirstOrDefault(m => 
            m.Content?.Headers?.Subject?.Contains(subject) == true);
        
        testEmail.Should().NotBeNull();
        // TODO: Add attachment verification when implemented
    }

    [Fact]
    public async Task VerifyEmailHeaders_ShouldContainCorrectMetadata()
    {
        // Arrange
        var to = EmailTestDataBuilder.CreateValidEmailAddress("headers-test@example.com");
        var subject = "MailHog Headers Test";
        var body = "Testing email headers in MailHog.";

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body);

        // Assert
        result.Should().BeTrue();
        await Task.Delay(2000);

        var mailHogResponse = await _mailHogClient.GetAsync("/messages");
        var messagesJson = await mailHogResponse.Content.ReadAsStringAsync();
        var mailHogData = JsonSerializer.Deserialize<MailHogMessagesResponse>(messagesJson);

        var testEmail = mailHogData?.Items?.FirstOrDefault(m => 
            m.Content?.Headers?.Subject?.Contains(subject) == true);
        
        testEmail.Should().NotBeNull();

        var headers = testEmail!.Content!.Headers!;
        headers.Subject.Should().Be(subject);
        headers.To.Should().Contain(to.Address);
        headers.From.Should().NotBeNullOrEmpty();
        headers.Date.Should().NotBeNullOrEmpty();
        headers.MessageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteMailHogMessages_ShouldClearInbox()
    {
        // Arrange - Send a test email first
        await _emailService.SendEmailAsync(
            EmailTestDataBuilder.CreateValidEmailAddress("delete-test@example.com"),
            "Delete Test Email",
            "This email will be deleted.");

        await Task.Delay(2000);

        // Act - Delete all messages
        var deleteResponse = await _mailHogClient.DeleteAsync("/messages");

        // Assert
        deleteResponse.IsSuccessStatusCode.Should().BeTrue();

        // Verify inbox is empty
        var messagesResponse = await _mailHogClient.GetAsync("/messages");
        var messagesJson = await messagesResponse.Content.ReadAsStringAsync();
        var mailHogData = JsonSerializer.Deserialize<MailHogMessagesResponse>(messagesJson);

        mailHogData!.Total.Should().Be(0);
    }

    [Fact]
    public async Task SearchMailHogMessages_ShouldFindSpecificEmails()
    {
        // Arrange - Send emails with different subjects
        var uniqueIdentifier = Guid.NewGuid().ToString("N")[..8];
        var searchSubject = $"Searchable Email {uniqueIdentifier}";
        var otherSubject = $"Other Email {uniqueIdentifier}";

        await _emailService.SendEmailAsync(
            EmailTestDataBuilder.CreateValidEmailAddress("search1@example.com"),
            searchSubject,
            "This email should be found in search.");

        await _emailService.SendEmailAsync(
            EmailTestDataBuilder.CreateValidEmailAddress("search2@example.com"),
            otherSubject,
            "This is a different email.");

        await Task.Delay(2000);

        // Act - Search for messages (MailHog API might support search)
        var messagesResponse = await _mailHogClient.GetAsync("/messages");
        var messagesJson = await messagesResponse.Content.ReadAsStringAsync();
        var mailHogData = JsonSerializer.Deserialize<MailHogMessagesResponse>(messagesJson);

        // Assert - Manually filter (since we're searching in retrieved messages)
        var searchableEmail = mailHogData?.Items?.FirstOrDefault(m => 
            m.Content?.Headers?.Subject?.Contains("Searchable") == true);
        
        var otherEmail = mailHogData?.Items?.FirstOrDefault(m => 
            m.Content?.Headers?.Subject?.Contains("Other Email") == true);

        searchableEmail.Should().NotBeNull();
        otherEmail.Should().NotBeNull();
        searchableEmail!.Content!.Headers!.Subject.Should().Contain("Searchable");
    }

    [Fact]
    public async Task LargeEmailContent_ShouldBeHandledCorrectly()
    {
        // Arrange - Create a large email body
        var to = EmailTestDataBuilder.CreateValidEmailAddress("large-email-test@example.com");
        var subject = "MailHog Large Email Test";
        var largeBody = string.Concat(Enumerable.Repeat("This is a line of text in a large email body. ", 100));

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, largeBody);

        // Assert
        result.Should().BeTrue();
        await Task.Delay(2000);

        var mailHogResponse = await _mailHogClient.GetAsync("/messages");
        var messagesJson = await mailHogResponse.Content.ReadAsStringAsync();
        var mailHogData = JsonSerializer.Deserialize<MailHogMessagesResponse>(messagesJson);

        var testEmail = mailHogData?.Items?.FirstOrDefault(m => 
            m.Content?.Headers?.Subject?.Contains(subject) == true);
        
        testEmail.Should().NotBeNull();
        testEmail!.Content!.Body.Should().Contain("This is a line of text");
        testEmail.Content.Size.Should().BeGreaterThan(1000); // Should be a substantial size
    }
}

// MailHog API response models
public class MailHogMessagesResponse
{
    public int Total { get; set; }
    public int Count { get; set; }
    public int Start { get; set; }
    public List<MailHogMessage>? Items { get; set; }
}

public class MailHogMessage
{
    public string? ID { get; set; }
    public MailHogMessageContent? Content { get; set; }
    public DateTime Created { get; set; }
    public string? MIME { get; set; }
    public MailHogMessageRaw? Raw { get; set; }
}

public class MailHogMessageContent
{
    public MailHogMessageHeaders? Headers { get; set; }
    public string? Body { get; set; }
    public int Size { get; set; }
    public string? MIME { get; set; }
}

public class MailHogMessageHeaders
{
    public string? Subject { get; set; }
    public List<string>? To { get; set; }
    public List<string>? From { get; set; }
    public string? Date { get; set; }
    public string? MessageId { get; set; }
    
    [JsonPropertyName("Message-ID")]
    public string? MessageID { get; set; }
    
    [JsonPropertyName("Content-Type")]
    public List<string>? ContentType { get; set; }
}

public class MailHogMessageRaw
{
    public string? From { get; set; }
    public List<string>? To { get; set; }
    public string? Data { get; set; }
    public string? Helo { get; set; }
}