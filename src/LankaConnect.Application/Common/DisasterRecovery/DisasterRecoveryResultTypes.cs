using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// TDD GREEN Phase: Disaster Recovery Result Types Implementation
/// Comprehensive result patterns for Cultural Intelligence backup, disaster recovery, and data integrity operations
/// </summary>

#region Data Integrity Validation Results

/// <summary>
/// Result type for data integrity validation operations
/// </summary>
public class DataIntegrityValidationResult : Result<DataIntegrityValidationSummary>
{
    public DataIntegrityValidationSummary? ValidationSummary => IsSuccess ? Value : null;
    public decimal IntegrityScore => ValidationSummary?.IntegrityScore ?? 0m;
    public bool HasCorruption => ValidationSummary?.CorruptRecords > 0;

    private DataIntegrityValidationResult(bool isSuccess, IEnumerable<string> errors, DataIntegrityValidationSummary? value = null)
        : base(isSuccess, errors, value) { }

    public static new DataIntegrityValidationResult Success(DataIntegrityValidationSummary summary)
        => new(true, Array.Empty<string>(), summary);

    public static new DataIntegrityValidationResult Failure(string error)
        => new(false, new[] { error });

    public static new DataIntegrityValidationResult Failure(IEnumerable<string> errors)
        => new(false, errors);
}

/// <summary>
/// Summary of data integrity validation results
/// </summary>
public class DataIntegrityValidationSummary
{
    public long TotalRecordsChecked { get; init; }
    public long CorruptRecords { get; init; }
    public decimal IntegrityScore { get; init; }
    public long ValidationDurationMs { get; init; }
    public DateTime ValidationTimestamp { get; init; } = DateTime.UtcNow;
    public string? ValidationScope { get; init; }
    public List<string> CorruptionDetails { get; init; } = new();
}

#endregion

#region Backup Verification Results

/// <summary>
/// Result type for backup verification operations
/// </summary>
public class BackupVerificationResult : Result<BackupVerificationSummary>
{
    public BackupVerificationSummary? VerificationSummary => IsSuccess ? Value : null;
    public bool IsVerified => VerificationSummary?.VerificationPassed ?? false;
    public decimal BackupSizeGB => VerificationSummary?.BackupSizeGB ?? 0m;

    private BackupVerificationResult(bool isSuccess, IEnumerable<string> errors, BackupVerificationSummary? value = null)
        : base(isSuccess, errors, value) { }

    public static new BackupVerificationResult Success(BackupVerificationSummary summary)
        => new(true, Array.Empty<string>(), summary);

    public static new BackupVerificationResult Failure(string error)
        => new(false, new[] { error });

    public static new BackupVerificationResult Failure(IEnumerable<string> errors)
        => new(false, errors);
}

/// <summary>
/// Summary of backup verification results
/// </summary>
public class BackupVerificationSummary
{
    public required string BackupPath { get; init; }
    public bool VerificationPassed { get; init; }
    public decimal BackupSizeGB { get; init; }
    public bool ChecksumValid { get; init; }
    public long RecordCount { get; init; }
    public long VerificationDurationMs { get; init; }
    public DateTime BackupTimestamp { get; init; } = DateTime.UtcNow;
    public string? BackupType { get; init; }
    public List<string> VerificationDetails { get; init; } = new();
}

#endregion

#region Consistency Validation Results

/// <summary>
/// Result type for cross-region consistency validation operations
/// </summary>
public class ConsistencyValidationResult : Result<ConsistencyValidationSummary>
{
    public ConsistencyValidationSummary? ConsistencySummary => IsSuccess ? Value : null;
    public bool IsConsistent => ConsistencySummary?.CrossRegionConsistency ?? false;
    public TimeSpan ReplicationLag => ConsistencySummary?.ReplicationLag ?? TimeSpan.Zero;

    private ConsistencyValidationResult(bool isSuccess, IEnumerable<string> errors, ConsistencyValidationSummary? value = null)
        : base(isSuccess, errors, value) { }

    public static new ConsistencyValidationResult Success(ConsistencyValidationSummary summary)
        => new(true, Array.Empty<string>(), summary);

    public static new ConsistencyValidationResult Failure(string error)
        => new(false, new[] { error });

    public static new ConsistencyValidationResult Failure(IEnumerable<string> errors)
        => new(false, errors);
}

/// <summary>
/// Summary of cross-region consistency validation results
/// </summary>
public class ConsistencyValidationSummary
{
    public bool CrossRegionConsistency { get; init; }
    public TimeSpan ReplicationLag { get; init; }
    public int ConsistentRegions { get; init; }
    public int InconsistentRegions { get; init; }
    public long ValidationDurationMs { get; init; }
    public DateTime ValidationTimestamp { get; init; } = DateTime.UtcNow;
    public List<string> RegionDetails { get; init; } = new();
    public List<string> InconsistencyDetails { get; init; } = new();
}

#endregion

#region Integration Results

/// <summary>
/// Result type for integrity monitoring operations
/// </summary>
public class IntegrityMonitoringResult : Result<object>
{
    private IntegrityMonitoringResult(bool isSuccess, IEnumerable<string> errors, object? value = null)
        : base(isSuccess, errors, value) { }

    public static new IntegrityMonitoringResult Success()
        => new(true, Array.Empty<string>());

    public static new IntegrityMonitoringResult Failure(string error)
        => new(false, new[] { error });
}

/// <summary>
/// Result type for community data integrity operations
/// </summary>
public class CommunityDataIntegrityResult : Result<object>
{
    private CommunityDataIntegrityResult(bool isSuccess, IEnumerable<string> errors, object? value = null)
        : base(isSuccess, errors, value) { }

    public static new CommunityDataIntegrityResult Success()
        => new(true, Array.Empty<string>());

    public static new CommunityDataIntegrityResult Failure(string error)
        => new(false, new[] { error });
}

/// <summary>
/// Result type for checksum validation operations
/// </summary>
public class ChecksumValidationResult : Result<object>
{
    private ChecksumValidationResult(bool isSuccess, IEnumerable<string> errors, object? value = null)
        : base(isSuccess, errors, value) { }

    public static new ChecksumValidationResult Success()
        => new(true, Array.Empty<string>());

    public static new ChecksumValidationResult Failure(string error)
        => new(false, new[] { error });
}

#endregion