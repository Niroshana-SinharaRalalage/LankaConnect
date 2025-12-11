using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for External Provider Management endpoints (Epic 1 Phase 2)
/// Tests POST /api/users/{id}/external-providers/link
/// Tests DELETE /api/users/{id}/external-providers/{provider}
/// Tests GET /api/users/{id}/external-providers
/// </summary>
public class ExternalProviderManagementTests : DockerComposeWebApiTestBase
{
    private async Task<Guid> CreateTestUser(string? email = null)
    {
        // Generate unique email if not provided
        email ??= $"testuser-{Guid.NewGuid()}@example.com";

        var emailVO = Domain.Shared.ValueObjects.Email.Create(email).Value;
        var user = User.Create(emailVO, "Test", "User").Value;
        user.SetPassword("hashed-password"); // User has local password

        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Commit transaction so API can see the user
        await CommitAndBeginNewTransaction();

        return user.Id;
    }

    #region LinkExternalProvider Tests

    [Fact]
    public async Task LinkExternalProvider_WithValidData_ShouldReturn200AndLinkProvider()
    {
        // Arrange
        var userId = await CreateTestUser();

        var externalProviderId = $"facebook-{Guid.NewGuid()}";
        var request = new
        {
            provider = "Facebook",
            externalProviderId = externalProviderId,
            providerEmail = "testuser@facebook.com"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", request);

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 200 OK but got {response.StatusCode}. Response: {errorContent}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "valid request should link provider successfully");

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.GetProperty("userId").GetGuid().Should().Be(userId);
        result.GetProperty("provider").GetString().Should().Be("Facebook");
        result.GetProperty("providerEmail").GetString().Should().Be("testuser@facebook.com");

        // Verify in database
        var user = await DbContext.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        user.Should().NotBeNull();
        user!.ExternalLogins.Should().ContainSingle();
        user.ExternalLogins.First().Provider.Should().Be(FederatedProvider.Facebook);
        user.ExternalLogins.First().ExternalProviderId.Should().Be(externalProviderId);
    }

    [Fact]
    public async Task LinkExternalProvider_WhenUserNotFound_ShouldReturn400()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        var request = new
        {
            provider = "Google",
            externalProviderId = $"google-{Guid.NewGuid()}",
            providerEmail = "test@google.com"
        };

        // Act
        var response = await HttpClient.PostAsJsonAsync($"/api/users/{nonExistentUserId}/external-providers/link", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("User not found", "error message should indicate user not found");
    }

    [Fact]
    public async Task LinkExternalProvider_WhenAlreadyLinked_ShouldReturn400()
    {
        // Arrange
        var userId = await CreateTestUser();

        var request = new
        {
            provider = "Apple",
            externalProviderId = $"apple-{Guid.NewGuid()}",
            providerEmail = "test@apple.com"
        };

        // Link provider first time
        await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", request);

        // Act - Try to link same provider again
        var response = await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already linked", "error should indicate provider is already linked");
    }

    [Fact]
    public async Task LinkExternalProvider_WithMultipleProviders_ShouldLinkAll()
    {
        // Arrange
        var userId = await CreateTestUser();

        // Act - Link Facebook
        var facebookRequest = new
        {
            provider = "Facebook",
            externalProviderId = $"facebook-{Guid.NewGuid()}",
            providerEmail = "test@facebook.com"
        };
        var facebookResponse = await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", facebookRequest);

        // Act - Link Google
        var googleRequest = new
        {
            provider = "Google",
            externalProviderId = $"google-{Guid.NewGuid()}",
            providerEmail = "test@google.com"
        };
        var googleResponse = await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", googleRequest);

        // Assert
        facebookResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        googleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify in database
        var user = await DbContext.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        user!.ExternalLogins.Should().HaveCount(2);
        user.ExternalLogins.Should().Contain(l => l.Provider == FederatedProvider.Facebook);
        user.ExternalLogins.Should().Contain(l => l.Provider == FederatedProvider.Google);
    }

    #endregion

    #region UnlinkExternalProvider Tests

    [Fact]
    public async Task UnlinkExternalProvider_WithValidData_ShouldReturn200AndUnlinkProvider()
    {
        // Arrange
        var userId = await CreateTestUser();

        // Link provider first
        var linkRequest = new
        {
            provider = "Facebook",
            externalProviderId = $"facebook-{Guid.NewGuid()}",
            providerEmail = "test@facebook.com"
        };
        await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", linkRequest);

        // Act
        var response = await HttpClient.DeleteAsync($"/api/users/{userId}/external-providers/Facebook");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "valid request should unlink provider successfully");

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.GetProperty("userId").GetGuid().Should().Be(userId);
        result.GetProperty("provider").GetString().Should().Be("Facebook");

        // Verify in database
        var user = await DbContext.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        user!.ExternalLogins.Should().BeEmpty();
    }

    [Fact]
    public async Task UnlinkExternalProvider_WhenUserNotFound_ShouldReturn404()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await HttpClient.DeleteAsync($"/api/users/{nonExistentUserId}/external-providers/Facebook");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnlinkExternalProvider_WhenNotLinked_ShouldReturn400()
    {
        // Arrange
        var userId = await CreateTestUser();

        // Act
        var response = await HttpClient.DeleteAsync($"/api/users/{userId}/external-providers/Google");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not linked");
    }

    [Fact]
    public async Task UnlinkExternalProvider_WhenLastAuthMethod_ShouldReturn400()
    {
        // Arrange
        var emailVO = Domain.Shared.ValueObjects.Email.Create($"external-only-{Guid.NewGuid()}@example.com").Value;
        var user = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            $"entra-{Guid.NewGuid()}",
            emailVO,
            "External",
            "User",
            FederatedProvider.Microsoft,
            emailVO.Value).Value;

        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Commit transaction so API can see the user
        await CommitAndBeginNewTransaction();

        // Act - Try to unlink the only authentication method
        var response = await HttpClient.DeleteAsync($"/api/users/{user.Id}/external-providers/Microsoft");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("last authentication method",
            "error should indicate cannot unlink last auth method");
    }

    [Fact]
    public async Task UnlinkExternalProvider_WhenUserHasOtherProviders_ShouldUnlinkSuccessfully()
    {
        // Arrange
        var userId = await CreateTestUser();

        // Link multiple providers
        await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", new
        {
            provider = "Facebook",
            externalProviderId = $"fb-{Guid.NewGuid()}",
            providerEmail = "test@fb.com"
        });

        await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", new
        {
            provider = "Google",
            externalProviderId = $"google-{Guid.NewGuid()}",
            providerEmail = "test@google.com"
        });

        // Act - Unlink one provider (still has local password + Google)
        var response = await HttpClient.DeleteAsync($"/api/users/{userId}/external-providers/Facebook");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Facebook is unlinked but Google remains
        var user = await DbContext.Users
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        user!.ExternalLogins.Should().ContainSingle();
        user.ExternalLogins.First().Provider.Should().Be(FederatedProvider.Google);
    }

    #endregion

    #region GetLinkedProviders Tests

    [Fact]
    public async Task GetLinkedProviders_WithNoProviders_ShouldReturn200AndEmptyList()
    {
        // Arrange
        var userId = await CreateTestUser();

        // Act
        var response = await HttpClient.GetAsync($"/api/users/{userId}/external-providers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.GetProperty("userId").GetGuid().Should().Be(userId);
        result.GetProperty("linkedProviders").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetLinkedProviders_WithLinkedProviders_ShouldReturn200AndProviderList()
    {
        // Arrange
        var userId = await CreateTestUser();

        // Link providers
        await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", new
        {
            provider = "Facebook",
            externalProviderId = $"fb-{Guid.NewGuid()}",
            providerEmail = "test@fb.com"
        });

        await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", new
        {
            provider = "Google",
            externalProviderId = $"google-{Guid.NewGuid()}",
            providerEmail = "test@google.com"
        });

        // Act
        var response = await HttpClient.GetAsync($"/api/users/{userId}/external-providers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        result.GetProperty("userId").GetGuid().Should().Be(userId);
        var providers = result.GetProperty("linkedProviders");
        providers.GetArrayLength().Should().Be(2);

        // Verify provider details
        var providersList = providers.EnumerateArray().ToList();
        providersList.Should().Contain(p => p.GetProperty("provider").GetString() == "Facebook");
        providersList.Should().Contain(p => p.GetProperty("provider").GetString() == "Google");

        // Verify all expected properties exist
        var firstProvider = providersList.First();
        firstProvider.GetProperty("provider").GetString().Should().NotBeNullOrEmpty();
        firstProvider.GetProperty("providerDisplayName").GetString().Should().NotBeNullOrEmpty();
        firstProvider.GetProperty("externalProviderId").GetString().Should().NotBeNullOrEmpty();
        firstProvider.GetProperty("providerEmail").GetString().Should().NotBeNullOrEmpty();
        firstProvider.GetProperty("linkedAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetLinkedProviders_WhenUserNotFound_ShouldReturn404()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/api/users/{nonExistentUserId}/external-providers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region End-to-End Workflow Test

    [Fact]
    public async Task ExternalProviderWorkflow_LinkGetUnlink_ShouldWorkEndToEnd()
    {
        // Arrange
        var userId = await CreateTestUser();

        // Step 1: Link Facebook
        var linkFacebookResponse = await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", new
        {
            provider = "Facebook",
            externalProviderId = $"fb-{Guid.NewGuid()}",
            providerEmail = "workflow@fb.com"
        });
        linkFacebookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Link Google
        var linkGoogleResponse = await HttpClient.PostAsJsonAsync($"/api/users/{userId}/external-providers/link", new
        {
            provider = "Google",
            externalProviderId = $"google-{Guid.NewGuid()}",
            providerEmail = "workflow@google.com"
        });
        linkGoogleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Get linked providers (should have 2)
        var getProvidersResponse1 = await HttpClient.GetAsync($"/api/users/{userId}/external-providers");
        getProvidersResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

        var content1 = await getProvidersResponse1.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<JsonElement>(content1, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        result1.GetProperty("linkedProviders").GetArrayLength().Should().Be(2);

        // Step 4: Unlink Facebook
        var unlinkResponse = await HttpClient.DeleteAsync($"/api/users/{userId}/external-providers/Facebook");
        unlinkResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 5: Get linked providers (should have 1)
        var getProvidersResponse2 = await HttpClient.GetAsync($"/api/users/{userId}/external-providers");
        getProvidersResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content2 = await getProvidersResponse2.Content.ReadAsStringAsync();
        var result2 = JsonSerializer.Deserialize<JsonElement>(content2, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        result2.GetProperty("linkedProviders").GetArrayLength().Should().Be(1);
        result2.GetProperty("linkedProviders")[0].GetProperty("provider").GetString().Should().Be("Google");
    }

    #endregion
}
