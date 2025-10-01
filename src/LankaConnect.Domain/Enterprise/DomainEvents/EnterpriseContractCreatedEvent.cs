using LankaConnect.Domain.Common;
using LankaConnect.Domain.Enterprise.ValueObjects;

namespace LankaConnect.Domain.Enterprise.DomainEvents;

public class EnterpriseContractCreatedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public string CompanyName { get; }
    public decimal AnnualValue { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseContractCreatedEvent(Guid contractId, EnterpriseClientId clientId, string companyName, decimal annualValue)
    {
        ContractId = contractId;
        ClientId = clientId;
        CompanyName = companyName;
        AnnualValue = annualValue;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseContractActivatedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public string CompanyName { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseContractActivatedEvent(Guid contractId, EnterpriseClientId clientId, string companyName)
    {
        ContractId = contractId;
        ClientId = clientId;
        CompanyName = companyName;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseUsageQuotaExceededEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public int CurrentUsage { get; }
    public int QuotaLimit { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseUsageQuotaExceededEvent(Guid contractId, EnterpriseClientId clientId, int currentUsage, int quotaLimit)
    {
        ContractId = contractId;
        ClientId = clientId;
        CurrentUsage = currentUsage;
        QuotaLimit = quotaLimit;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseAPIUsageRecordedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public int APICallsUsed { get; }
    public string Endpoint { get; }
    public DateTime UsageDate { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseAPIUsageRecordedEvent(Guid contractId, EnterpriseClientId clientId, int apiCallsUsed, string endpoint, DateTime usageDate)
    {
        ContractId = contractId;
        ClientId = clientId;
        APICallsUsed = apiCallsUsed;
        Endpoint = endpoint;
        UsageDate = usageDate;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseConsultingOverageEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public int OverageHours { get; }
    public string ConsultingType { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseConsultingOverageEvent(Guid contractId, EnterpriseClientId clientId, int overageHours, string consultingType)
    {
        ContractId = contractId;
        ClientId = clientId;
        OverageHours = overageHours;
        ConsultingType = consultingType;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseConsultingUsageRecordedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public int HoursUsed { get; }
    public string ConsultingType { get; }
    public DateTime UsageDate { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseConsultingUsageRecordedEvent(Guid contractId, EnterpriseClientId clientId, int hoursUsed, string consultingType, DateTime usageDate)
    {
        ContractId = contractId;
        ClientId = clientId;
        HoursUsed = hoursUsed;
        ConsultingType = consultingType;
        UsageDate = usageDate;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseMonthlyBillingProcessedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public decimal BillingAmount { get; }
    public DateTime BillingDate { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseMonthlyBillingProcessedEvent(Guid contractId, EnterpriseClientId clientId, decimal billingAmount, DateTime billingDate)
    {
        ContractId = contractId;
        ClientId = clientId;
        BillingAmount = billingAmount;
        BillingDate = billingDate;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseComplianceValidatedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public string ComplianceNotes { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseComplianceValidatedEvent(Guid contractId, EnterpriseClientId clientId, string complianceNotes)
    {
        ContractId = contractId;
        ClientId = clientId;
        ComplianceNotes = complianceNotes;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseComplianceFailedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public string FailureReason { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseComplianceFailedEvent(Guid contractId, EnterpriseClientId clientId, string failureReason)
    {
        ContractId = contractId;
        ClientId = clientId;
        FailureReason = failureReason;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseContractSuspendedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public string SuspensionReason { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseContractSuspendedEvent(Guid contractId, EnterpriseClientId clientId, string suspensionReason)
    {
        ContractId = contractId;
        ClientId = clientId;
        SuspensionReason = suspensionReason;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseContractRenewedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public DateTime NewEndDate { get; }
    public decimal NewAnnualValue { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseContractRenewedEvent(Guid contractId, EnterpriseClientId clientId, DateTime newEndDate, decimal newAnnualValue)
    {
        ContractId = contractId;
        ClientId = clientId;
        NewEndDate = newEndDate;
        NewAnnualValue = newAnnualValue;
        OccurredAt = DateTime.UtcNow;
    }
}

public class EnterpriseContractTerminatedEvent : IDomainEvent
{
    public Guid ContractId { get; }
    public EnterpriseClientId ClientId { get; }
    public string TerminationReason { get; }
    public DateTime TerminationDate { get; }
    public DateTime OccurredAt { get; }

    public EnterpriseContractTerminatedEvent(Guid contractId, EnterpriseClientId clientId, string terminationReason, DateTime terminationDate)
    {
        ContractId = contractId;
        ClientId = clientId;
        TerminationReason = terminationReason;
        TerminationDate = terminationDate;
        OccurredAt = DateTime.UtcNow;
    }
}