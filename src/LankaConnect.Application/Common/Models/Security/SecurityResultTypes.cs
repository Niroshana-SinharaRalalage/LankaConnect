using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common.Security;

namespace LankaConnect.Application.Common.Models.Security;

/// <summary>
/// Security validation result types for access control and authentication
/// </summary>

#region Access Validation Types

public class AccessValidationResult : Result<AccessValidationData>
{
    public AccessRequest Request { get; private set; } = null!;
    public CulturalContentPermissions Permissions { get; private set; } = null!;
    public DateTime ValidationTimestamp { get; private set; }

    private AccessValidationResult(bool isSuccess, IEnumerable<string> errors, AccessValidationData? value = null)
        : base(isSuccess, errors, value)
    {
        ValidationTimestamp = DateTime.UtcNow;
    }

    public static AccessValidationResult Success(AccessValidationData data, AccessRequest request, CulturalContentPermissions permissions)
    {
        return new AccessValidationResult(true, Enumerable.Empty<string>(), data)
        {
            Request = request,
            Permissions = permissions
        };
    }

    public static AccessValidationResult Failure(string error, AccessRequest request)
    {
        return new AccessValidationResult(false, new[] { error })
        {
            Request = request
        };
    }
}

public class AccessRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public AccessType RequestedAccess { get; set; }
    public CulturalContext CulturalContext { get; set; } = null!;
    public DateTime RequestTimestamp { get; set; }
    public string ClientIpAddress { get; set; } = string.Empty;
    public Dictionary<string, string> RequestMetadata { get; set; } = new();
}

public class CulturalContentPermissions
{
    public string UserId { get; set; } = string.Empty;
    public List<string> AllowedCulturalContent { get; set; } = new();
    public List<string> RestrictedCulturalContent { get; set; } = new();
    public CulturalAccessLevel AccessLevel { get; set; }
    public List<string> CulturalRoles { get; set; } = new();
    public DateTime PermissionsExpiry { get; set; }
    public bool IsCommunityModerator { get; set; }
    public List<string> ModeratedCommunities { get; set; } = new();
}

public class AccessValidationData
{
    public bool IsValid { get; set; }
    public AccessType GrantedAccess { get; set; }
    public List<string> GrantedPermissions { get; set; } = new();
    public List<string> DeniedPermissions { get; set; } = new();
    public string ValidationReason { get; set; } = string.Empty;
    public CulturalContextValidation CulturalValidation { get; set; } = null!;
}

#endregion

#region JIT Access Types

public class JITAccessResult : Result<JITAccessData>
{
    public JITAccessRequest Request { get; private set; } = null!;
    public CulturalResourcePolicy Policy { get; private set; } = null!;
    public TimeSpan AccessDuration { get; private set; }

    private JITAccessResult(bool isSuccess, IEnumerable<string> errors, JITAccessData? value = null)
        : base(isSuccess, errors, value)
    {
    }

    public static JITAccessResult Success(JITAccessData data, JITAccessRequest request, CulturalResourcePolicy policy, TimeSpan duration)
    {
        return new JITAccessResult(true, Enumerable.Empty<string>(), data)
        {
            Request = request,
            Policy = policy,
            AccessDuration = duration
        };
    }

    public static JITAccessResult Failure(string error, JITAccessRequest request)
    {
        return new JITAccessResult(false, new[] { error })
        {
            Request = request
        };
    }
}

public class JITAccessRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ResourcePath { get; set; } = string.Empty;
    public JITAccessType AccessType { get; set; }
    public TimeSpan RequestedDuration { get; set; }
    public string BusinessJustification { get; set; } = string.Empty;
    public CulturalContext CulturalContext { get; set; } = null!;
    public DateTime RequestTimestamp { get; set; }
    public string ApprovalRequired { get; set; } = string.Empty;
    public List<string> RequiredApprovers { get; set; } = new();
}

public class CulturalResourcePolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public List<string> ApplicableResources { get; set; } = new();
    public CulturalAccessLevel MinimumAccessLevel { get; set; }
    public TimeSpan MaxAccessDuration { get; set; }
    public bool RequiresApproval { get; set; }
    public List<string> ApproverRoles { get; set; } = new();
    public Dictionary<string, object> PolicyParameters { get; set; } = new();
    public DateTime PolicyEffectiveDate { get; set; }
    public DateTime PolicyExpiryDate { get; set; }
}

public class JITAccessData
{
    public bool IsGranted { get; set; }
    public DateTime AccessStartTime { get; set; }
    public DateTime AccessEndTime { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public List<string> GrantedPermissions { get; set; } = new();
    public string AccessReason { get; set; } = string.Empty;
}

#endregion

#region Session Security Types

public class SessionSecurityResult : Result<SessionSecurityData>
{
    public CulturalSessionPolicy Policy { get; private set; } = null!;
    public DateTime SecurityCheckTimestamp { get; private set; }

    private SessionSecurityResult(bool isSuccess, IEnumerable<string> errors, SessionSecurityData? value = null)
        : base(isSuccess, errors, value)
    {
        SecurityCheckTimestamp = DateTime.UtcNow;
    }

    public static SessionSecurityResult Success(SessionSecurityData data, CulturalSessionPolicy policy)
    {
        return new SessionSecurityResult(true, Enumerable.Empty<string>(), data)
        {
            Policy = policy
        };
    }

    public static SessionSecurityResult Failure(string error, CulturalSessionPolicy policy)
    {
        return new SessionSecurityResult(false, new[] { error })
        {
            Policy = policy
        };
    }
}

public class CulturalSessionPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public TimeSpan SessionTimeout { get; set; }
    public TimeSpan IdleTimeout { get; set; }
    public int MaxConcurrentSessions { get; set; }
    public bool RequireMFA { get; set; }
    public List<string> AllowedIpRanges { get; set; } = new();
    public CulturalSecurityLevel SecurityLevel { get; set; }
    public bool CulturalContextValidationRequired { get; set; }
    public Dictionary<string, object> SessionParameters { get; set; } = new();
}

public class SessionSecurityData
{
    public bool IsSecure { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime SessionStartTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public string ClientFingerprint { get; set; } = string.Empty;
    public List<SecurityViolation> SecurityViolations { get; set; } = new();
    public CulturalContextValidation CulturalValidation { get; set; } = null!;
}

#endregion

#region MFA Types

public class MFAResult : Result<MFAData>
{
    public MFAConfiguration Configuration { get; private set; } = null!;
    public CulturalAuthenticationContext AuthContext { get; private set; } = null!;

    private MFAResult(bool isSuccess, IEnumerable<string> errors, MFAData? value = null)
        : base(isSuccess, errors, value)
    {
    }

    public static MFAResult Success(MFAData data, MFAConfiguration config, CulturalAuthenticationContext authContext)
    {
        return new MFAResult(true, Enumerable.Empty<string>(), data)
        {
            Configuration = config,
            AuthContext = authContext
        };
    }

    public static MFAResult Failure(string error, MFAConfiguration config)
    {
        return new MFAResult(false, new[] { error })
        {
            Configuration = config
        };
    }
}

public class MFAConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public List<MFAMethod> RequiredMethods { get; set; } = new();
    public List<MFAMethod> OptionalMethods { get; set; } = new();
    public int RequiredFactorCount { get; set; }
    public TimeSpan MFATokenLifetime { get; set; }
    public bool CulturalMethodsAllowed { get; set; }
    public List<string> CulturalMFAMethods { get; set; } = new();
    public Dictionary<string, object> MFAParameters { get; set; } = new();
}

public class CulturalAuthenticationContext
{
    public string UserId { get; set; } = string.Empty;
    public CulturalContext CulturalContext { get; set; } = null!;
    public List<string> CulturalIdentifiers { get; set; } = new();
    public string PreferredLanguage { get; set; } = string.Empty;
    public string CulturalRegion { get; set; } = string.Empty;
    public bool IsCulturalLeader { get; set; }
    public List<string> CulturalRoles { get; set; } = new();
    public Dictionary<string, object> CulturalMetadata { get; set; } = new();
}

public class MFAData
{
    public bool IsAuthenticated { get; set; }
    public List<MFAMethod> CompletedMethods { get; set; } = new();
    public List<MFAMethod> PendingMethods { get; set; } = new();
    public string MFAToken { get; set; } = string.Empty;
    public DateTime AuthenticationTimestamp { get; set; }
    public CulturalAuthenticationScore CulturalScore { get; set; } = null!;
}

#endregion

#region API Access Control Types

public class APIAccessControlResult : Result<APIAccessData>
{
    public APIAccessRequest Request { get; private set; } = null!;
    public CulturalAPIPolicy Policy { get; private set; } = null!;

    private APIAccessControlResult(bool isSuccess, IEnumerable<string> errors, APIAccessData? value = null)
        : base(isSuccess, errors, value)
    {
    }

    public static APIAccessControlResult Success(APIAccessData data, APIAccessRequest request, CulturalAPIPolicy policy)
    {
        return new APIAccessControlResult(true, Enumerable.Empty<string>(), data)
        {
            Request = request,
            Policy = policy
        };
    }

    public static APIAccessControlResult Failure(string error, APIAccessRequest request)
    {
        return new APIAccessControlResult(false, new[] { error })
        {
            Request = request
        };
    }
}

public class APIAccessRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string RequestPayload { get; set; } = string.Empty;
    public CulturalContext CulturalContext { get; set; } = null!;
    public DateTime RequestTimestamp { get; set; }
    public string ClientIpAddress { get; set; } = string.Empty;
}

public class CulturalAPIPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public string ApiEndpointPattern { get; set; } = string.Empty;
    public List<string> AllowedHttpMethods { get; set; } = new();
    public RateLimitConfiguration RateLimit { get; set; } = null!;
    public CulturalAccessLevel RequiredAccessLevel { get; set; }
    public bool CulturalContextRequired { get; set; }
    public List<string> AllowedCulturalRoles { get; set; } = new();
    public Dictionary<string, object> PolicyConstraints { get; set; } = new();
}

public class APIAccessData
{
    public bool IsAllowed { get; set; }
    public List<string> GrantedOperations { get; set; } = new();
    public List<string> DeniedOperations { get; set; } = new();
    public RateLimitStatus RateLimitStatus { get; set; } = null!;
    public string AccessReason { get; set; } = string.Empty;
    public CulturalAccessValidation CulturalValidation { get; set; } = null!;
}

#endregion

#region Supporting Types

public enum AccessType
{
    Read = 1,
    Write = 2,
    Delete = 3,
    Admin = 4,
    CulturalModerator = 5
}

public enum JITAccessType
{
    Emergency = 1,
    Maintenance = 2,
    Investigation = 3,
    CulturalEvent = 4
}

public enum CulturalAccessLevel
{
    Basic = 1,
    Community = 2,
    Moderator = 3,
    Leader = 4,
    Admin = 5
}

public enum CulturalSecurityLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Sacred = 5
}

public enum MFAMethod
{
    SMS = 1,
    Email = 2,
    TOTP = 3,
    BiometricFingerprint = 4,
    BiometricFace = 5,
    CulturalVoiceRecognition = 6,
    CulturalKnowledgeQuestions = 7
}

public class CulturalContextValidation
{
    public bool IsValid { get; set; }
    public string ValidationReason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<string> ValidationFactors { get; set; } = new();
}


public class CulturalAuthenticationScore
{
    public double OverallScore { get; set; }
    public double CulturalContextScore { get; set; }
    public double BehavioralScore { get; set; }
    public double TrustScore { get; set; }
    public DateTime ScoreTimestamp { get; set; }
}

public class RateLimitConfiguration
{
    public int RequestsPerMinute { get; set; }
    public int RequestsPerHour { get; set; }
    public int RequestsPerDay { get; set; }
    public bool CulturalEventBurstAllowed { get; set; }
    public decimal BurstMultiplier { get; set; } = 1.0m;
}

public class RateLimitStatus
{
    public bool IsWithinLimit { get; set; }
    public int RemainingRequests { get; set; }
    public DateTime ResetTime { get; set; }
    public bool IsBurstMode { get; set; }
}

public class CulturalAccessValidation
{
    public bool IsValid { get; set; }
    public List<string> ValidatedRoles { get; set; } = new();
    public string CulturalContext { get; set; } = string.Empty;
    public DateTime ValidationTimestamp { get; set; }
}

#endregion