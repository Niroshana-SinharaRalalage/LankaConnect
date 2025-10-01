using FluentAssertions;
using LankaConnect.Application.Common.DisasterRecovery;
using LankaConnect.Domain.Common;
using Xunit;

namespace LankaConnect.Application.Tests.Common.DisasterRecovery;

/// <summary>
/// TDD RED Phase: Disaster Recovery Result Types Tests
/// Testing comprehensive disaster recovery result patterns for Cultural Intelligence platform
/// </summary>
public class DisasterRecoveryResultTypesTests
{
    #region DataIntegrityValidationResult Tests (RED Phase)

    [Fact]
    public void DataIntegrityValidationResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var validationSummary = new DataIntegrityValidationSummary
        {
            TotalRecordsChecked = 10000,
            CorruptRecords = 0,
            IntegrityScore = 100.0m,
            ValidationDurationMs = 5000
        };

        // Act
        var result = DataIntegrityValidationResult.Success(validationSummary);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ValidationSummary.Should().Be(validationSummary);
        result.IntegrityScore.Should().Be(100.0m);
        result.HasCorruption.Should().BeFalse();
    }

    [Fact]
    public void DataIntegrityValidationResult_CreateFailure_ShouldReturnFailedResult()
    {
        // Arrange
        var error = "Critical data corruption detected in Cultural Events collection";

        // Act
        var result = DataIntegrityValidationResult.Failure(error);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.ValidationSummary.Should().BeNull();
    }

    [Fact]
    public void DataIntegrityValidationResult_WithCorruption_ShouldIndicateIssues()
    {
        // Arrange
        var corruptionSummary = new DataIntegrityValidationSummary
        {
            TotalRecordsChecked = 10000,
            CorruptRecords = 150,
            IntegrityScore = 85.0m,
            ValidationDurationMs = 7500
        };

        // Act
        var result = DataIntegrityValidationResult.Success(corruptionSummary);

        // Assert
        result.HasCorruption.Should().BeTrue();
        result.IntegrityScore.Should().Be(85.0m);
        result.ValidationSummary.CorruptRecords.Should().Be(150);
    }

    #endregion

    #region BackupVerificationResult Tests (RED Phase)

    [Fact]
    public void BackupVerificationResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var verificationSummary = new BackupVerificationSummary
        {
            BackupPath = "/backup/cultural-events-20241213.bak",
            VerificationPassed = true,
            BackupSizeGB = 45.7m,
            ChecksumValid = true,
            RecordCount = 500000,
            VerificationDurationMs = 12000
        };

        // Act
        var result = BackupVerificationResult.Success(verificationSummary);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.VerificationSummary.Should().Be(verificationSummary);
        result.IsVerified.Should().BeTrue();
        result.BackupSizeGB.Should().Be(45.7m);
    }

    [Fact]
    public void BackupVerificationResult_WithChecksumFailure_ShouldReturnFailure()
    {
        // Arrange
        var error = "Backup checksum validation failed - potential corruption";

        // Act
        var result = BackupVerificationResult.Failure(error);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.IsVerified.Should().BeFalse();
        result.VerificationSummary.Should().BeNull();
    }

    [Fact]
    public void BackupVerificationResult_CanChainWithDataIntegrity_ShouldWork()
    {
        // Arrange
        var backupResult = BackupVerificationResult.Success(new BackupVerificationSummary
        {
            BackupPath = "/backup/test.bak",
            VerificationPassed = true,
            ChecksumValid = true,
            RecordCount = 1000,
            VerificationDurationMs = 5000
        });

        var integrityResult = DataIntegrityValidationResult.Success(new DataIntegrityValidationSummary
        {
            TotalRecordsChecked = 1000,
            CorruptRecords = 0,
            IntegrityScore = 100.0m,
            ValidationDurationMs = 3000
        });

        // Act
        var combinedResult = CombineResults(backupResult, integrityResult);

        // Assert
        combinedResult.Should().BeTrue();
    }

    #endregion

    #region ConsistencyValidationResult Tests (RED Phase)

    [Fact]
    public void ConsistencyValidationResult_CreateSuccess_ShouldReturnValidResult()
    {
        // Arrange
        var consistencySummary = new ConsistencyValidationSummary
        {
            CrossRegionConsistency = true,
            ReplicationLag = TimeSpan.FromSeconds(2),
            ConsistentRegions = 5,
            InconsistentRegions = 0,
            ValidationDurationMs = 8000
        };

        // Act
        var result = ConsistencyValidationResult.Success(consistencySummary);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ConsistencySummary.Should().Be(consistencySummary);
        result.IsConsistent.Should().BeTrue();
        result.ReplicationLag.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void ConsistencyValidationResult_WithInconsistency_ShouldIndicateIssues()
    {
        // Arrange
        var inconsistencySummary = new ConsistencyValidationSummary
        {
            CrossRegionConsistency = false,
            ReplicationLag = TimeSpan.FromMinutes(5),
            ConsistentRegions = 3,
            InconsistentRegions = 2,
            ValidationDurationMs = 15000
        };

        // Act
        var result = ConsistencyValidationResult.Success(inconsistencySummary);

        // Assert
        result.IsConsistent.Should().BeFalse();
        result.ConsistencySummary.InconsistentRegions.Should().Be(2);
        result.ReplicationLag.Should().Be(TimeSpan.FromMinutes(5));
    }

    #endregion

    private bool CombineResults(BackupVerificationResult backup, DataIntegrityValidationResult integrity)
    {
        return backup.IsSuccess && integrity.IsSuccess && backup.IsVerified && !integrity.HasCorruption;
    }
}