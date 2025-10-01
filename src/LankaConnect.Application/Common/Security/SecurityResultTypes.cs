using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Security;

/// <summary>
/// TDD GREEN Phase: Application-specific security result types extending Domain Result pattern
/// These types provide specialized result handling for security operations
/// </summary>

/// <summary>
/// Result type for privileged access operations with cultural intelligence
/// </summary>
public class PrivilegedAccessResult : Result<PrivilegedAccessData>
{
    public string AccessLevel => IsSuccess ? Value.AccessLevel : string.Empty;
    public IEnumerable<string> GrantedPermissions => IsSuccess ? Value.GrantedPermissions : Array.Empty<string>();

    protected PrivilegedAccessResult(bool isSuccess, IEnumerable<string> errors, PrivilegedAccessData? value = null)
        : base(isSuccess, errors, value)
    {
    }

    public static PrivilegedAccessResult Success(string accessLevel, IEnumerable<string> grantedPermissions)
    {
        var data = new PrivilegedAccessData(accessLevel ?? string.Empty, grantedPermissions ?? Array.Empty<string>());
        return new PrivilegedAccessResult(true, Array.Empty<string>(), data);
    }

    public static new PrivilegedAccessResult Failure(string error)
    {
        return new PrivilegedAccessResult(false, new[] { error });
    }

    public static new PrivilegedAccessResult Failure(IEnumerable<string> errors)
    {
        return new PrivilegedAccessResult(false, errors);
    }
}

/// <summary>
/// Data container for privileged access information
/// </summary>
public record PrivilegedAccessData(string AccessLevel, IEnumerable<string> GrantedPermissions);

/// <summary>
/// Result type for cultural content access validation
/// </summary>
public class AccessValidationResult : Result<AccessValidationData>
{
    public bool IsValid => IsSuccess && Value.IsValid;
    public string ContentType => IsSuccess ? Value.ContentType : string.Empty;
    public IReadOnlyDictionary<string, object> ValidationMetadata => IsSuccess ? Value.ValidationMetadata : new Dictionary<string, object>();

    protected AccessValidationResult(bool isSuccess, IEnumerable<string> errors, AccessValidationData? value = null)
        : base(isSuccess, errors, value)
    {
    }

    public static AccessValidationResult Success(bool isValid, string contentType, IReadOnlyDictionary<string, object>? validationMetadata = null)
    {
        var data = new AccessValidationData(
            isValid, 
            contentType ?? string.Empty, 
            validationMetadata ?? new Dictionary<string, object>()
        );
        return new AccessValidationResult(true, Array.Empty<string>(), data);
    }

    public static new AccessValidationResult Failure(string error)
    {
        return new AccessValidationResult(false, new[] { error });
    }

    public static new AccessValidationResult Failure(IEnumerable<string> errors)
    {
        return new AccessValidationResult(false, errors);
    }
}

/// <summary>
/// Data container for access validation information
/// </summary>
public record AccessValidationData(bool IsValid, string ContentType, IReadOnlyDictionary<string, object> ValidationMetadata);

/// <summary>
/// Result type for Just-in-Time access operations
/// </summary>
public class JITAccessResult : Result<JITAccessData>
{
    public string AccessToken => IsSuccess ? Value.AccessToken : string.Empty;
    public DateTime ExpirationTime => IsSuccess ? Value.ExpirationTime : DateTime.MinValue;
    public IEnumerable<string> ScopedPermissions => IsSuccess ? Value.ScopedPermissions : Array.Empty<string>();
    public bool IsExpired => IsSuccess && DateTime.UtcNow > Value.ExpirationTime;

    protected JITAccessResult(bool isSuccess, IEnumerable<string> errors, JITAccessData? value = null)
        : base(isSuccess, errors, value)
    {
    }

    public static JITAccessResult Success(string accessToken, DateTime expirationTime, IEnumerable<string> scopedPermissions)
    {
        var data = new JITAccessData(
            accessToken ?? string.Empty,
            expirationTime,
            scopedPermissions ?? Array.Empty<string>()
        );
        return new JITAccessResult(true, Array.Empty<string>(), data);
    }

    public static new JITAccessResult Failure(string error)
    {
        return new JITAccessResult(false, new[] { error });
    }

    public static new JITAccessResult Failure(IEnumerable<string> errors)
    {
        return new JITAccessResult(false, errors);
    }
}

/// <summary>
/// Data container for Just-in-Time access information
/// </summary>
public record JITAccessData(string AccessToken, DateTime ExpirationTime, IEnumerable<string> ScopedPermissions);