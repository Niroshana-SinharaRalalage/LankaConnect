using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Application.Auth.Commands.RegisterUser;
using LankaConnect.Application.Auth.Commands.LoginUser;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.IntegrationTests.Common;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.IntegrationTests.Controllers;

public class AuthControllerTests : DockerComposeWebApiTestBase
{
    [Fact]
    public async Task Register_WithValidData_ShouldReturn201Created()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "testuser@example.com",
            "ValidPassword123!",
            "John",
            "Doe",
            UserRole.GeneralUser);

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterUserResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        result.Should().NotBeNull();
        result!.Email.Should().Be(request.Email);
        result.FullName.Should().Be("John Doe");
        result.EmailVerificationRequired.Should().BeTrue();

        // Verify user was created in database
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Email.Value == request.Email);
        user.Should().NotBeNull();
        user!.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Role.Should().Be(UserRole.GeneralUser);
        user.IsEmailVerified.Should().BeFalse();
        user.PasswordHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "invalid-email",
            "ValidPassword123!",
            "John",
            "Doe");

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new RegisterUserCommand(
            "testuser2@example.com",
            "weak",
            "John",
            "Doe");

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn400BadRequest()
    {
        // Arrange
        var email = "duplicate@example.com";
        var firstRequest = new RegisterUserCommand(email, "ValidPassword123!", "John", "Doe");
        var secondRequest = new RegisterUserCommand(email, "AnotherPassword123!", "Jane", "Smith");

        // Act
        await HttpClient.PostAsJsonAsync("/api/auth/register", firstRequest);
        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", secondRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200OK()
    {
        // Arrange
        var email = "logintest@example.com";
        var password = "ValidPassword123!";
        
        // First register a user
        await RegisterAndVerifyUser(email, password, "John", "Doe");

        var loginRequest = new LoginUserCommand { Email = email, Password = password };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        result.RootElement.TryGetProperty("user", out var userElement).Should().BeTrue();
        result.RootElement.TryGetProperty("accessToken", out var tokenElement).Should().BeTrue();
        result.RootElement.TryGetProperty("tokenExpiresAt", out var expiryElement).Should().BeTrue();

        userElement.GetProperty("email").GetString().Should().Be(email);
        tokenElement.GetString().Should().NotBeNullOrEmpty();

        // Verify refresh token cookie is set
        response.Headers.Should().ContainKey("Set-Cookie");
        var cookies = response.Headers.GetValues("Set-Cookie");
        cookies.Should().Contain(c => c.StartsWith("refreshToken="));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn400BadRequest()
    {
        // Arrange
        var loginRequest = new LoginUserCommand { Email = "nonexistent@example.com", Password = "wrongpassword" };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithUnverifiedEmail_ShouldReturn401Unauthorized()
    {
        // Arrange
        var email = "unverified@example.com";
        var password = "ValidPassword123!";
        
        // Register user but don't verify email
        var registerRequest = new RegisterUserCommand(email, password, "John", "Doe");
        await HttpClient.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginUserCommand { Email = email, Password = password };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("verified");
    }

    [Fact]
    public async Task GetProfile_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/auth/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_WithValidToken_ShouldReturn200OK()
    {
        // Arrange
        var email = "profiletest@example.com";
        var password = "ValidPassword123!";
        
        await RegisterAndVerifyUser(email, password, "John", "Doe");
        var token = await LoginAndGetToken(email, password);

        HttpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await HttpClient.GetAsync("/api/auth/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        result.RootElement.GetProperty("email").GetString().Should().Be(email);
        result.RootElement.GetProperty("firstName").GetString().Should().Be("John");
        result.RootElement.GetProperty("lastName").GetString().Should().Be("Doe");
        result.RootElement.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Logout_WithoutAuthentication_ShouldReturn401Unauthorized()
    {
        // Act
        var response = await HttpClient.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldReturn200OK()
    {
        // Arrange
        var email = "logouttest@example.com";
        var password = "ValidPassword123!";
        
        await RegisterAndVerifyUser(email, password, "John", "Doe");
        
        // Login to get tokens and cookies
        var loginRequest = new LoginUserCommand { Email = email, Password = password };
        var loginResponse = await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        // Extract access token
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonDocument.Parse(loginContent);
        var accessToken = loginResult.RootElement.GetProperty("accessToken").GetString();

        HttpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        // Extract refresh token cookie
        var cookies = loginResponse.Headers.GetValues("Set-Cookie");
        var refreshCookie = cookies.First(c => c.StartsWith("refreshToken="));
        HttpClient.DefaultRequestHeaders.Add("Cookie", refreshCookie);

        // Act
        var response = await HttpClient.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify refresh token cookie is cleared
        var logoutCookies = response.Headers.GetValues("Set-Cookie");
        logoutCookies.Should().Contain(c => c.StartsWith("refreshToken=;"));
    }

    [Fact]
    public async Task RefreshToken_WithoutCookie_ShouldReturn400BadRequest()
    {
        // Act
        var response = await HttpClient.PostAsync("/api/auth/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_ShouldReturn200OK()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/auth/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        
        result.RootElement.GetProperty("service").GetString().Should().Be("Authentication");
        result.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Theory]
    [InlineData(UserRole.GeneralUser)]
    [InlineData(UserRole.EventOrganizer)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.AdminManager)]
    public async Task Register_WithDifferentRoles_ShouldCreateUserWithCorrectRole(UserRole role)
    {
        // Arrange
        var request = new RegisterUserCommand(
            $"role-test-{role}@example.com",
            "ValidPassword123!",
            "Test",
            "User",
            role);

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Verify role in database
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Email.Value == request.Email);
        user.Should().NotBeNull();
        user!.Role.Should().Be(role);
    }

    [Fact]
    public async Task Login_MultipleFailedAttempts_ShouldLockAccount()
    {
        // Arrange
        var email = "locktest@example.com";
        var password = "ValidPassword123!";
        
        await RegisterAndVerifyUser(email, password, "John", "Doe");

        // Attempt multiple failed logins
        for (int i = 0; i < 5; i++)
        {
            var failedRequest = new LoginUserCommand { Email = email, Password = "wrongpassword" };
            await HttpClient.PostAsJsonAsync("/api/auth/login", failedRequest);
        }

        // Act - Try with correct password
        var validRequest = new LoginUserCommand { Email = email, Password = password };
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login", validRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Locked);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("locked");
    }

    [Fact]
    public async Task ForgotPassword_WithValidEmail_ShouldReturn200OK()
    {
        // Arrange
        var email = "forgotpassword@example.com";
        var password = "ValidPassword123!";

        await RegisterAndVerifyUser(email, password, "John", "Doe");

        var request = new { Email = email };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("password reset link has been sent");
        content.Should().Contain(email);

        // Verify token was set in database
        var user = await DbContext.Users.FirstAsync(u => u.Email.Value == email);
        user.PasswordResetToken.Should().NotBeNullOrEmpty();
        user.PasswordResetTokenExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmail_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new { Email = "invalid-email" };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentUser_ShouldReturn200OK()
    {
        // Arrange - Security feature: don't reveal if user exists
        var request = new { Email = "nonexistent@example.com" };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Assert - Should return success for security
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("password reset link has been sent");
    }

    [Fact]
    public async Task ResetPassword_WithValidTokenAndPassword_ShouldReturn200OK()
    {
        // Arrange
        var email = "resetpassword@example.com";
        var password = "ValidPassword123!";

        await RegisterAndVerifyUser(email, password, "John", "Doe");

        // Trigger forgot password to get token
        var forgotRequest = new { Email = email };
        await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", forgotRequest);

        // Get token from database
        var user = await DbContext.Users.FirstAsync(u => u.Email.Value == email);
        var token = user.PasswordResetToken;

        var resetRequest = new
        {
            Email = email,
            Token = token,
            NewPassword = "NewValidPassword123!"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);

        result.RootElement.GetProperty("message").GetString().Should().Contain("reset successfully");
        result.RootElement.GetProperty("email").GetString().Should().Be(email);
        result.RootElement.GetProperty("requiresLogin").GetBoolean().Should().BeTrue();

        // Verify token was cleared
        await DbContext.Entry(user).ReloadAsync();
        user.PasswordResetToken.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ShouldReturn400BadRequest()
    {
        // Arrange
        var email = "resetinvalid@example.com";
        var password = "ValidPassword123!";

        await RegisterAndVerifyUser(email, password, "John", "Doe");

        var resetRequest = new
        {
            Email = email,
            Token = "invalid-token-12345",
            NewPassword = "NewValidPassword123!"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid or expired");
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ShouldReturn400BadRequest()
    {
        // Arrange
        var email = "resetexpired@example.com";
        var password = "ValidPassword123!";

        await RegisterAndVerifyUser(email, password, "John", "Doe");

        // Set expired token directly in database
        var user = await DbContext.Users.FirstAsync(u => u.Email.Value == email);
        user.SetPasswordResetToken("expired-token", DateTime.UtcNow.AddHours(-1)); // Expired 1 hour ago
        await DbContext.SaveChangesAsync();

        var resetRequest = new
        {
            Email = email,
            Token = "expired-token",
            NewPassword = "NewValidPassword123!"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid or expired");
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_ShouldReturn400BadRequest()
    {
        // Arrange
        var email = "resetweak@example.com";
        var password = "ValidPassword123!";

        await RegisterAndVerifyUser(email, password, "John", "Doe");

        // Trigger forgot password to get token
        var forgotRequest = new { Email = email };
        await HttpClient.PostAsJsonAsync("/api/auth/forgot-password", forgotRequest);

        // Get token from database
        var user = await DbContext.Users.FirstAsync(u => u.Email.Value == email);
        var token = user.PasswordResetToken;

        var resetRequest = new
        {
            Email = email,
            Token = token,
            NewPassword = "weak" // Weak password
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/reset-password", resetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("password");
    }

    [Fact]
    public async Task VerifyEmail_WithValidToken_ShouldReturn200OK()
    {
        // Arrange
        var email = "verifyemail@example.com";
        var password = "ValidPassword123!";

        // Register user (don't verify)
        var registerRequest = new RegisterUserCommand(email, password, "John", "Doe");
        await HttpClient.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Get verification token from database
        var user = await DbContext.Users.FirstAsync(u => u.Email.Value == email);
        var token = user.EmailVerificationToken;

        var verifyRequest = new
        {
            UserId = user.Id,
            Token = token
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);

        result.RootElement.GetProperty("message").GetString().Should().Contain("verified successfully");
        result.RootElement.GetProperty("email").GetString().Should().Be(email);
        result.RootElement.GetProperty("wasAlreadyVerified").GetBoolean().Should().BeFalse();

        // Verify email is now verified in database
        await DbContext.Entry(user).ReloadAsync();
        user.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ShouldReturn400BadRequest()
    {
        // Arrange
        var email = "verifyinvalid@example.com";
        var password = "ValidPassword123!";

        var registerRequest = new RegisterUserCommand(email, password, "John", "Doe");
        await HttpClient.PostAsJsonAsync("/api/auth/register", registerRequest);

        var user = await DbContext.Users.FirstAsync(u => u.Email.Value == email);

        var verifyRequest = new
        {
            UserId = user.Id,
            Token = "invalid-token-12345"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid or expired");
    }

    [Fact]
    public async Task VerifyEmail_WithAlreadyVerifiedEmail_ShouldReturn200OK()
    {
        // Arrange
        var email = "alreadyverified@example.com";
        var password = "ValidPassword123!";

        await RegisterAndVerifyUser(email, password, "John", "Doe");

        var user = await DbContext.Users.FirstAsync(u => u.Email.Value == email);

        // Set a new verification token for testing
        user.SetEmailVerificationToken(Guid.NewGuid().ToString(), DateTime.UtcNow.AddHours(24));
        await DbContext.SaveChangesAsync();

        var verifyRequest = new
        {
            UserId = user.Id,
            Token = user.EmailVerificationToken
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);

        result.RootElement.GetProperty("message").GetString().Should().Contain("already verified");
        result.RootElement.GetProperty("wasAlreadyVerified").GetBoolean().Should().BeTrue();
    }

    #region Helper Methods

    private async Task RegisterAndVerifyUser(string email, string password, string firstName, string lastName)
    {
        var registerRequest = new RegisterUserCommand(email, password, firstName, lastName);
        await HttpClient.PostAsJsonAsync("/api/auth/register", registerRequest);

        // Manually verify email in database for testing
        var user = await DbContext.Users.FirstAsync(u => u.Email.Value == email);
        user.VerifyEmail();
        await DbContext.SaveChangesAsync();
    }

    private async Task<string> LoginAndGetToken(string email, string password)
    {
        var loginRequest = new LoginUserCommand { Email = email, Password = password };
        var response = await HttpClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonDocument.Parse(content);
        return result.RootElement.GetProperty("accessToken").GetString()!;
    }

    #endregion
}