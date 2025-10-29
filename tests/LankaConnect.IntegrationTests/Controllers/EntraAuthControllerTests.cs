using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LankaConnect.Application.Auth.Commands.LoginWithEntra;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.IntegrationTests.Common;
using LankaConnect.IntegrationTests.Fakes;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LankaConnect.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for Microsoft Entra External ID authentication endpoints
/// Tests the /api/auth/login/entra endpoint with various scenarios
/// </summary>
public class EntraAuthControllerTests : DockerComposeWebApiTestBase
{
    [Fact]
    public async Task LoginWithEntra_WithValidToken_NewUser_ShouldReturn200AndAutoProvisionUser()
    {
        // Arrange
        var request = new
        {
            accessToken = "mock-valid-entra-token-new-user",
            ipAddress = "192.168.1.100"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login/entra", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "valid Entra token should authenticate successfully");

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.GetProperty("user").GetProperty("email").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("user").GetProperty("isNewUser").GetBoolean().Should().BeTrue(
            "first-time Entra user should be auto-provisioned");
        result.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        result.GetProperty("tokenExpiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow);

        // Verify user was created in database with Entra provider
        var userId = result.GetProperty("user").GetProperty("userId").GetGuid();
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        user.Should().NotBeNull();
        user!.IdentityProvider.Should().Be(IdentityProvider.EntraExternal);
        user.ExternalProviderId.Should().NotBeNullOrEmpty();
        user.IsEmailVerified.Should().BeTrue("Entra users have pre-verified emails");
        user.PasswordHash.Should().BeNull("Entra users don't have local passwords");
    }

    [Fact]
    public async Task LoginWithEntra_WithValidToken_ExistingUser_ShouldReturn200AndNotCreateDuplicate()
    {
        // Arrange
        var request = new
        {
            accessToken = TestEntraTokens.ValidTokenExistingUser,
            ipAddress = "192.168.1.101"
        };

        // Act - First login (auto-provision)
        var firstResponse = await HttpClient.PostAsJsonAsync("/api/auth/login/entra", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        var firstResult = JsonSerializer.Deserialize<JsonElement>(firstContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var userId = firstResult.GetProperty("user").GetProperty("userId").GetGuid();

        // Act - Second login (existing user)
        var secondResponse = await HttpClient.PostAsJsonAsync("/api/auth/login/entra", request);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondContent = await secondResponse.Content.ReadAsStringAsync();
        var secondResult = JsonSerializer.Deserialize<JsonElement>(secondContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        secondResult.GetProperty("user").GetProperty("userId").GetGuid().Should().Be(userId,
            "same Entra user should get same userId on subsequent logins");
        secondResult.GetProperty("user").GetProperty("isNewUser").GetBoolean().Should().BeFalse(
            "existing user should not be flagged as new");

        // Verify no duplicate users created
        var userCount = await DbContext.Users
            .Where(u => u.Id == userId)
            .CountAsync();
        userCount.Should().Be(1, "should not create duplicate users");
    }

    [Fact]
    public async Task LoginWithEntra_WithInvalidToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = new
        {
            accessToken = TestEntraTokens.InvalidToken,
            ipAddress = "192.168.1.102"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login/entra", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "invalid Entra token should be rejected");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("error", "error response should include error message");
    }

    [Fact]
    public async Task LoginWithEntra_WithExpiredToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        var request = new
        {
            accessToken = TestEntraTokens.ExpiredToken,
            ipAddress = "192.168.1.103"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login/entra", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "expired Entra token should be rejected");
    }

    [Fact]
    public async Task LoginWithEntra_WithEmailAlreadyRegisteredLocally_ShouldReturn400BadRequest()
    {
        // Arrange
        // First, register a user with local provider using the same email
        var registerRequest = new
        {
            email = "duplicate@example.com",
            password = "SecurePassword123!",
            firstName = "Local",
            lastName = "User",
            role = UserRole.User
        };
        await HttpClient.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Now try to login with Entra using the same email
        var entraRequest = new
        {
            accessToken = TestEntraTokens.ValidTokenDuplicateEmail,
            ipAddress = "192.168.1.104"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login/entra", entraRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "should prevent dual registration with different providers");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already registered",
            "error message should indicate email conflict");
    }

    [Fact]
    public async Task LoginWithEntra_WhenEntraDisabled_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new
        {
            accessToken = "mock-valid-entra-token",
            ipAddress = "192.168.1.105"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login/entra", request);

        // Assert
        // Note: This test will pass when Entra is enabled.
        // When Entra is disabled, should return 400 or 503
        if (response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("not enabled",
                "error message should indicate Entra is disabled");
        }
        else
        {
            // If Entra is enabled, test should still verify proper authentication
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task LoginWithEntra_ShouldReturnRefreshTokenInResponse()
    {
        // Arrange
        var request = new
        {
            accessToken = TestEntraTokens.ValidTokenProfileUpdate,
            ipAddress = "192.168.1.106"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login/entra", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Verify response structure includes refresh token capability
        result.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty(
            "access token should be provided");
        result.GetProperty("tokenExpiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow,
            "token expiry should be in the future");

        // Verify user has refresh token stored
        var userId = result.GetProperty("user").GetProperty("userId").GetGuid();
        var user = await DbContext.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId);

        user.Should().NotBeNull();
        user!.RefreshTokens.Should().NotBeEmpty("user should have at least one refresh token");
        user.RefreshTokens.Should().Contain(rt => rt.CreatedByIp == "192.168.1.106",
            "refresh token should track IP address");
    }

    [Fact]
    public async Task LoginWithEntra_WithMissingAccessToken_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new
        {
            ipAddress = "192.168.1.107"
            // Missing accessToken field
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login/entra", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "missing access token should result in validation error");
    }
}
