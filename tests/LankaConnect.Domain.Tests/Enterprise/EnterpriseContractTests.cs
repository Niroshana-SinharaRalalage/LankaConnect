using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Enterprise.ValueObjects;
using LankaConnect.Domain.Enterprise.DomainEvents;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using FluentAssertions;
using Xunit;

namespace LankaConnect.Domain.Tests.Enterprise;

/// <summary>
/// TDD Tests for Enterprise Contract aggregate root
/// These tests define the behavior for Fortune 500 enterprise contracts with Cultural Intelligence SLA guarantees
/// </summary>
public class EnterpriseContractTests
{
    #region Fortune 500 Contract Creation Tests

    [Fact]
    public void CreateFortune500Contract_WithValidParameters_ShouldCreateContractSuccessfully()
    {
        // Arrange
        var companyName = "Google LLC";
        var primaryContactEmail = "enterprise@google.com";
        var annualValue = 750000m; // $750K annual value
        var startDate = DateTime.UtcNow.AddDays(1);
        var culturalRequirements = "Buddhist calendar integration, Silicon Valley diaspora analytics";

        // Act
        var contract = EnterpriseContract.CreateFortune500Contract(
            companyName,
            primaryContactEmail,
            annualValue,
            startDate,
            contractTermMonths: 36,
            culturalRequirements);

        // Assert
        contract.Should().NotBeNull();
        contract.CompanyName.Should().Be(companyName);
        contract.PrimaryContactEmail.Should().Be(primaryContactEmail);
        contract.ContractValue.AnnualValue.Amount.Should().Be(annualValue);
        contract.ContractValue.IsEnterpriseTier.Should().BeTrue();
        contract.StartDate.Should().Be(startDate);
        contract.EndDate.Should().Be(startDate.AddMonths(36));
        contract.Status.Should().Be(ContractStatus.Draft);
        contract.AutoRenewal.Should().BeTrue(); // Fortune 500 contracts auto-renew
        contract.CulturalRequirements.Should().Be(culturalRequirements);
        
        // Verify SLA requirements for Fortune 500
        contract.SLARequirements.Should().NotBeNull();
        contract.SLARequirements.UptimeGuaranteePercentage.Should().Be(99.95);
        contract.SLARequirements.MaxResponseTime.Should().Be(TimeSpan.FromMilliseconds(200));
        contract.SLARequirements.IsEnterprise.Should().BeTrue();
        
        // Verify Cultural Intelligence features
        contract.FeatureAccess.Should().NotBeNull();
        contract.FeatureAccess.IsEnterpriseTier.Should().BeTrue();
        contract.FeatureAccess.BuddhistCalendarPremium.Should().BeTrue();
        contract.FeatureAccess.HinduCalendarPremium.Should().BeTrue();
        contract.FeatureAccess.CulturalAppropriatenessScoring.Should().BeTrue();
        contract.FeatureAccess.DiasporaAnalyticsEnterprise.Should().BeTrue();
        contract.FeatureAccess.CustomAIModelDevelopment.Should().BeTrue();
        contract.FeatureAccess.WhiteLabelLicensing.Should().BeTrue();
        contract.FeatureAccess.CulturalConsultingServices.Should().BeTrue();
        
        // Verify Usage Limits
        contract.UsageLimits.Should().NotBeNull();
        contract.UsageLimits.IsUnlimited.Should().BeTrue();
        contract.UsageLimits.HasPremiumCalendarAccess.Should().BeTrue();
        contract.UsageLimits.HasEnterpriseAnalytics.Should().BeTrue();
        contract.UsageLimits.IncludesConsulting.Should().BeTrue();
        contract.UsageLimits.ConsultingHoursIncluded.Should().Be(100);
        
        // Verify domain event
        contract.DomainEvents.Should().HaveCount(1);
        var domainEvent = contract.DomainEvents.First() as EnterpriseContractCreatedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent.CompanyName.Should().Be(companyName);
        domainEvent.AnnualValue.Should().Be(annualValue);
    }

    [Fact]
    public void CreateFortune500Contract_WithInvalidAnnualValue_ShouldThrowException()
    {
        // Arrange
        var companyName = "Test Company";
        var primaryContactEmail = "test@company.com";
        var invalidAnnualValue = 400000m; // Below $500K minimum for Fortune 500
        var startDate = DateTime.UtcNow.AddDays(1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            EnterpriseContract.CreateFortune500Contract(
                companyName,
                primaryContactEmail,
                invalidAnnualValue,
                startDate));
                
        exception.Message.Should().Contain("Fortune 500 contracts must have minimum $500K annual value");
    }

    [Theory]
    [InlineData(null, "test@company.com")]
    [InlineData("", "test@company.com")]
    [InlineData("   ", "test@company.com")]
    [InlineData("Valid Company", null)]
    [InlineData("Valid Company", "")]
    [InlineData("Valid Company", "   ")]
    public void CreateFortune500Contract_WithInvalidCompanyNameOrEmail_ShouldThrowException(
        string companyName, string primaryContactEmail)
    {
        // Arrange
        var annualValue = 750000m;
        var startDate = DateTime.UtcNow.AddDays(1);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            EnterpriseContract.CreateFortune500Contract(
                companyName,
                primaryContactEmail,
                annualValue,
                startDate));
                
        exception.Should().NotBeNull();
    }

    #endregion

    #region Educational Institution Contract Tests

    [Fact]
    public void CreateEducationalInstitutionContract_WithValidParameters_ShouldCreateContractSuccessfully()
    {
        // Arrange
        var institutionName = "Stanford University";
        var primaryContactEmail = "cultural.studies@stanford.edu";
        var annualValue = 150000m;
        var startDate = DateTime.UtcNow.AddDays(1);
        var culturalRequirements = "Student cultural community support, academic research access";

        // Act
        var contract = EnterpriseContract.CreateEducationalInstitutionContract(
            institutionName,
            primaryContactEmail,
            annualValue,
            startDate,
            culturalRequirements);

        // Assert
        contract.Should().NotBeNull();
        contract.CompanyName.Should().Be(institutionName);
        contract.ContractValue.Tier.Should().Be(ContractTier.Educational);
        contract.EndDate.Should().Be(startDate.AddMonths(12)); // 1-year term for educational
        contract.AutoRenewal.Should().BeFalse(); // Educational contracts don't auto-renew
        
        // Educational institutions get premium SLA, not enterprise
        contract.SLARequirements.UptimeGuaranteePercentage.Should().Be(99.9);
        contract.SLARequirements.MaxResponseTime.Should().Be(TimeSpan.FromMilliseconds(500));
        
        // Educational feature set
        contract.FeatureAccess.BuddhistCalendarPremium.Should().BeTrue();
        contract.FeatureAccess.HinduCalendarPremium.Should().BeTrue();
        contract.FeatureAccess.CulturalAppropriatenessScoring.Should().BeTrue();
        contract.FeatureAccess.CulturalConsultingServices.Should().BeTrue();
        contract.FeatureAccess.DiasporaAnalyticsEnterprise.Should().BeFalse(); // Not included for educational
        contract.FeatureAccess.CustomAIModelDevelopment.Should().BeFalse(); // Not included for educational
        contract.FeatureAccess.WhiteLabelLicensing.Should().BeFalse(); // Not included for educational
        
        // Educational usage limits
        contract.UsageLimits.ConsultingHoursIncluded.Should().Be(50);
        contract.UsageLimits.DataRetentionMonths.Should().Be(60); // 5 years for research
    }

    #endregion

    #region Government Agency Contract Tests

    [Fact]
    public void CreateGovernmentAgencyContract_WithValidParameters_ShouldCreateContractSuccessfully()
    {
        // Arrange
        var agencyName = "Department of Cultural Affairs";
        var primaryContactEmail = "cultural.analytics@gov.agency";
        var annualValue = 300000m;
        var startDate = DateTime.UtcNow.AddDays(1);
        var culturalRequirements = "Census cultural analytics, community service optimization";

        // Act
        var contract = EnterpriseContract.CreateGovernmentAgencyContract(
            agencyName,
            primaryContactEmail,
            annualValue,
            startDate,
            culturalRequirements);

        // Assert
        contract.Should().NotBeNull();
        contract.CompanyName.Should().Be(agencyName);
        contract.ContractValue.Tier.Should().Be(ContractTier.Government);
        contract.EndDate.Should().Be(startDate.AddMonths(60)); // 5-year government contract
        contract.AutoRenewal.Should().BeFalse(); // Government contracts don't auto-renew
        
        // Government requires enterprise-grade SLA
        contract.SLARequirements.UptimeGuaranteePercentage.Should().Be(99.95);
        contract.SLARequirements.MaxResponseTime.Should().Be(TimeSpan.FromMilliseconds(200));
        
        // Government feature set
        contract.FeatureAccess.BuddhistCalendarPremium.Should().BeTrue();
        contract.FeatureAccess.HinduCalendarPremium.Should().BeTrue();
        contract.FeatureAccess.CulturalAppropriatenessScoring.Should().BeTrue();
        contract.FeatureAccess.DiasporaAnalyticsEnterprise.Should().BeTrue();
        contract.FeatureAccess.CulturalConsultingServices.Should().BeTrue();
        contract.FeatureAccess.CulturalTrendPrediction.Should().BeTrue();
        contract.FeatureAccess.CustomAIModelDevelopment.Should().BeFalse(); // Not included for government
        contract.FeatureAccess.WhiteLabelLicensing.Should().BeFalse(); // Not included for government
        
        // Government usage limits
        contract.UsageLimits.ConsultingHoursIncluded.Should().Be(75);
        contract.UsageLimits.DataRetentionMonths.Should().Be(120); // 10 years for compliance
        contract.UsageLimits.DiasporaAnalyticsReportsPerMonth.Should().Be(200);
    }

    #endregion

    #region Contract Activation Tests

    [Fact]
    public void Activate_WithValidatedCompliance_ShouldActivateContractSuccessfully()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        contract.ValidateCompliance(true, "SOC 2 Type II compliance validated");

        // Act
        contract.Activate();

        // Assert
        contract.Status.Should().Be(ContractStatus.Active);
        contract.NextBillingDate.Should().NotBeNull();
        contract.NextBillingDate.Should().Be(contract.StartDate.AddMonths(1));
        
        // Check domain events
        var activationEvent = contract.DomainEvents
            .OfType<EnterpriseContractActivatedEvent>()
            .FirstOrDefault();
        activationEvent.Should().NotBeNull();
        activationEvent.CompanyName.Should().Be(contract.CompanyName);
    }

    [Fact]
    public void Activate_WithoutComplianceValidation_ShouldThrowException()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        // Not calling ValidateCompliance, so ComplianceValidated remains false

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => contract.Activate());
        exception.Message.Should().Contain("must be compliance validated before activation");
    }

    [Fact]
    public void Activate_FromInvalidStatus_ShouldThrowException()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        contract.ValidateCompliance(true);
        contract.Activate(); // First activation

        // Act & Assert - trying to activate again
        var exception = Assert.Throws<InvalidOperationException>(() => contract.Activate());
        exception.Message.Should().Contain("can only be activated from Draft or PendingApproval status");
    }

    #endregion

    #region API Usage Recording Tests

    [Fact]
    public void RecordAPIUsage_WithActiveContract_ShouldUpdateUsageSuccessfully()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        var apiCallsUsed = 1000;
        var endpoint = "/cultural/buddhist-calendar";
        var usageDate = DateTime.UtcNow;

        // Act
        contract.RecordAPIUsage(apiCallsUsed, endpoint, usageDate);

        // Assert
        contract.CurrentMonthAPIUsage.Should().Be(apiCallsUsed);
        contract.UpdatedAt.Should().NotBeNull();
        contract.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));
        
        // Check domain event
        var usageEvent = contract.DomainEvents
            .OfType<EnterpriseAPIUsageRecordedEvent>()
            .FirstOrDefault();
        usageEvent.Should().NotBeNull();
        usageEvent.APICallsUsed.Should().Be(apiCallsUsed);
        usageEvent.Endpoint.Should().Be(endpoint);
        usageEvent.UsageDate.Should().Be(usageDate);
    }

    [Fact]
    public void RecordAPIUsage_ExceedingQuota_ShouldRaiseQuotaExceededEvent()
    {
        // Arrange - Create mid-market contract with limited usage
        var contract = CreateMidMarketContractWithLimitedUsage();
        ActivateContract(contract);
        
        var quotaLimit = contract.UsageLimits.MonthlyAPIRequests;
        var excessiveUsage = quotaLimit + 1000;

        // Act
        contract.RecordAPIUsage(excessiveUsage, "/cultural/appropriateness", DateTime.UtcNow);

        // Assert
        contract.CurrentMonthAPIUsage.Should().Be(excessiveUsage);
        
        // Check quota exceeded event
        var quotaEvent = contract.DomainEvents
            .OfType<EnterpriseUsageQuotaExceededEvent>()
            .FirstOrDefault();
        quotaEvent.Should().NotBeNull();
        quotaEvent.CurrentUsage.Should().Be(excessiveUsage);
        quotaEvent.QuotaLimit.Should().Be(quotaLimit);
    }

    [Fact]
    public void RecordAPIUsage_WithInactiveContract_ShouldThrowException()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        // Contract is not activated, so it's inactive

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            contract.RecordAPIUsage(100, "/cultural/test", DateTime.UtcNow));
        exception.Message.Should().Contain("Cannot record usage for inactive contracts");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void RecordAPIUsage_WithInvalidUsageCount_ShouldThrowException(int invalidUsage)
    {
        // Arrange
        var contract = CreateAndActivateContract();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            contract.RecordAPIUsage(invalidUsage, "/cultural/test", DateTime.UtcNow));
        exception.ParamName.Should().Be("apiCallsUsed");
    }

    #endregion

    #region Consulting Usage Recording Tests

    [Fact]
    public void RecordConsultingUsage_WithActiveContractAndConsultingFeature_ShouldUpdateUsageSuccessfully()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        var hoursUsed = 10;
        var consultingType = "Cultural Appropriateness Review";
        var usageDate = DateTime.UtcNow;

        // Act
        contract.RecordConsultingUsage(hoursUsed, consultingType, usageDate);

        // Assert
        contract.ConsultingHoursUsed.Should().Be(hoursUsed);
        contract.RemainingConsultingHours.Should().Be(90); // 100 included - 10 used
        
        // Check domain event
        var consultingEvent = contract.DomainEvents
            .OfType<EnterpriseConsultingUsageRecordedEvent>()
            .FirstOrDefault();
        consultingEvent.Should().NotBeNull();
        consultingEvent.HoursUsed.Should().Be(hoursUsed);
        consultingEvent.ConsultingType.Should().Be(consultingType);
    }

    [Fact]
    public void RecordConsultingUsage_ExceedingIncludedHours_ShouldRaiseOverageEvent()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        var includedHours = contract.UsageLimits.ConsultingHoursIncluded;
        var excessiveHours = includedHours + 25; // 25 hours overage

        // Act
        contract.RecordConsultingUsage(excessiveHours, "Extended Cultural Analysis", DateTime.UtcNow);

        // Assert
        contract.ConsultingHoursUsed.Should().Be(excessiveHours);
        contract.RemainingConsultingHours.Should().Be(0);
        
        // Check overage event
        var overageEvent = contract.DomainEvents
            .OfType<EnterpriseConsultingOverageEvent>()
            .FirstOrDefault();
        overageEvent.Should().NotBeNull();
        overageEvent.OverageHours.Should().Be(25);
    }

    [Fact]
    public void RecordConsultingUsage_WithoutConsultingFeature_ShouldThrowException()
    {
        // Arrange - Create contract without consulting services
        var contract = CreateContractWithoutConsulting();
        ActivateContract(contract);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            contract.RecordConsultingUsage(10, "Test Consulting", DateTime.UtcNow));
        exception.Message.Should().Contain("Consulting services not included in this contract");
    }

    #endregion

    #region Monthly Billing Tests

    [Fact]
    public void ProcessMonthlyBilling_OnSchedule_ShouldProcessBillingSuccessfully()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        
        // Fast-forward to billing date
        SetNextBillingDateToPast(contract);
        var expectedBillingDate = contract.NextBillingDate.Value;
        var expectedAmount = contract.ContractValue.MonthlyValue.Amount;

        // Act
        contract.ProcessMonthlyBilling();

        // Assert
        contract.LastBillingDate.Should().Be(expectedBillingDate);
        contract.NextBillingDate.Should().Be(expectedBillingDate.AddMonths(1));
        contract.CurrentMonthAPIUsage.Should().Be(0); // Reset after billing
        
        // Check domain event
        var billingEvent = contract.DomainEvents
            .OfType<EnterpriseMonthlyBillingProcessedEvent>()
            .FirstOrDefault();
        billingEvent.Should().NotBeNull();
        billingEvent.BillingAmount.Should().Be(expectedAmount);
        billingEvent.BillingDate.Should().Be(expectedBillingDate);
    }

    [Fact]
    public void ProcessMonthlyBilling_BeforeBillingDate_ShouldThrowException()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        // NextBillingDate is in the future by default

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            contract.ProcessMonthlyBilling());
        exception.Message.Should().Contain("Billing date has not arrived");
    }

    [Fact]
    public void ProcessMonthlyBilling_WithInactiveContract_ShouldThrowException()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        // Contract is not activated

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            contract.ProcessMonthlyBilling());
        exception.Message.Should().Contain("Cannot process billing for inactive contracts");
    }

    #endregion

    #region Compliance Validation Tests

    [Fact]
    public void ValidateCompliance_WithPassingCompliance_ShouldUpdateComplianceStatus()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        var complianceNotes = "SOC 2 Type II audit passed, GDPR compliance verified";

        // Act
        contract.ValidateCompliance(true, complianceNotes);

        // Assert
        contract.ComplianceValidated.Should().BeTrue();
        contract.UpdatedAt.Should().NotBeNull();
        
        // Check domain event
        var complianceEvent = contract.DomainEvents
            .OfType<EnterpriseComplianceValidatedEvent>()
            .FirstOrDefault();
        complianceEvent.Should().NotBeNull();
        complianceEvent.ComplianceNotes.Should().Be(complianceNotes);
    }

    [Fact]
    public void ValidateCompliance_WithFailingCompliance_ShouldSetComplianceFailedStatus()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        var failureReason = "Failed SOC 2 Type II audit - data encryption issues";

        // Act
        contract.ValidateCompliance(false, failureReason);

        // Assert
        contract.ComplianceValidated.Should().BeFalse();
        contract.Status.Should().Be(ContractStatus.ComplianceFailed);
        
        // Check domain event
        var failureEvent = contract.DomainEvents
            .OfType<EnterpriseComplianceFailedEvent>()
            .FirstOrDefault();
        failureEvent.Should().NotBeNull();
        failureEvent.FailureReason.Should().Be(failureReason);
    }

    #endregion

    #region Contract Suspension Tests

    [Fact]
    public void Suspend_WithActiveContract_ShouldSuspendSuccessfully()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        var suspensionReason = "SLA violations - response time exceeded 3 consecutive times";

        // Act
        contract.Suspend(suspensionReason);

        // Assert
        contract.Status.Should().Be(ContractStatus.Suspended);
        contract.UpdatedAt.Should().NotBeNull();
        
        // Check domain event
        var suspensionEvent = contract.DomainEvents
            .OfType<EnterpriseContractSuspendedEvent>()
            .FirstOrDefault();
        suspensionEvent.Should().NotBeNull();
        suspensionEvent.SuspensionReason.Should().Be(suspensionReason);
    }

    [Fact]
    public void Suspend_WithInactiveContract_ShouldThrowException()
    {
        // Arrange
        var contract = CreateValidFortune500Contract();
        // Contract is not activated

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            contract.Suspend("Test reason"));
        exception.Message.Should().Contain("Only active contracts can be suspended");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Suspend_WithInvalidReason_ShouldThrowException(string invalidReason)
    {
        // Arrange
        var contract = CreateAndActivateContract();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            contract.Suspend(invalidReason));
        exception.ParamName.Should().Be("reason");
    }

    #endregion

    #region Contract Renewal Tests

    [Fact]
    public void Renew_WithActiveContract_ShouldRenewSuccessfully()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        var currentEndDate = contract.EndDate;
        var newEndDate = currentEndDate.AddMonths(24); // Extend by 2 years
        var newContractValue = ContractValue.CreateFortune500Contract(
            new Money(850000m, Currency.USD)); // Increased annual value

        // Act
        contract.Renew(newEndDate, newContractValue);

        // Assert
        contract.EndDate.Should().Be(newEndDate);
        contract.ContractValue.AnnualValue.Amount.Should().Be(850000m);
        contract.Status.Should().Be(ContractStatus.Active);
        
        // Check domain event
        var renewalEvent = contract.DomainEvents
            .OfType<EnterpriseContractRenewedEvent>()
            .FirstOrDefault();
        renewalEvent.Should().NotBeNull();
        renewalEvent.NewEndDate.Should().Be(newEndDate);
        renewalEvent.NewAnnualValue.Should().Be(850000m);
    }

    [Fact]
    public void Renew_WithInvalidNewEndDate_ShouldThrowException()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        var invalidNewEndDate = contract.EndDate.AddDays(-1); // Before current end date

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            contract.Renew(invalidNewEndDate));
        exception.Message.Should().Contain("New end date must be after current end date");
    }

    #endregion

    #region Contract Termination Tests

    [Fact]
    public void Terminate_WithValidReason_ShouldTerminateSuccessfully()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        var terminationReason = "Client requested early termination";
        var terminationDate = DateTime.UtcNow.AddDays(30);

        // Act
        contract.Terminate(terminationReason, terminationDate);

        // Assert
        contract.Status.Should().Be(ContractStatus.Terminated);
        contract.EndDate.Should().Be(terminationDate);
        
        // Check domain event
        var terminationEvent = contract.DomainEvents
            .OfType<EnterpriseContractTerminatedEvent>()
            .FirstOrDefault();
        terminationEvent.Should().NotBeNull();
        terminationEvent.TerminationReason.Should().Be(terminationReason);
        terminationEvent.TerminationDate.Should().Be(terminationDate);
    }

    [Fact]
    public void Terminate_AlreadyTerminated_ShouldThrowException()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        contract.Terminate("First termination", DateTime.UtcNow.AddDays(30));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => 
            contract.Terminate("Second termination", DateTime.UtcNow.AddDays(45)));
        exception.Message.Should().Contain("Contract is already terminated");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Terminate_WithInvalidReason_ShouldThrowException(string invalidReason)
    {
        // Arrange
        var contract = CreateAndActivateContract();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            contract.Terminate(invalidReason));
        exception.ParamName.Should().Be("reason");
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void IsExpiring_WhenContractExpiresWithin30Days_ShouldReturnTrue()
    {
        // Arrange
        var contract = CreateAndActivateContract();
        SetContractToExpireSoon(contract);

        // Act & Assert
        contract.IsExpiring.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WithActiveContractInValidDateRange_ShouldReturnTrue()
    {
        // Arrange
        var contract = CreateAndActivateContract();

        // Act & Assert
        contract.IsActive.Should().BeTrue();
        contract.RequiresSLAMonitoring.Should().BeTrue();
        contract.IncludesCulturalConsulting.Should().BeTrue();
        contract.SupportsWhiteLabel.Should().BeTrue();
    }

    [Fact]
    public void APIUsagePercentage_WithLimitedUsage_ShouldCalculateCorrectly()
    {
        // Arrange
        var contract = CreateMidMarketContractWithLimitedUsage();
        ActivateContract(contract);
        
        var quotaLimit = contract.UsageLimits.MonthlyAPIRequests;
        var currentUsage = quotaLimit / 2; // 50% usage
        contract.RecordAPIUsage(currentUsage, "/cultural/test", DateTime.UtcNow);

        // Act & Assert
        contract.APIUsagePercentage.Should().BeApproximately(50.0, 0.1);
    }

    [Fact]
    public void APIUsagePercentage_WithUnlimitedUsage_ShouldReturnZero()
    {
        // Arrange
        var contract = CreateAndActivateContract(); // Fortune 500 has unlimited usage
        contract.RecordAPIUsage(1000000, "/cultural/test", DateTime.UtcNow);

        // Act & Assert
        contract.APIUsagePercentage.Should().Be(0);
    }

    #endregion

    #region Factory Pattern Tests (TDD)

    [Fact]
    public void Create_WithValidParameters_ShouldCreateEnterpriseContractSuccessfully()
    {
        // Arrange
        var clientId = new EnterpriseClientId();
        var slaRequirements = SLARequirements.CreateEnterpriseSLA();
        var featureAccess = CulturalIntelligenceFeatures.CreateFortune500FeatureSet();
        var usageLimits = EnterpriseUsageLimits.CreateFortune500Limits();
        var contractValue = ContractValue.CreateFortune500Contract(
            Domain.Shared.ValueObjects.Money.Create(750000m, Domain.Shared.Enums.Currency.USD).Value);
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddMonths(36);
        var companyName = "Test Fortune 500 Factory";
        var primaryContactEmail = "factory@test.com";
        var culturalRequirements = "Factory pattern test requirements";

        // Act
        var result = EnterpriseContract.Create(
            clientId,
            slaRequirements,
            featureAccess,
            usageLimits,
            contractValue,
            startDate,
            endDate,
            companyName,
            primaryContactEmail,
            culturalRequirements);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ClientId.Should().Be(clientId);
        result.Value.SLARequirements.Should().Be(slaRequirements);
        result.Value.FeatureAccess.Should().Be(featureAccess);
        result.Value.UsageLimits.Should().Be(usageLimits);
        result.Value.ContractValue.Should().Be(contractValue);
        result.Value.StartDate.Should().Be(startDate);
        result.Value.EndDate.Should().Be(endDate);
        result.Value.CompanyName.Should().Be(companyName);
        result.Value.PrimaryContactEmail.Should().Be(primaryContactEmail);
        result.Value.CulturalRequirements.Should().Be(culturalRequirements);
        result.Value.Status.Should().Be(ContractStatus.Draft);
        result.Value.AutoRenewal.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "test@company.com", "Company name is required")]
    [InlineData("", "test@company.com", "Company name is required")]
    [InlineData("Valid Company", null, "Primary contact email is required")]
    [InlineData("Valid Company", "", "Primary contact email is required")]
    public void Create_WithInvalidParameters_ShouldReturnFailureResult(
        string companyName, string primaryContactEmail, string expectedErrorContains)
    {
        // Arrange
        var clientId = new EnterpriseClientId();
        var slaRequirements = SLARequirements.CreateEnterpriseSLA();
        var featureAccess = CulturalIntelligenceFeatures.CreateFortune500FeatureSet();
        var usageLimits = EnterpriseUsageLimits.CreateFortune500Limits();
        var contractValue = ContractValue.CreateFortune500Contract(
            Domain.Shared.ValueObjects.Money.Create(750000m, Domain.Shared.Enums.Currency.USD).Value);
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddMonths(36);

        // Act
        var result = EnterpriseContract.Create(
            clientId,
            slaRequirements,
            featureAccess,
            usageLimits,
            contractValue,
            startDate,
            endDate,
            companyName,
            primaryContactEmail);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(expectedErrorContains);
    }

    #endregion

    #region Helper Methods

    private EnterpriseContract CreateValidFortune500Contract()
    {
        return EnterpriseContract.CreateFortune500Contract(
            "Test Fortune 500 Company",
            "enterprise@testcompany.com",
            750000m,
            DateTime.UtcNow.AddDays(1),
            36,
            "Full cultural intelligence features");
    }

    private EnterpriseContract CreateAndActivateContract()
    {
        var contract = CreateValidFortune500Contract();
        contract.ValidateCompliance(true, "Compliance validated for testing");
        contract.Activate();
        return contract;
    }

    private EnterpriseContract CreateMidMarketContractWithLimitedUsage()
    {
        var clientId = new EnterpriseClientId();
        var slaRequirements = SLARequirements.CreatePremiumSLA();
        var featureAccess = new CulturalIntelligenceFeatures(
            buddhistCalendarPremium: true,
            hinduCalendarPremium: true,
            culturalAppropriatenessScoring: true);
        var usageLimits = EnterpriseUsageLimits.CreateMidMarketLimits(); // Has usage limits
        var contractValue = ContractValue.CreateMidMarketContract(
            new Money(100000m, Currency.USD));
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddMonths(24);

        return new EnterpriseContract(
            clientId,
            slaRequirements,
            featureAccess,
            usageLimits,
            contractValue,
            startDate,
            endDate,
            "Mid-Market Test Company",
            "midmarket@testcompany.com",
            "Basic cultural features");
    }

    private EnterpriseContract CreateContractWithoutConsulting()
    {
        var clientId = new EnterpriseClientId();
        var slaRequirements = SLARequirements.CreateStandardSLA();
        var featureAccess = new CulturalIntelligenceFeatures(
            buddhistCalendarPremium: true,
            culturalConsultingServices: false); // No consulting
        var usageLimits = new EnterpriseUsageLimits(
            monthlyAPIRequests: 100000,
            concurrentRequests: 1000,
            consultingHoursIncluded: 0); // No consulting hours
        var contractValue = new ContractValue(
            new Money(50000m, Currency.USD),
            new Money(5000m, Currency.USD),
            ContractTier.Standard);
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = startDate.AddMonths(12);

        return new EnterpriseContract(
            clientId,
            slaRequirements,
            featureAccess,
            usageLimits,
            contractValue,
            startDate,
            endDate,
            "No Consulting Company",
            "noconsulting@testcompany.com");
    }

    private void ActivateContract(EnterpriseContract contract)
    {
        contract.ValidateCompliance(true, "Compliance validated for testing");
        contract.Activate();
    }

    private void SetNextBillingDateToPast(EnterpriseContract contract)
    {
        // Use reflection to set NextBillingDate to past for testing
        var nextBillingDateProperty = typeof(EnterpriseContract)
            .GetProperty("NextBillingDate", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        nextBillingDateProperty?.SetValue(contract, DateTime.UtcNow.AddDays(-1));
    }

    private void SetContractToExpireSoon(EnterpriseContract contract)
    {
        // Use reflection to set EndDate to soon-expiring date for testing
        var endDateProperty = typeof(EnterpriseContract)
            .GetProperty("EndDate", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        endDateProperty?.SetValue(contract, DateTime.UtcNow.AddDays(15)); // Expires in 15 days
    }

    #endregion
}