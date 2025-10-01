using LankaConnect.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// TDD GREEN Phase: Security Privilege Types Implementation
/// Cultural privilege management for diaspora communities with enterprise security
/// </summary>

#region PrivilegedUser

/// <summary>
/// Privileged user entity with cultural clearance levels for accessing sensitive cultural content
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
    public List<string> CulturalCommunities { get; private set; } = new();
    public Dictionary<string, DateTime> AccessHistory { get; private set; } = new();

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

    public CulturalAccessType GetAccessType(string resourcePath)
    {
        return CulturalAccess.TryGetValue(resourcePath, out var accessType)
            ? accessType
            : CulturalAccessType.None;
    }

    public CulturalPrivilegeEvaluation EvaluateCulturalPrivilege(string resourcePath)
    {
        var evaluation = new CulturalPrivilegeEvaluation
        {
            UserId = Id,
            ResourcePath = resourcePath,
            ClearanceLevel = ClearanceLevel,
            HasAccess = HasAccessTo(resourcePath),
            AccessType = GetAccessType(resourcePath),
            EvaluatedAt = DateTime.UtcNow
        };

        return evaluation;
    }

    public void UpdateLastAccess()
    {
        LastAccessAt = DateTime.UtcNow;
    }

    public void RecordResourceAccess(string resourcePath)
    {
        AccessHistory[resourcePath] = DateTime.UtcNow;
        UpdateLastAccess();
    }

    public void AddCulturalCommunity(string community)
    {
        if (!string.IsNullOrWhiteSpace(community) && !CulturalCommunities.Contains(community))
        {
            CulturalCommunities.Add(community);
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static bool IsValidEmail(string email)
    {
        return new EmailAddressAttribute().IsValid(email);
    }
}

#endregion

#region CulturalPrivilegePolicy

/// <summary>
/// Policy defining cultural access privileges and restrictions for diaspora communities
/// </summary>
public class CulturalPrivilegePolicy
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string CulturalContext { get; private set; } = string.Empty;
    public CulturalClearanceLevel MinimumClearanceLevel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public List<string> CulturalRestrictions { get; private set; } = new();
    public Dictionary<string, object> PolicyMetadata { get; private set; } = new();
    public List<string> ExemptCommunities { get; private set; } = new();

    private CulturalPrivilegePolicy() { }

    private CulturalPrivilegePolicy(Guid id, string name, string culturalContext, CulturalClearanceLevel minClearanceLevel)
    {
        Id = id;
        Name = name;
        CulturalContext = culturalContext;
        MinimumClearanceLevel = minClearanceLevel;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<CulturalPrivilegePolicy> Create(Guid id, string name, string culturalContext, CulturalClearanceLevel minClearanceLevel)
    {
        if (id == Guid.Empty)
            return Result<CulturalPrivilegePolicy>.Failure("Policy ID cannot be empty");

        if (string.IsNullOrWhiteSpace(name))
            return Result<CulturalPrivilegePolicy>.Failure("Policy name cannot be empty");

        if (string.IsNullOrWhiteSpace(culturalContext))
            return Result<CulturalPrivilegePolicy>.Failure("Cultural context cannot be empty");

        var policy = new CulturalPrivilegePolicy(id, name, culturalContext, minClearanceLevel);
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

        if (CulturalRestrictions.Contains(resourcePath))
        {
            // Check if user's community is exempt
            var isExempt = user.CulturalCommunities.Any(community => ExemptCommunities.Contains(community));
            if (!isExempt)
                return CulturalAccessEvaluation.Deny("Resource is restricted by cultural policy");
        }

        return CulturalAccessEvaluation.Allow("Access granted by policy evaluation");
    }

    public void AddCulturalRestriction(string restriction)
    {
        if (!string.IsNullOrWhiteSpace(restriction) && !CulturalRestrictions.Contains(restriction))
        {
            CulturalRestrictions.Add(restriction);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddExemptCommunity(string community)
    {
        if (!string.IsNullOrWhiteSpace(community) && !ExemptCommunities.Contains(community))
        {
            ExemptCommunities.Add(community);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveCulturalRestriction(string restriction)
    {
        if (CulturalRestrictions.Remove(restriction))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}

#endregion

#region PrivilegedAccessResult

/// <summary>
/// Result of privileged access evaluation with cultural context
/// </summary>
public class PrivilegedAccessResult
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ResourcePath { get; private set; } = string.Empty;
    public CulturalAccessType AccessType { get; private set; }
    public bool IsGranted { get; private set; }
    public DateTime EvaluatedAt { get; private set; } = DateTime.UtcNow;
    public string CulturalContext { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public List<string> AuditTrail { get; private set; } = new();
    public Dictionary<string, object> AccessMetadata { get; private set; } = new();

    private PrivilegedAccessResult() { }

    private PrivilegedAccessResult(Guid id, Guid userId, string resourcePath, CulturalAccessType accessType, bool isGranted)
    {
        Id = id;
        UserId = userId;
        ResourcePath = resourcePath;
        AccessType = accessType;
        IsGranted = isGranted;
        EvaluatedAt = DateTime.UtcNow;
    }

    public static Result<PrivilegedAccessResult> Create(Guid id, Guid userId, string resourcePath, CulturalAccessType accessType, bool isGranted)
    {
        if (id == Guid.Empty)
            return Result<PrivilegedAccessResult>.Failure("Access result ID cannot be empty");

        if (userId == Guid.Empty)
            return Result<PrivilegedAccessResult>.Failure("User ID cannot be empty");

        if (string.IsNullOrWhiteSpace(resourcePath))
            return Result<PrivilegedAccessResult>.Failure("Resource path cannot be empty");

        var result = new PrivilegedAccessResult(id, userId, resourcePath, accessType, isGranted);
        return Result<PrivilegedAccessResult>.Success(result);
    }

    public void SetCulturalContext(string culturalContext)
    {
        if (!string.IsNullOrWhiteSpace(culturalContext))
        {
            CulturalContext = culturalContext;
        }
    }

    public void SetReason(string reason)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Reason = reason;
        }
    }

    public void AddAuditEntry(string entry)
    {
        if (!string.IsNullOrWhiteSpace(entry))
        {
            var auditEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC: {entry}";
            AuditTrail.Add(auditEntry);
        }
    }

    public void AddMetadata(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key) && value != null)
        {
            AccessMetadata[key] = value;
        }
    }

    public void GrantAccess(string reason)
    {
        IsGranted = true;
        SetReason(reason);
        AddAuditEntry($"Access granted: {reason}");
    }

    public void DenyAccess(string reason)
    {
        IsGranted = false;
        SetReason(reason);
        AddAuditEntry($"Access denied: {reason}");
    }
}

#endregion

#region Supporting Types and Enums

/// <summary>
/// Cultural privilege evaluation result
/// </summary>
public class CulturalPrivilegeEvaluation
{
    public Guid UserId { get; set; }
    public string ResourcePath { get; set; } = string.Empty;
    public CulturalClearanceLevel ClearanceLevel { get; set; }
    public bool HasAccess { get; set; }
    public CulturalAccessType AccessType { get; set; }
    public DateTime EvaluatedAt { get; set; }
}

/// <summary>
/// Cultural access evaluation result
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
    None = 0,
    ReadOnly = 1,
    ReadWrite = 2,
    Admin = 3,
    Sacred = 4
}

#endregion