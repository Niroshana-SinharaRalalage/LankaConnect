using LankaConnect.Domain.Common;
using LankaConnect.Domain.Enterprise.ValueObjects;
using LankaConnect.Domain.Enterprise.DomainEvents;

namespace LankaConnect.Domain.Enterprise;

/// <summary>
/// Enterprise Contract aggregate root - manages Fortune 500 and large organization agreements
/// with comprehensive Cultural Intelligence service offerings, SLA guarantees, and consulting services
/// </summary>
public class EnterpriseContract : AggregateRoot
{
    public required EnterpriseClientId ClientId { get; init; }
    public required SLARequirements SLARequirements { get; init; }
    public required CulturalIntelligenceFeatures FeatureAccess { get; init; }
    public required EnterpriseUsageLimits UsageLimits { get; init; }
    public required ContractValue ContractValue { get; set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ContractStatus Status { get; private set; }
    public required string CompanyName { get; init; }
    public required string PrimaryContactEmail { get; init; }
    public required string CulturalRequirements { get; init; }
    public bool AutoRenewal { get; private set; }
    public DateTime? LastBillingDate { get; private set; }
    public DateTime? NextBillingDate { get; private set; }
    public int ConsultingHoursUsed { get; private set; }
    public int CurrentMonthAPIUsage { get; private set; }
    public bool ComplianceValidated { get; private set; }
    public new DateTime CreatedAt { get; private set; }
    public new DateTime? UpdatedAt { get; private set; }

    // Domain events are handled by base class BaseEntity

    // Private constructor for EF Core
    private EnterpriseContract() { }

    /// <summary>
    /// Sets initial values for properties that cannot be set through object initializer
    /// </summary>
    internal void SetInitialValues(DateTime startDate, DateTime endDate, bool autoRenewal)
    {
        StartDate = startDate;
        EndDate = endDate;
        Status = ContractStatus.Draft;
        AutoRenewal = autoRenewal;
        ConsultingHoursUsed = 0;
        CurrentMonthAPIUsage = 0;
        ComplianceValidated = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a domain event to the internal collection
    /// </summary>
    internal void AddDomainEvent(IDomainEvent domainEvent)
    {
        RaiseDomainEvent(domainEvent);
    }

    public EnterpriseContract(
        EnterpriseClientId clientId,
        SLARequirements slaRequirements,
        CulturalIntelligenceFeatures featureAccess,
        EnterpriseUsageLimits usageLimits,
        ContractValue contractValue,
        DateTime startDate,
        DateTime endDate,
        string companyName,
        string primaryContactEmail,
        string culturalRequirements = "",
        bool autoRenewal = false)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name is required.", nameof(companyName));
            
        if (string.IsNullOrWhiteSpace(primaryContactEmail))
            throw new ArgumentException("Primary contact email is required.", nameof(primaryContactEmail));
            
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date.");
            
        if (endDate <= DateTime.UtcNow)
            throw new ArgumentException("End date must be in the future.");

        Id = Guid.NewGuid();
        ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        SLARequirements = slaRequirements ?? throw new ArgumentNullException(nameof(slaRequirements));
        FeatureAccess = featureAccess ?? throw new ArgumentNullException(nameof(featureAccess));
        UsageLimits = usageLimits ?? throw new ArgumentNullException(nameof(usageLimits));
        ContractValue = contractValue ?? throw new ArgumentNullException(nameof(contractValue));
        StartDate = startDate;
        EndDate = endDate;
        Status = ContractStatus.Draft;
        CompanyName = companyName;
        PrimaryContactEmail = primaryContactEmail;
        CulturalRequirements = culturalRequirements ?? string.Empty;
        AutoRenewal = autoRenewal;
        ConsultingHoursUsed = 0;
        CurrentMonthAPIUsage = 0;
        ComplianceValidated = false;
        CreatedAt = DateTime.UtcNow;

        // Domain event for contract creation
        RaiseDomainEvent(new EnterpriseContractCreatedEvent(Id, ClientId, CompanyName, ContractValue.AnnualValue.Amount));
    }

    /// <summary>
    /// Creates a Result-based factory for EnterpriseContract with validation
    /// </summary>
    public static Result<EnterpriseContract> Create(
        EnterpriseClientId clientId,
        SLARequirements slaRequirements,
        CulturalIntelligenceFeatures featureAccess,
        EnterpriseUsageLimits usageLimits,
        ContractValue contractValue,
        DateTime startDate,
        DateTime endDate,
        string companyName,
        string primaryContactEmail,
        string culturalRequirements = "",
        bool autoRenewal = false)
    {
        // Validation
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(companyName))
            errors.Add("Company name is required.");
            
        if (string.IsNullOrWhiteSpace(primaryContactEmail))
            errors.Add("Primary contact email is required.");
            
        if (startDate >= endDate)
            errors.Add("Start date must be before end date.");
            
        if (endDate <= DateTime.UtcNow)
            errors.Add("End date must be in the future.");
            
        if (clientId == null)
            errors.Add("Client ID is required.");
            
        if (slaRequirements == null)
            errors.Add("SLA requirements are required.");
            
        if (featureAccess == null)
            errors.Add("Feature access configuration is required.");
            
        if (usageLimits == null)
            errors.Add("Usage limits are required.");
            
        if (contractValue == null)
            errors.Add("Contract value is required.");

        if (errors.Any())
            return Result<EnterpriseContract>.Failure(errors);

        // Create using object initializer for required properties
        var contract = new EnterpriseContract
        {
            Id = Guid.NewGuid(),
            ClientId = clientId!,
            SLARequirements = slaRequirements!,
            FeatureAccess = featureAccess!,
            UsageLimits = usageLimits!,
            ContractValue = contractValue!,
            CompanyName = companyName!,
            PrimaryContactEmail = primaryContactEmail!,
            CulturalRequirements = culturalRequirements ?? string.Empty
        };
        
        // Set non-required properties through reflection or by making properties settable
        contract.SetInitialValues(startDate, endDate, autoRenewal);
        
        // Add domain event
        contract.AddDomainEvent(new EnterpriseContractCreatedEvent(contract.Id, clientId!, companyName!, contractValue!.AnnualValue.Amount));
        
        return Result<EnterpriseContract>.Success(contract);
    }

    /// <summary>
    /// Creates Fortune 500 enterprise contract with premium cultural intelligence features
    /// </summary>
    public static EnterpriseContract CreateFortune500Contract(
        string companyName,
        string primaryContactEmail,
        decimal annualValue,
        DateTime startDate,
        int contractTermMonths = 36,
        string culturalRequirements = "")
    {
        var clientId = new EnterpriseClientId();
        var slaRequirements = SLARequirements.CreateEnterpriseSLA();
        var featureAccess = CulturalIntelligenceFeatures.CreateFortune500FeatureSet();
        var usageLimits = EnterpriseUsageLimits.CreateFortune500Limits();
        var contractValue = ContractValue.CreateFortune500Contract(
            Domain.Shared.ValueObjects.Money.Create(annualValue, Domain.Shared.Enums.Currency.USD).Value);
        var endDate = startDate.AddMonths(contractTermMonths);

        // Use factory method with required property initialization
        var result = Create(
            clientId,
            slaRequirements,
            featureAccess,
            usageLimits,
            contractValue,
            startDate,
            endDate,
            companyName,
            primaryContactEmail,
            culturalRequirements,
            autoRenewal: true);
            
        if (result.IsFailure)
            throw new ArgumentException($"Failed to create Fortune 500 contract: {string.Join(", ", result.Errors)}");
            
        return result.Value;
    }

    /// <summary>
    /// Creates educational institution contract with academic-focused cultural intelligence
    /// </summary>
    public static EnterpriseContract CreateEducationalInstitutionContract(
        string institutionName,
        string primaryContactEmail,
        decimal annualValue,
        DateTime startDate,
        string culturalRequirements = "")
    {
        var clientId = new EnterpriseClientId();
        var slaRequirements = SLARequirements.CreatePremiumSLA();
        var featureAccess = CulturalIntelligenceFeatures.CreateEducationalInstitutionFeatureSet();
        var usageLimits = EnterpriseUsageLimits.CreateEducationalLimits();
        var contractValue = ContractValue.CreateEducationalContract(
            Domain.Shared.ValueObjects.Money.Create(annualValue, Domain.Shared.Enums.Currency.USD).Value);
        var endDate = startDate.AddMonths(12);

        // Use factory method with required property initialization
        var result = Create(
            clientId,
            slaRequirements,
            featureAccess,
            usageLimits,
            contractValue,
            startDate,
            endDate,
            institutionName,
            primaryContactEmail,
            culturalRequirements,
            autoRenewal: false);
            
        if (result.IsFailure)
            throw new ArgumentException($"Failed to create Educational contract: {string.Join(", ", result.Errors)}");
            
        return result.Value;
    }

    /// <summary>
    /// Creates government agency contract with compliance-focused cultural analytics
    /// </summary>
    public static EnterpriseContract CreateGovernmentAgencyContract(
        string agencyName,
        string primaryContactEmail,
        decimal annualValue,
        DateTime startDate,
        string culturalRequirements = "")
    {
        var clientId = new EnterpriseClientId();
        var slaRequirements = SLARequirements.CreateEnterpriseSLA(); // Government requires highest SLA
        var featureAccess = CulturalIntelligenceFeatures.CreateGovernmentFeatureSet();
        var usageLimits = EnterpriseUsageLimits.CreateGovernmentLimits();
        var contractValue = ContractValue.CreateGovernmentContract(
            Domain.Shared.ValueObjects.Money.Create(annualValue, Domain.Shared.Enums.Currency.USD).Value);
        var endDate = startDate.AddMonths(60); // 5-year government contract

        // Use factory method with required property initialization
        var result = Create(
            clientId,
            slaRequirements,
            featureAccess,
            usageLimits,
            contractValue,
            startDate,
            endDate,
            agencyName,
            primaryContactEmail,
            culturalRequirements,
            autoRenewal: false); // Government contracts typically don't auto-renew
            
        if (result.IsFailure)
            throw new ArgumentException($"Failed to create Government contract: {string.Join(", ", result.Errors)}");
            
        return result.Value;
    }

    /// <summary>
    /// Activates contract after compliance validation and legal approval
    /// </summary>
    public void Activate()
    {
        if (Status != ContractStatus.Draft && Status != ContractStatus.PendingApproval)
            throw new InvalidOperationException("Contract can only be activated from Draft or PendingApproval status.");
            
        if (!ComplianceValidated)
            throw new InvalidOperationException("Contract must be compliance validated before activation.");
            
        Status = ContractStatus.Active;
        NextBillingDate = StartDate.AddMonths(1);
        UpdatedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new EnterpriseContractActivatedEvent(Id, ClientId, CompanyName));
    }

    /// <summary>
    /// Records API usage for billing and quota management
    /// </summary>
    public void RecordAPIUsage(int apiCallsUsed, string endpoint, DateTime usageDate)
    {
        if (Status != ContractStatus.Active)
            throw new InvalidOperationException("Cannot record usage for inactive contracts.");
            
        if (apiCallsUsed <= 0)
            throw new ArgumentException("API calls used must be positive.", nameof(apiCallsUsed));

        CurrentMonthAPIUsage += apiCallsUsed;
        UpdatedAt = DateTime.UtcNow;
        
        // Check for quota exceeded (unless unlimited)
        if (!UsageLimits.IsUnlimited && CurrentMonthAPIUsage > UsageLimits.MonthlyAPIRequests)
        {
            RaiseDomainEvent(new EnterpriseUsageQuotaExceededEvent(Id, ClientId, CurrentMonthAPIUsage, UsageLimits.MonthlyAPIRequests));
        }
        
        RaiseDomainEvent(new EnterpriseAPIUsageRecordedEvent(Id, ClientId, apiCallsUsed, endpoint, usageDate));
    }

    /// <summary>
    /// Records consulting hours usage for enterprise clients
    /// </summary>
    public void RecordConsultingUsage(int hoursUsed, string consultingType, DateTime usageDate)
    {
        if (Status != ContractStatus.Active)
            throw new InvalidOperationException("Cannot record consulting usage for inactive contracts.");
            
        if (!FeatureAccess.CulturalConsultingServices)
            throw new InvalidOperationException("Consulting services not included in this contract.");
            
        if (hoursUsed <= 0)
            throw new ArgumentException("Consulting hours used must be positive.", nameof(hoursUsed));

        ConsultingHoursUsed += hoursUsed;
        UpdatedAt = DateTime.UtcNow;
        
        // Check if consulting hours exceeded
        if (ConsultingHoursUsed > UsageLimits.ConsultingHoursIncluded)
        {
            var overage = ConsultingHoursUsed - UsageLimits.ConsultingHoursIncluded;
            RaiseDomainEvent(new EnterpriseConsultingOverageEvent(Id, ClientId, overage, consultingType));
        }
        
        RaiseDomainEvent(new EnterpriseConsultingUsageRecordedEvent(Id, ClientId, hoursUsed, consultingType, usageDate));
    }

    /// <summary>
    /// Process monthly billing for enterprise contract
    /// </summary>
    public void ProcessMonthlyBilling()
    {
        if (Status != ContractStatus.Active)
            throw new InvalidOperationException("Cannot process billing for inactive contracts.");
            
        if (NextBillingDate > DateTime.UtcNow)
            throw new InvalidOperationException("Billing date has not arrived.");

        LastBillingDate = NextBillingDate;
        NextBillingDate = LastBillingDate!.Value.AddMonths(1);
        
        // Reset monthly usage counters
        CurrentMonthAPIUsage = 0;
        UpdatedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new EnterpriseMonthlyBillingProcessedEvent(Id, ClientId, ContractValue.MonthlyValue.Amount, LastBillingDate.Value));
    }

    /// <summary>
    /// Validates compliance requirements for enterprise contract
    /// </summary>
    public void ValidateCompliance(bool passedCompliance, string complianceNotes = "")
    {
        ComplianceValidated = passedCompliance;
        UpdatedAt = DateTime.UtcNow;
        
        if (passedCompliance)
        {
            RaiseDomainEvent(new EnterpriseComplianceValidatedEvent(Id, ClientId, complianceNotes));
        }
        else
        {
            Status = ContractStatus.ComplianceFailed;
            RaiseDomainEvent(new EnterpriseComplianceFailedEvent(Id, ClientId, complianceNotes));
        }
    }

    /// <summary>
    /// Suspends contract due to SLA violations or non-payment
    /// </summary>
    public void Suspend(string reason)
    {
        if (Status != ContractStatus.Active)
            throw new InvalidOperationException("Only active contracts can be suspended.");
            
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Suspension reason is required.", nameof(reason));

        Status = ContractStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new EnterpriseContractSuspendedEvent(Id, ClientId, reason));
    }

    /// <summary>
    /// Renews contract for additional term
    /// </summary>
    public void Renew(DateTime newEndDate, ContractValue? newContractValue = null)
    {
        if (Status != ContractStatus.Active && Status != ContractStatus.Expiring)
            throw new InvalidOperationException("Only active or expiring contracts can be renewed.");
            
        if (newEndDate <= EndDate)
            throw new ArgumentException("New end date must be after current end date.");

        EndDate = newEndDate;
        if (newContractValue != null)
        {
            ContractValue = newContractValue;
        }
        Status = ContractStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new EnterpriseContractRenewedEvent(Id, ClientId, newEndDate, ContractValue.AnnualValue.Amount));
    }

    /// <summary>
    /// Terminates contract before end date
    /// </summary>
    public void Terminate(string reason, DateTime? terminationDate = null)
    {
        if (Status == ContractStatus.Terminated)
            throw new InvalidOperationException("Contract is already terminated.");
            
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Termination reason is required.", nameof(reason));

        var effectiveTerminationDate = terminationDate ?? DateTime.UtcNow;
        if (effectiveTerminationDate < StartDate)
            throw new ArgumentException("Termination date cannot be before start date.");

        Status = ContractStatus.Terminated;
        EndDate = effectiveTerminationDate;
        UpdatedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new EnterpriseContractTerminatedEvent(Id, ClientId, reason, effectiveTerminationDate));
    }

    // ClearDomainEvents is handled by base class BaseEntity
    
    // Computed properties
    public bool IsExpiring => Status == ContractStatus.Active && EndDate <= DateTime.UtcNow.AddDays(30);
    public bool IsActive => Status == ContractStatus.Active && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
    public bool HasUnlimitedUsage => UsageLimits.IsUnlimited;
    public bool RequiresSLAMonitoring => SLARequirements.IsEnterprise;
    public int DaysUntilExpiration => (int)(EndDate - DateTime.UtcNow).TotalDays;
    public decimal MonthlyRecurringRevenue => ContractValue.MonthlyValue.Amount;
    public bool IncludesCulturalConsulting => FeatureAccess.CulturalConsultingServices;
    public bool SupportsWhiteLabel => FeatureAccess.WhiteLabelLicensing;
    public int RemainingConsultingHours => Math.Max(0, UsageLimits.ConsultingHoursIncluded - ConsultingHoursUsed);
    public double APIUsagePercentage => UsageLimits.IsUnlimited ? 0 : (double)CurrentMonthAPIUsage / UsageLimits.MonthlyAPIRequests * 100;

    /// <summary>
    /// Validates the current state of the enterprise contract aggregate
    /// </summary>
    public override ValidationResult Validate()
    {
        var errors = new List<string>();
        
        if (ClientId == null)
            errors.Add("Client ID is required");
            
        if (SLARequirements == null)
            errors.Add("SLA requirements are required");
            
        if (FeatureAccess == null)
            errors.Add("Feature access configuration is required");
            
        if (UsageLimits == null)
            errors.Add("Usage limits are required");
            
        if (ContractValue == null)
            errors.Add("Contract value is required");
            
        if (string.IsNullOrWhiteSpace(CompanyName))
            errors.Add("Company name is required");
            
        if (string.IsNullOrWhiteSpace(PrimaryContactEmail))
            errors.Add("Primary contact email is required");
            
        if (StartDate >= EndDate)
            errors.Add("Start date must be before end date");
            
        return errors.Any() ? ValidationResult.Invalid(errors) : ValidationResult.Valid();
    }
}


public enum ContractStatus
{
    Draft,
    PendingApproval,
    Active,
    Suspended,
    Expiring,
    Terminated,
    ComplianceFailed
}