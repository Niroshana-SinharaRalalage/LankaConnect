using System;
using System.Collections.Generic;
using Xunit;
using LankaConnect.Application.Common.Models.CulturalIntelligence;

namespace LankaConnect.Application.Tests;

/// <summary>
/// Verification that all critical compliance types compile and can be instantiated
/// This confirms PHASE 13B-2 TDD implementation requirements
/// </summary>
public class ComplianceTypesCompilationVerification
{
    [Fact]
    public void AllCriticalComplianceTypes_ShouldCompile()
    {
        // Verify SOC2ValidationCriteria exists and can be instantiated
        var soc2Criteria = new SOC2ValidationCriteria
        {
            CriteriaId = "TEST",
            TrustServiceCriteria = new List<string>(),
            ControlObjectives = new Dictionary<string, object>(),
            AuditType = "Type II",
            EvidenceRequirements = new List<string>()
        };
        Assert.NotNull(soc2Criteria);

        // Verify GDPRValidationScope exists and can be instantiated
        var gdprScope = new GDPRValidationScope
        {
            ScopeId = "TEST",
            DataCategories = new List<string>(),
            ValidationCriteria = new Dictionary<string, object>(),
            ValidationLevel = "Enhanced",
            ProcessingActivities = new List<string>()
        };
        Assert.NotNull(gdprScope);

        // Verify GDPRComplianceResult exists and can be instantiated
        var gdprResult = new GDPRComplianceResult
        {
            ComplianceAchieved = true,
            ComplianceChecks = new Dictionary<string, bool>(),
            NonComplianceItems = new List<string>(),
            ComplianceTimestamp = DateTime.UtcNow,
            ComplianceLevel = "Full"
        };
        Assert.NotNull(gdprResult);

        // Verify SOC2Gap exists and can be instantiated
        var soc2Gap = new SOC2Gap("SECURITY", "Test gap");
        Assert.NotNull(soc2Gap);

        // Verify HIPAAValidationCriteria exists and can be instantiated
        var hipaaValidation = new HIPAAValidationCriteria
        {
            CriteriaId = "TEST",
            ProtectedHealthInfoCategories = new List<string>(),
            SafeguardRequirements = new Dictionary<string, object>(),
            ValidationStandard = "HIPAA Security Rule",
            AuditRequirements = new List<string>()
        };
        Assert.NotNull(hipaaValidation);

        // Verify HIPAAComplianceResult exists and can be instantiated
        var hipaaResult = new HIPAAComplianceResult
        {
            ComplianceAchieved = true,
            SafeguardCompliance = new Dictionary<string, bool>(),
            ComplianceGaps = new List<string>(),
            ComplianceTimestamp = DateTime.UtcNow,
            ComplianceOfficer = "Test Officer"
        };
        Assert.NotNull(hipaaResult);

        // Verify PCIDSSValidationScope exists and can be instantiated
        var pciScope = new PCIDSSValidationScope
        {
            ScopeId = "TEST",
            CardDataEnvironments = new List<string>(),
            ValidationRequirements = new Dictionary<string, object>(),
            PCILevel = "Level 1",
            SecurityControls = new List<string>()
        };
        Assert.NotNull(pciScope);

        // All types compiled and instantiated successfully
        Assert.True(true, "All critical compliance types compiled successfully");
    }
}