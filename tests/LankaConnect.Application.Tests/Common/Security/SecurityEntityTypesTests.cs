using FluentAssertions;
using LankaConnect.Application.Common.Security;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Security;

/// <summary>
/// TDD RED Phase: Security Entity Types Tests
/// Testing comprehensive security entity patterns for Cultural Intelligence platform
/// </summary>
public class SecurityEntityTypesTests
{
    #region PrivilegedUser Tests (RED Phase)

    [Fact]
    public void PrivilegedUser_Create_ShouldReturnValidUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "admin@lankaconnect.com";
        var culturalClearanceLevel = CulturalClearanceLevel.High;

        // Act
        var result = PrivilegedUser.Create(userId, email, culturalClearanceLevel);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be(email);
        result.Value.ClearanceLevel.Should().Be(culturalClearanceLevel);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PrivilegedUser_CreateWithInvalidEmail_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidEmail = "invalid-email";

        // Act
        var result = PrivilegedUser.Create(userId, invalidEmail, CulturalClearanceLevel.Medium);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
    }

    [Fact]
    public void PrivilegedUser_GrantCulturalAccess_ShouldUpdatePermissions()
    {
        // Arrange
        var user = PrivilegedUser.Create(Guid.NewGuid(), "user@test.com", CulturalClearanceLevel.Low).Value;
        var culturalResource = "sacred-ceremonies-data";

        // Act
        var result = user.GrantCulturalAccess(culturalResource, CulturalAccessType.ReadWrite);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.HasAccessTo(culturalResource).Should().BeTrue();
    }

    #endregion

    #region AccessRequest Tests (RED Phase)

    [Fact]
    public void AccessRequest_Create_ShouldReturnValidRequest()
    {
        // Arrange
        var requesterId = Guid.NewGuid();
        var resourcePath = "/cultural-intelligence/diaspora-analytics";
        var accessType = CulturalAccessType.ReadOnly;

        // Act
        var result = AccessRequest.Create(requesterId, resourcePath, accessType);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.RequesterId.Should().Be(requesterId);
        result.Value.ResourcePath.Should().Be(resourcePath);
        result.Value.AccessType.Should().Be(accessType);
        result.Value.Status.Should().Be(AccessRequestStatus.Pending);
    }

    [Fact]
    public void AccessRequest_Approve_ShouldChangeStatus()
    {
        // Arrange
        var request = AccessRequest.Create(Guid.NewGuid(), "/test-resource", CulturalAccessType.ReadOnly).Value;
        var approverId = Guid.NewGuid();

        // Act
        var result = request.Approve(approverId, "Approved for cultural research");

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(AccessRequestStatus.Approved);
        request.ApproverId.Should().Be(approverId);
    }

    [Fact]
    public void AccessRequest_Reject_ShouldChangeStatusWithReason()
    {
        // Arrange
        var request = AccessRequest.Create(Guid.NewGuid(), "/sacred-content", CulturalAccessType.Admin).Value;
        var rejectionReason = "Insufficient cultural clearance level";

        // Act
        var result = request.Reject(Guid.NewGuid(), rejectionReason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(AccessRequestStatus.Rejected);
        request.RejectionReason.Should().Be(rejectionReason);
    }

    #endregion

    #region CulturalPrivilegePolicy Tests (RED Phase)

    [Fact]
    public void CulturalPrivilegePolicy_Create_ShouldReturnValidPolicy()
    {
        // Arrange
        var policyName = "Sacred Content Access Policy";
        var culturalContext = "Buddhist Temple Ceremonies";
        var minClearanceLevel = CulturalClearanceLevel.High;

        // Act
        var result = CulturalPrivilegePolicy.Create(policyName, culturalContext, minClearanceLevel);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(policyName);
        result.Value.CulturalContext.Should().Be(culturalContext);
        result.Value.MinimumClearanceLevel.Should().Be(minClearanceLevel);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CulturalPrivilegePolicy_EvaluateAccess_ShouldReturnCorrectResult()
    {
        // Arrange
        var policy = CulturalPrivilegePolicy.Create("Test Policy", "General", CulturalClearanceLevel.Medium).Value;
        var highClearanceUser = PrivilegedUser.Create(Guid.NewGuid(), "admin@test.com", CulturalClearanceLevel.High).Value;
        var lowClearanceUser = PrivilegedUser.Create(Guid.NewGuid(), "user@test.com", CulturalClearanceLevel.Low).Value;

        // Act
        var highUserAccess = policy.EvaluateAccess(highClearanceUser, "/test-resource");
        var lowUserAccess = policy.EvaluateAccess(lowClearanceUser, "/test-resource");

        // Assert
        highUserAccess.IsAllowed.Should().BeTrue();
        lowUserAccess.IsAllowed.Should().BeFalse();
        lowUserAccess.DenialReason.Should().Contain("clearance level");
    }

    #endregion

    #region CulturalContentPermissions Tests (RED Phase)

    [Fact]
    public void CulturalContentPermissions_Create_ShouldReturnValidPermissions()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var contentType = CulturalContentType.ReligiousCeremony;
        var sensitivityLevel = CulturalSensitivityLevel.Sacred;

        // Act
        var result = CulturalContentPermissions.Create(contentId, contentType, sensitivityLevel);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.ContentId.Should().Be(contentId);
        result.Value.ContentType.Should().Be(contentType);
        result.Value.SensitivityLevel.Should().Be(sensitivityLevel);
        result.Value.RequiresSpecialApproval.Should().BeTrue();
    }

    [Fact]
    public void CulturalContentPermissions_CheckViewPermission_ShouldValidateCorrectly()
    {
        // Arrange
        var permissions = CulturalContentPermissions.Create(
            Guid.NewGuid(), 
            CulturalContentType.TraditionalMusic, 
            CulturalSensitivityLevel.Public).Value;
        
        var user = PrivilegedUser.Create(Guid.NewGuid(), "user@test.com", CulturalClearanceLevel.Low).Value;

        // Act
        var canView = permissions.CanUserView(user);

        // Assert
        canView.Should().BeTrue(); // Public content should be viewable by all users
    }

    #endregion

    #region Integration Tests (RED Phase)

    [Fact]
    public void SecurityEntities_WorkTogether_ShouldProvideAccessControl()
    {
        // Arrange
        var user = PrivilegedUser.Create(Guid.NewGuid(), "cultural-admin@lankaconnect.com", CulturalClearanceLevel.High).Value;
        var policy = CulturalPrivilegePolicy.Create("Sacred Content Policy", "Temple Access", CulturalClearanceLevel.High).Value;
        var permissions = CulturalContentPermissions.Create(Guid.NewGuid(), CulturalContentType.ReligiousCeremony, CulturalSensitivityLevel.Sacred).Value;
        var accessRequest = AccessRequest.Create(user.Id, "/sacred-temple-ceremonies", CulturalAccessType.ReadOnly).Value;

        // Act
        var policyEvaluation = policy.EvaluateAccess(user, "/sacred-temple-ceremonies");
        var canViewContent = permissions.CanUserView(user);
        var requestApproval = accessRequest.Approve(Guid.NewGuid(), "High clearance user approved for sacred content");

        // Assert
        policyEvaluation.IsAllowed.Should().BeTrue();
        canViewContent.Should().BeTrue(); // High clearance can view sacred content
        requestApproval.IsSuccess.Should().BeTrue();
        accessRequest.Status.Should().Be(AccessRequestStatus.Approved);
    }

    #endregion
}