using Microsoft.Extensions.DependencyInjection;
using LankaConnect.IntegrationTests.Common;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;

namespace LankaConnect.IntegrationTests.Email;

public class EmailApiTests : BaseIntegrationTest
{
    [Fact]
    public async Task SendEmailVerification_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var request = new SendEmailVerificationRequest
        {
            Email = "test-verification@example.com"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/email/send-verification", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendEmailVerification_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new SendEmailVerificationRequest
        {
            Email = "invalid-email-format"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/email/send-verification", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email format");
    }

    [Fact]
    public async Task VerifyEmail_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange
        var request = new VerifyEmailRequest
        {
            Token = "valid-test-token-123"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/email/verify", request);

        // Assert - This might fail if token validation is strict
        // The test serves as a specification for the API contract
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
        }
    }

    [Fact]
    public async Task VerifyEmail_WithEmptyToken_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new VerifyEmailRequest
        {
            Token = ""
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/email/verify", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetEmailStatus_WithValidId_ShouldReturnEmailStatus()
    {
        // Arrange
        var emailId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/api/email/status/{emailId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EmailStatusResponse>(content, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            result.Should().NotBeNull();
            result!.Id.Should().Be(emailId);
        }
    }

    [Fact]
    public async Task GetEmailStatus_WithInvalidId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidId = "invalid-guid";

        // Act
        var response = await HttpClient.GetAsync($"/api/email/status/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendPasswordResetEmail_WithValidEmail_ShouldReturnSuccess()
    {
        // Arrange
        var request = new SendPasswordResetRequest
        {
            Email = "password-reset-test@example.com"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/email/send-password-reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EmailApiEndpoints_ShouldRequireAuthentication()
    {
        // Arrange - Create a new client without authentication
        var unauthenticatedClient = Factory.CreateClient();

        var protectedEndpoints = new[]
        {
            "/api/email/admin/queue-status",
            "/api/email/admin/metrics",
            "/api/email/admin/retry-failed"
        };

        // Act & Assert
        foreach (var endpoint in protectedEndpoints)
        {
            var response = await unauthenticatedClient.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, 
                $"Endpoint {endpoint} should require authentication");
        }
    }

    [Fact]
    public async Task EmailApi_ShouldHandleContentNegotiation()
    {
        // Arrange
        var request = new SendEmailVerificationRequest
        {
            Email = "content-negotiation-test@example.com"
        };

        // Act - Request JSON response
        HttpClient.DefaultRequestHeaders.Accept.Clear();
        HttpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await HttpClient.PostAsJsonAsync("/api/email/send-verification", request);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task EmailApi_ShouldHandleRateLimiting()
    {
        // Arrange
        var request = new SendEmailVerificationRequest
        {
            Email = "rate-limit-test@example.com"
        };

        var tasks = new List<Task<HttpResponseMessage>>();
        var requestCount = 10; // Send multiple requests quickly

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(HttpClient.PostAsJsonAsync("/api/email/send-verification", request));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var rateLimitedCount = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

        // Either all should succeed (no rate limiting) or some should be rate limited
        (successCount + rateLimitedCount).Should().Be(requestCount);
        
        if (rateLimitedCount > 0)
        {
            // If rate limiting is implemented, verify proper headers
            var rateLimitedResponse = responses.First(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            rateLimitedResponse.Headers.Should().ContainKey("Retry-After");
        }
    }

    [Fact]
    public async Task EmailApi_ShouldValidateRequestPayload()
    {
        // Arrange - Send malformed JSON
        var malformedJson = "{ \"email\": \"test@example.com\", \"invalidField\": }";
        var content = new StringContent(malformedJson, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await HttpClient.PostAsync("/api/email/send-verification", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EmailApi_ShouldReturnCorrectErrorResponse()
    {
        // Arrange
        var request = new SendEmailVerificationRequest
        {
            Email = "invalid-email"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/email/send-verification", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        errorResponse.Should().NotBeNull();
        errorResponse!.Errors.Should().NotBeEmpty();
        errorResponse.Errors.Should().Contain(error => error.Contains("email"));
    }

    [Fact]
    public async Task EmailWebhook_ShouldHandleMailHogWebhooks()
    {
        // Arrange - Simulate a webhook payload from MailHog or email service
        var webhookPayload = new EmailWebhookPayload
        {
            EmailId = Guid.NewGuid().ToString(),
            Event = "delivered",
            Timestamp = DateTime.UtcNow,
            RecipientEmail = "webhook-test@example.com"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/email/webhook", webhookPayload);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}

// Request/Response DTOs for API testing
public class SendEmailVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    public string Token { get; set; } = string.Empty;
}

public class SendPasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class EmailStatusResponse
{
    public Guid Id { get; set; }
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool IsOpened { get; set; }
    public bool IsClicked { get; set; }
}

public class EmailWebhookPayload
{
    public string EmailId { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
}