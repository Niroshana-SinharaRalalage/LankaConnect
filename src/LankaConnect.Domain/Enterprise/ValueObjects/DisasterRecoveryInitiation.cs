using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class DisasterRecoveryInitiation : ValueObject
{
    public string InitiationId { get; private set; }
    public EnterpriseClientId ClientId { get; private set; }
    public DisasterRecoveryTrigger Trigger { get; private set; }
    public DateTime InitiatedAt { get; private set; }
    public string InitiatedBy { get; private set; }
    public string RecoveryPlan { get; private set; }
    public string PrimaryRegion { get; private set; }
    public string FailoverRegion { get; private set; }
    public IReadOnlyList<string> RecoverySteps { get; private set; }
    public IReadOnlyDictionary<string, string> RecoveryParameters { get; private set; }
    public TimeSpan EstimatedExecutionTime { get; private set; }
    public string Priority { get; private set; }
    public IReadOnlyList<string> StakeholdersNotified { get; private set; }
    public IReadOnlyList<string> ServicesInScope { get; private set; }
    public string ApprovalStatus { get; private set; }
    public string ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public bool IsAutoApproved { get; private set; }

    private DisasterRecoveryInitiation(
        string initiationId,
        EnterpriseClientId clientId,
        DisasterRecoveryTrigger trigger,
        DateTime initiatedAt,
        string initiatedBy,
        string recoveryPlan,
        string primaryRegion,
        string failoverRegion,
        IReadOnlyList<string> recoverySteps,
        IReadOnlyDictionary<string, string> recoveryParameters,
        TimeSpan estimatedExecutionTime,
        string priority,
        IReadOnlyList<string> stakeholdersNotified,
        IReadOnlyList<string> servicesInScope,
        string approvalStatus,
        string approvedBy,
        DateTime? approvedAt,
        bool isAutoApproved)
    {
        InitiationId = initiationId;
        ClientId = clientId;
        Trigger = trigger;
        InitiatedAt = initiatedAt;
        InitiatedBy = initiatedBy;
        RecoveryPlan = recoveryPlan;
        PrimaryRegion = primaryRegion;
        FailoverRegion = failoverRegion;
        RecoverySteps = recoverySteps;
        RecoveryParameters = recoveryParameters;
        EstimatedExecutionTime = estimatedExecutionTime;
        Priority = priority;
        StakeholdersNotified = stakeholdersNotified;
        ServicesInScope = servicesInScope;
        ApprovalStatus = approvalStatus;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
        IsAutoApproved = isAutoApproved;
    }

    public static DisasterRecoveryInitiation Create(
        string initiationId,
        EnterpriseClientId clientId,
        DisasterRecoveryTrigger trigger,
        DateTime initiatedAt,
        string initiatedBy,
        string recoveryPlan,
        string primaryRegion,
        string failoverRegion,
        IEnumerable<string> recoverySteps,
        IReadOnlyDictionary<string, string> recoveryParameters,
        TimeSpan estimatedExecutionTime,
        string priority,
        IEnumerable<string> stakeholdersNotified,
        IEnumerable<string> servicesInScope,
        string approvalStatus,
        string approvedBy = "",
        DateTime? approvedAt = null,
        bool isAutoApproved = false)
    {
        if (string.IsNullOrWhiteSpace(initiationId)) throw new ArgumentException("Initiation ID is required", nameof(initiationId));
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (trigger == null) throw new ArgumentNullException(nameof(trigger));
        if (initiatedAt > DateTime.UtcNow.AddMinutes(1)) throw new ArgumentException("Initiated at cannot be significantly in the future", nameof(initiatedAt));
        if (string.IsNullOrWhiteSpace(initiatedBy)) throw new ArgumentException("Initiated by is required", nameof(initiatedBy));
        if (string.IsNullOrWhiteSpace(recoveryPlan)) throw new ArgumentException("Recovery plan is required", nameof(recoveryPlan));
        if (string.IsNullOrWhiteSpace(primaryRegion)) throw new ArgumentException("Primary region is required", nameof(primaryRegion));
        if (string.IsNullOrWhiteSpace(failoverRegion)) throw new ArgumentException("Failover region is required", nameof(failoverRegion));
        if (recoveryParameters == null) throw new ArgumentNullException(nameof(recoveryParameters));
        if (estimatedExecutionTime < TimeSpan.Zero) throw new ArgumentException("Estimated execution time cannot be negative", nameof(estimatedExecutionTime));
        if (string.IsNullOrWhiteSpace(priority)) throw new ArgumentException("Priority is required", nameof(priority));
        if (string.IsNullOrWhiteSpace(approvalStatus)) throw new ArgumentException("Approval status is required", nameof(approvalStatus));

        var stepsList = recoverySteps?.ToList() ?? throw new ArgumentNullException(nameof(recoverySteps));
        var notifiedList = stakeholdersNotified?.ToList() ?? new List<string>();
        var servicesList = servicesInScope?.ToList() ?? throw new ArgumentNullException(nameof(servicesInScope));

        if (!stepsList.Any()) throw new ArgumentException("At least one recovery step is required", nameof(recoverySteps));
        if (!servicesList.Any()) throw new ArgumentException("At least one service in scope is required", nameof(servicesInScope));

        // Validate priority
        var validPriorities = new[] { "P1 - Critical", "P2 - High", "P3 - Medium", "P4 - Low" };
        if (!validPriorities.Contains(priority))
            throw new ArgumentException($"Priority must be one of: {string.Join(", ", validPriorities)}", nameof(priority));

        // Validate approval status
        var validApprovalStatuses = new[] { "Pending", "Approved", "Rejected", "Auto-Approved", "Escalated" };
        if (!validApprovalStatuses.Contains(approvalStatus))
            throw new ArgumentException($"Approval status must be one of: {string.Join(", ", validApprovalStatuses)}", nameof(approvalStatus));

        // Validation for approved status
        if ((approvalStatus == "Approved" || approvalStatus == "Auto-Approved") && !approvedAt.HasValue)
            throw new ArgumentException("Approved at is required when approval status is Approved or Auto-Approved", nameof(approvedAt));

        if (approvalStatus == "Approved" && string.IsNullOrWhiteSpace(approvedBy))
            throw new ArgumentException("Approved by is required when approval status is Approved", nameof(approvedBy));

        return new DisasterRecoveryInitiation(
            initiationId,
            clientId,
            trigger,
            initiatedAt,
            initiatedBy,
            recoveryPlan,
            primaryRegion,
            failoverRegion,
            stepsList.AsReadOnly(),
            recoveryParameters,
            estimatedExecutionTime,
            priority,
            notifiedList.AsReadOnly(),
            servicesList.AsReadOnly(),
            approvalStatus,
            approvedBy,
            approvedAt,
            isAutoApproved);
    }

    public bool IsApproved()
    {
        return ApprovalStatus == "Approved" || ApprovalStatus == "Auto-Approved";
    }

    public bool RequiresApproval()
    {
        return ApprovalStatus == "Pending" || ApprovalStatus == "Escalated";
    }

    public bool IsCriticalPriority()
    {
        return Priority == "P1 - Critical" || Priority == "P2 - High";
    }

    public TimeSpan GetApprovalTimeElapsed()
    {
        if (ApprovedAt.HasValue)
            return ApprovedAt.Value - InitiatedAt;
        return DateTime.UtcNow - InitiatedAt;
    }

    public string GetExecutionReadiness()
    {
        if (!IsApproved()) return "Pending Approval";
        if (IsCriticalPriority() && GetApprovalTimeElapsed() > TimeSpan.FromMinutes(15)) return "Delayed";
        if (EstimatedExecutionTime > TimeSpan.FromHours(4)) return "Long Duration";
        return "Ready";
    }

    public IReadOnlyList<string> GetPreExecutionChecklist()
    {
        var checklist = new List<string>
        {
            "Verify failover region capacity",
            "Confirm backup data integrity",
            "Validate network connectivity",
            "Ensure monitoring systems are active"
        };

        if (IsCriticalPriority())
        {
            checklist.Add("Notify executive leadership");
            checklist.Add("Prepare customer communications");
        }

        if (Trigger.TriggerType == "SecurityBreach")
        {
            checklist.Add("Confirm security containment measures");
            checklist.Add("Validate security team readiness");
        }

        if (Trigger.TriggerType == "CulturalEventOverload")
        {
            checklist.Add("Verify cultural event calendar");
            checklist.Add("Confirm regional load balancers");
        }

        return checklist.AsReadOnly();
    }

    public double GetExecutionRiskScore()
    {
        double risk = 0.0;

        // Base risk from trigger severity
        risk += Trigger.Severity switch
        {
            "Emergency" => 0.4,
            "Critical" => 0.3,
            "High" => 0.2,
            "Medium" => 0.1,
            _ => 0.05
        };

        // Risk from execution time
        if (EstimatedExecutionTime > TimeSpan.FromHours(4)) risk += 0.2;
        else if (EstimatedExecutionTime > TimeSpan.FromHours(1)) risk += 0.1;

        // Risk from number of services
        if (ServicesInScope.Count > 10) risk += 0.2;
        else if (ServicesInScope.Count > 5) risk += 0.1;

        // Risk from approval delay
        if (RequiresApproval() && GetApprovalTimeElapsed() > TimeSpan.FromMinutes(30)) risk += 0.1;

        return Math.Min(1.0, risk);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return InitiationId;
        yield return ClientId;
        yield return Trigger;
        yield return InitiatedAt;
        yield return InitiatedBy;
        yield return RecoveryPlan;
        yield return PrimaryRegion;
        yield return FailoverRegion;
        yield return EstimatedExecutionTime;
        yield return Priority;
        yield return ApprovalStatus;
        yield return ApprovedBy ?? string.Empty;
        yield return ApprovedAt ?? DateTime.MinValue;
        yield return IsAutoApproved;
        
        foreach (var step in RecoverySteps)
            yield return step;
        
        foreach (var parameter in RecoveryParameters.OrderBy(x => x.Key))
        {
            yield return parameter.Key;
            yield return parameter.Value;
        }
        
        foreach (var stakeholder in StakeholdersNotified)
            yield return stakeholder;
        
        foreach (var service in ServicesInScope)
            yield return service;
    }
}