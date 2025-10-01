using System;
using System.Collections.Generic;
using Xunit;
using LankaConnect.Domain.Common.Security;

namespace LankaConnect.Domain.Tests.Common.Security
{
    public class RegionalSecurityTypesTests
    {
        [Fact]
        public void RegionalSecurityImplementation_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var implementation = new RegionalSecurityImplementation();

            // Assert
            Assert.NotEqual(Guid.Empty, implementation.Id);
            Assert.NotNull(implementation.ImplementationId);
            Assert.NotNull(implementation.RegionId);
            Assert.NotNull(implementation.SecurityLevel);
            Assert.NotNull(implementation.SecurityPolicies);
            Assert.NotNull(implementation.ComplianceFrameworks);
            Assert.NotNull(implementation.EncryptionSettings);
            Assert.NotNull(implementation.AccessControls);
            Assert.NotNull(implementation.ImplementationStatus);
            Assert.True(implementation.ImplementedAt <= DateTimeOffset.UtcNow);
        }

        [Fact]
        public void DataSovereigntySecurityResult_ShouldCreateInstanceWithDefaultValues()
        {
            // Act
            var result = new DataSovereigntySecurityResult();

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.NotNull(result.ResultId);
            Assert.NotNull(result.DataSovereigntyRequirements);
            Assert.NotNull(result.Implementation);
            Assert.NotNull(result.ComplianceViolations);
            Assert.NotNull(result.SecurityMetrics);
            Assert.NotNull(result.RecommendedActions);
            Assert.NotNull(result.ComplianceStatus);
            Assert.True(result.ValidatedAt <= DateTimeOffset.UtcNow);
        }

        [Theory]
        [InlineData("us-east-1")]
        [InlineData("eu-west-1")]
        [InlineData("ap-southeast-1")]
        public void RegionalSecurityImplementation_ShouldAcceptValidRegionId(string regionId)
        {
            // Arrange
            var implementation = new RegionalSecurityImplementation();

            // Act
            implementation.RegionId = regionId;

            // Assert
            Assert.Equal(regionId, implementation.RegionId);
        }

        [Theory]
        [InlineData("High")]
        [InlineData("Medium")]
        [InlineData("Low")]
        public void RegionalSecurityImplementation_ShouldAcceptValidSecurityLevel(string securityLevel)
        {
            // Arrange
            var implementation = new RegionalSecurityImplementation();

            // Act
            implementation.SecurityLevel = securityLevel;

            // Assert
            Assert.Equal(securityLevel, implementation.SecurityLevel);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RegionalSecurityImplementation_ShouldReflectImplementationStatus(bool isImplemented)
        {
            // Arrange
            var implementation = new RegionalSecurityImplementation();

            // Act
            implementation.IsImplemented = isImplemented;

            // Assert
            Assert.Equal(isImplemented, implementation.IsImplemented);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DataSovereigntySecurityResult_ShouldReflectComplianceStatus(bool isCompliant)
        {
            // Arrange
            var result = new DataSovereigntySecurityResult();

            // Act
            result.IsCompliant = isCompliant;

            // Assert
            Assert.Equal(isCompliant, result.IsCompliant);
        }

        [Fact]
        public void RegionalSecurityImplementation_ShouldSupportMultipleComplianceFrameworks()
        {
            // Arrange
            var implementation = new RegionalSecurityImplementation();
            var frameworks = new List<string> { "GDPR", "SOC2", "ISO27001" };

            // Act
            implementation.ComplianceFrameworks = frameworks;

            // Assert
            Assert.Equal(frameworks.Count, implementation.ComplianceFrameworks.Count);
            Assert.Contains("GDPR", implementation.ComplianceFrameworks);
            Assert.Contains("SOC2", implementation.ComplianceFrameworks);
            Assert.Contains("ISO27001", implementation.ComplianceFrameworks);
        }

        [Fact]
        public void RegionalSecurityImplementation_ShouldSupportMultipleAccessControls()
        {
            // Arrange
            var implementation = new RegionalSecurityImplementation();
            var accessControls = new List<string> { "RBAC", "ABAC", "MFA" };

            // Act
            implementation.AccessControls = accessControls;

            // Assert
            Assert.Equal(accessControls.Count, implementation.AccessControls.Count);
            Assert.Contains("RBAC", implementation.AccessControls);
            Assert.Contains("ABAC", implementation.AccessControls);
            Assert.Contains("MFA", implementation.AccessControls);
        }

        [Fact]
        public void DataSovereigntySecurityResult_ShouldSupportMultipleViolations()
        {
            // Arrange
            var result = new DataSovereigntySecurityResult();
            var violations = new List<string> { "Data residency violation", "Encryption non-compliance" };

            // Act
            result.ComplianceViolations = violations;

            // Assert
            Assert.Equal(violations.Count, result.ComplianceViolations.Count);
            Assert.Contains("Data residency violation", result.ComplianceViolations);
            Assert.Contains("Encryption non-compliance", result.ComplianceViolations);
        }

        [Theory]
        [InlineData("AES-256")]
        [InlineData("ChaCha20-Poly1305")]
        public void RegionalSecurityImplementation_ShouldSupportEncryptionSettings(string encryptionType)
        {
            // Arrange
            var implementation = new RegionalSecurityImplementation();

            // Act
            implementation.EncryptionSettings["algorithm"] = encryptionType;

            // Assert
            Assert.Contains("algorithm", implementation.EncryptionSettings);
            Assert.Equal(encryptionType, implementation.EncryptionSettings["algorithm"]);
        }

        [Fact]
        public void DataSovereigntySecurityResult_ShouldSupportSecurityMetrics()
        {
            // Arrange
            var result = new DataSovereigntySecurityResult();

            // Act
            result.SecurityMetrics["encryption_coverage"] = 0.95;
            result.SecurityMetrics["compliance_score"] = 0.88;

            // Assert
            Assert.Contains("encryption_coverage", result.SecurityMetrics);
            Assert.Contains("compliance_score", result.SecurityMetrics);
            Assert.Equal(0.95, result.SecurityMetrics["encryption_coverage"]);
            Assert.Equal(0.88, result.SecurityMetrics["compliance_score"]);
        }
    }
}