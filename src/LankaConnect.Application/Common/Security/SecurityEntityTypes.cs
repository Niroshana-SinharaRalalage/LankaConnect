using LankaConnect.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace LankaConnect.Application.Common.Security;

/// <summary>
/// TDD GREEN Phase: Security Entity Types Implementation
/// Comprehensive security entities for Cultural Intelligence platform access control
/// </summary>

#region Privileged User

/// <summary>
/// Privileged user entity with cultural clearance levels
/// </summary>
public class PrivilegedUser
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public CulturalClearanceLevel ClearanceLevel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastAccessAt { get; private set; }
    public Dictionary<string, CulturalAccessType> CulturalAccess { get; private set; } = new();

    private PrivilegedUser() { }

    private PrivilegedUser(Guid id, string email, CulturalClearanceLevel clearanceLevel)
    {
        Id = id;
        Email = email;
        ClearanceLevel = clearanceLevel;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<PrivilegedUser> Create(Guid userId, string email, CulturalClearanceLevel clearanceLevel)
    {
        if (userId == Guid.Empty)
            return Result<PrivilegedUser>.Failure("User ID cannot be empty");

        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            return Result<PrivilegedUser>.Failure("Invalid email format");

        var user = new PrivilegedUser(userId, email.ToLowerInvariant(), clearanceLevel);
        return Result<PrivilegedUser>.Success(user);
    }

    public Result GrantCulturalAccess(string resourcePath, CulturalAccessType accessType)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return Result.Failure("Resource path cannot be empty");

        CulturalAccess[resourcePath] = accessType;
        return Result.Success();
    }

    public bool HasAccessTo(string resourcePath)
    {
        return CulturalAccess.ContainsKey(resourcePath);
    }

    public void UpdateLastAccess()
    {
        LastAccessAt = DateTime.UtcNow;
    }

    private static bool IsValidEmail(string email)
    {
        return new EmailAddressAttribute().IsValid(email);
    }
}

#endregion

#region Access Request

/// <summary>
/// Request for access to cultural content or resources
/// </summary>
public class AccessRequest
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RequesterId { get; private set; }
    public string ResourcePath { get; private set; } = string.Empty;
    public CulturalAccessType AccessType { get; private set; }
    public AccessRequestStatus Status { get; private set; } = AccessRequestStatus.Pending;
    public DateTime RequestedAt { get; private set; } = DateTime.UtcNow;
    public Guid? ApproverId { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? ApprovalReason { get; private set; }
    public string? RejectionReason { get; private set; }

    private AccessRequest() { }

    private AccessRequest(Guid requesterId, string resourcePath, CulturalAccessType accessType)
    {
        RequesterId = requesterId;
        ResourcePath = resourcePath;
        AccessType = accessType;
        Status = AccessRequestStatus.Pending;
        RequestedAt = DateTime.UtcNow;
    }

    public static Result<AccessRequest> Create(Guid requesterId, string resourcePath, CulturalAccessType accessType)
    {
        if (requesterId == Guid.Empty)
            return Result<AccessRequest>.Failure("Requester ID cannot be empty");

        if (string.IsNullOrWhiteSpace(resourcePath))
            return Result<AccessRequest>.Failure("Resource path cannot be empty");

        var request = new AccessRequest(requesterId, resourcePath, accessType);
        return Result<AccessRequest>.Success(request);
    }

    public Result Approve(Guid approverId, string reason = "")
    {
        if (Status != AccessRequestStatus.Pending)
            return Result.Failure("Request is not in pending status");

        if (approverId == Guid.Empty)
            return Result.Failure("Approver ID cannot be empty");

        Status = AccessRequestStatus.Approved;
        ApproverId = approverId;
        ApprovalReason = reason;
        ProcessedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result Reject(Guid rejectorId, string reason)
    {
        if (Status != AccessRequestStatus.Pending)
            return Result.Failure("Request is not in pending status");

        if (rejectorId == Guid.Empty)
            return Result.Failure("Rejector ID cannot be empty");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Rejection reason is required");

        Status = AccessRequestStatus.Rejected;
        ApproverId = rejectorId;
        RejectionReason = reason;
        ProcessedAt = DateTime.UtcNow;

        return Result.Success();
    }
}

#endregion

#region Cultural Privilege Policy

/// <summary>
/// Policy defining cultural access privileges and restrictions
/// </summary>
public class CulturalPrivilegePolicy
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string CulturalContext { get; private set; } = string.Empty;
    public CulturalClearanceLevel MinimumClearanceLevel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public List<string> RestrictedResources { get; private set; } = new();
    public Dictionary<string, object> PolicyMetadata { get; private set; } = new();

    private CulturalPrivilegePolicy() { }

    private CulturalPrivilegePolicy(string name, string culturalContext, CulturalClearanceLevel minClearanceLevel)
    {
        Name = name;
        CulturalContext = culturalContext;
        MinimumClearanceLevel = minClearanceLevel;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<CulturalPrivilegePolicy> Create(string name, string culturalContext, CulturalClearanceLevel minClearanceLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<CulturalPrivilegePolicy>.Failure("Policy name cannot be empty");

        if (string.IsNullOrWhiteSpace(culturalContext))
            return Result<CulturalPrivilegePolicy>.Failure("Cultural context cannot be empty");

        var policy = new CulturalPrivilegePolicy(name, culturalContext, minClearanceLevel);
        return Result<CulturalPrivilegePolicy>.Success(policy);
    }

    public CulturalAccessEvaluation EvaluateAccess(PrivilegedUser user, string resourcePath)
    {
        if (!IsActive)
            return CulturalAccessEvaluation.Deny("Policy is inactive");

        if (!user.IsActive)
            return CulturalAccessEvaluation.Deny("User is inactive");

        if (user.ClearanceLevel < MinimumClearanceLevel)
            return CulturalAccessEvaluation.Deny($"Insufficient clearance level. Required: {MinimumClearanceLevel}, Current: {user.ClearanceLevel}");

        if (RestrictedResources.Contains(resourcePath))
            return CulturalAccessEvaluation.Deny("Resource is restricted by policy");

        return CulturalAccessEvaluation.Allow("Access granted by policy evaluation");
    }

    public void AddRestrictedResource(string resourcePath)
    {
        if (!string.IsNullOrWhiteSpace(resourcePath) && !RestrictedResources.Contains(resourcePath))
        {
            RestrictedResources.Add(resourcePath);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

#endregion

#region Cultural Content Permissions

/// <summary>
/// Permissions for accessing cultural content based on sensitivity levels
/// </summary>
public class CulturalContentPermissions
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ContentId { get; private set; }
    public CulturalContentType ContentType { get; private set; }
    public CulturalSensitivityLevel SensitivityLevel { get; private set; }
    public bool RequiresSpecialApproval { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public List<Guid> ApprovedUsers { get; private set; } = new();
    public Dictionary<string, object> PermissionMetadata { get; private set; } = new();

    private CulturalContentPermissions() { }

    private CulturalContentPermissions(Guid contentId, CulturalContentType contentType, CulturalSensitivityLevel sensitivityLevel)
    {
        ContentId = contentId;
        ContentType = contentType;
        SensitivityLevel = sensitivityLevel;
        RequiresSpecialApproval = sensitivityLevel >= CulturalSensitivityLevel.Sacred;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<CulturalContentPermissions> Create(Guid contentId, CulturalContentType contentType, CulturalSensitivityLevel sensitivityLevel)
    {
        if (contentId == Guid.Empty)
            return Result<CulturalContentPermissions>.Failure("Content ID cannot be empty");

        var permissions = new CulturalContentPermissions(contentId, contentType, sensitivityLevel);
        return Result<CulturalContentPermissions>.Success(permissions);
    }

    public bool CanUserView(PrivilegedUser user)
    {
        if (!user.IsActive)
            return false;

        // Public content can be viewed by all users
        if (SensitivityLevel == CulturalSensitivityLevel.Public)
            return true;

        // General content requires basic clearance
        if (SensitivityLevel == CulturalSensitivityLevel.General && user.ClearanceLevel >= CulturalClearanceLevel.Low)
            return true;

        // Sensitive content requires medium clearance
        if (SensitivityLevel == CulturalSensitivityLevel.Sensitive && user.ClearanceLevel >= CulturalClearanceLevel.Medium)
            return true;

        // Sacred content requires high clearance
        if (SensitivityLevel == CulturalSensitivityLevel.Sacred && user.ClearanceLevel >= CulturalClearanceLevel.High)
            return true;

        // Check if user is explicitly approved
        return ApprovedUsers.Contains(user.Id);
    }

    public void ApproveUser(Guid userId)
    {
        if (userId != Guid.Empty && !ApprovedUsers.Contains(userId))
        {
            ApprovedUsers.Add(userId);
        }
    }
}

#endregion

#region Supporting Types and Enums

/// <summary>
/// Cultural clearance levels for access control
/// </summary>
public enum CulturalClearanceLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Types of cultural access
/// </summary>
public enum CulturalAccessType
{
    ReadOnly = 1,
    ReadWrite = 2,
    Admin = 3
}

/// <summary>
/// Status of access requests
/// </summary>
public enum AccessRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4
}

/// <summary>
/// Types of cultural content
/// </summary>
public enum CulturalContentType
{
    General = 1,
    TraditionalMusic = 2,
    ReligiousCeremony = 3,
    SacredText = 4,
    AncestralWisdom = 5,
    CulturalArtifact = 6
}

/// <summary>
/// Levels of cultural sensitivity
/// </summary>
public enum CulturalSensitivityLevel
{
    Public = 1,
    General = 2,
    Sensitive = 3,
    Sacred = 4,
    Restricted = 5
}

/// <summary>
/// Result of cultural access evaluation
/// </summary>
public class CulturalAccessEvaluation
{
    public bool IsAllowed { get; private set; }
    public string Reason { get; private set; }
    public string? DenialReason => IsAllowed ? null : Reason;

    private CulturalAccessEvaluation(bool isAllowed, string reason)
    {
        IsAllowed = isAllowed;
        Reason = reason;
    }

    public static CulturalAccessEvaluation Allow(string reason) => new(true, reason);
    public static CulturalAccessEvaluation Deny(string reason) => new(false, reason);
}

#endregion