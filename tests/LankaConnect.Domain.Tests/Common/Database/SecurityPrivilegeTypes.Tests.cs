using Xunit;
using FluentAssertions;
using LankaConnect.Domain.Common.Database;
using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Tests.Common.Database;

/// <summary>
/// TDD RED Phase: Tests for Security Privilege Types
/// Testing cultural privilege management for diaspora communities
/// </summary>
public class SecurityPrivilegeTypesTests
{
    #region PrivilegedUser Tests

    [Fact]
    public void PrivilegedUser_Create_ValidInput_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "admin@lankaconnect.com";
        var clearanceLevel = CulturalClearanceLevel.High;

        // Act
        var result = PrivilegedUser.Create(userId, email, clearanceLevel);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be(email.ToLowerInvariant());
        result.Value.ClearanceLevel.Should().Be(clearanceLevel);
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void PrivilegedUser_Create_InvalidEmail_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "invalid-email";
        var clearanceLevel = CulturalClearanceLevel.High;

        // Act
        var result = PrivilegedUser.Create(userId, email, clearanceLevel);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("email");
    }

    [Fact]
    public void PrivilegedUser_GrantCulturalAccess_ValidResource_ShouldSucceed()
    {
        // Arrange
        var user = CreateValidPrivilegedUser();
        var resourcePath = "/cultural/sacred-ceremonies";
        var accessType = CulturalAccessType.ReadOnly;

        // Act
        var result = user.GrantCulturalAccess(resourcePath, accessType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.HasAccessTo(resourcePath).Should().BeTrue();
    }

    [Fact]
    public void PrivilegedUser_EvaluateCulturalPrivilege_ShouldReturnEvaluation()
    {
        // Arrange
        var user = CreateValidPrivilegedUser();
        var resource = "sacred-texts";

        // Act
        var evaluation = user.EvaluateCulturalPrivilege(resource);

        // Assert
        evaluation.Should().NotBeNull();
        evaluation.ResourcePath.Should().Be(resource);
        evaluation.ClearanceLevel.Should().Be(user.ClearanceLevel);
    }

    #endregion

    #region CulturalPrivilegePolicy Tests

    [Fact]
    public void CulturalPrivilegePolicy_Create_ValidInput_ShouldSucceed()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policyName = "Sacred Content Access";
        var culturalContext = "Buddhist Temple";
        var minClearance = CulturalClearanceLevel.High;

        // Act
        var result = CulturalPrivilegePolicy.Create(policyId, policyName, culturalContext, minClearance);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(policyId);
        result.Value.Name.Should().Be(policyName);
        result.Value.CulturalContext.Should().Be(culturalContext);
        result.Value.MinimumClearanceLevel.Should().Be(minClearance);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CulturalPrivilegePolicy_Create_EmptyName_ShouldFail()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policyName = "";
        var culturalContext = "Buddhist Temple";
        var minClearance = CulturalClearanceLevel.High;

        // Act
        var result = CulturalPrivilegePolicy.Create(policyId, policyName, culturalContext, minClearance);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name");
    }

    [Fact]
    public void CulturalPrivilegePolicy_EvaluateAccess_SufficientClearance_ShouldAllow()
    {
        // Arrange
        var policy = CreateValidPrivilegePolicy();
        var user = CreateValidPrivilegedUser();
        var resourcePath = "/cultural/general";

        // Act
        var evaluation = policy.EvaluateAccess(user, resourcePath);

        // Assert
        evaluation.IsAllowed.Should().BeTrue();
        evaluation.Reason.Should().Contain("granted");
    }

    [Fact]
    public void CulturalPrivilegePolicy_AddCulturalRestriction_ShouldUpdateRestrictions()
    {
        // Arrange
        var policy = CreateValidPrivilegePolicy();
        var restriction = "sacred-ceremonies";

        // Act
        policy.AddCulturalRestriction(restriction);

        // Assert
        policy.CulturalRestrictions.Should().Contain(restriction);
    }

    #endregion

    #region PrivilegedAccessResult Tests

    [Fact]
    public void PrivilegedAccessResult_Create_ValidInput_ShouldSucceed()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourcePath = "/cultural/sacred";
        var accessType = CulturalAccessType.ReadOnly;
        var isGranted = true;

        // Act
        var result = PrivilegedAccessResult.Create(resultId, userId, resourcePath, accessType, isGranted);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(resultId);
        result.Value.UserId.Should().Be(userId);
        result.Value.ResourcePath.Should().Be(resourcePath);
        result.Value.AccessType.Should().Be(accessType);
        result.Value.IsGranted.Should().Be(isGranted);
    }

    [Fact]
    public void PrivilegedAccessResult_Create_EmptyResourcePath_ShouldFail()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var resourcePath = "";
        var accessType = CulturalAccessType.ReadOnly;
        var isGranted = true;

        // Act
        var result = PrivilegedAccessResult.Create(resultId, userId, resourcePath, accessType, isGranted);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("resource");
    }

    [Fact]
    public void PrivilegedAccessResult_SetCulturalContext_ShouldUpdateContext()
    {
        // Arrange
        var accessResult = CreateValidAccessResult();
        var culturalContext = "Vesak Festival";

        // Act
        accessResult.SetCulturalContext(culturalContext);

        // Assert
        accessResult.CulturalContext.Should().Be(culturalContext);
    }

    [Fact]
    public void PrivilegedAccessResult_AddAuditEntry_ShouldUpdateAuditTrail()
    {
        // Arrange
        var accessResult = CreateValidAccessResult();
        var auditMessage = "Access granted for sacred content";

        // Act
        accessResult.AddAuditEntry(auditMessage);

        // Assert
        accessResult.AuditTrail.Should().Contain(entry => entry.Contains(auditMessage));
    }

    #endregion

    #region Helper Methods

    private PrivilegedUser CreateValidPrivilegedUser()
    {
        return PrivilegedUser.Create(
            Guid.NewGuid(),
            "test@lankaconnect.com",
            CulturalClearanceLevel.High
        ).Value;
    }

    private CulturalPrivilegePolicy CreateValidPrivilegePolicy()
    {
        return CulturalPrivilegePolicy.Create(
            Guid.NewGuid(),
            "Test Policy",
            "General Cultural Content",
            CulturalClearanceLevel.Medium
        ).Value;
    }

    private PrivilegedAccessResult CreateValidAccessResult()
    {
        return PrivilegedAccessResult.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "/cultural/general",
            CulturalAccessType.ReadOnly,
            true
        ).Value;
    }

    #endregion
}