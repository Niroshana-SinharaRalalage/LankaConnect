using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class EnterpriseGatewayHealth : ValueObject
{
    public string GatewayId { get; private set; }
    public string HealthStatus { get; private set; }
    public DateTime LastHealthCheck { get; private set; }
    public double CpuUtilization { get; private set; }
    public double MemoryUtilization { get; private set; }
    public double DiskUtilization { get; private set; }
    public int ActiveConnections { get; private set; }
    public double ResponseTimeAverage { get; private set; }
    public double ThroughputRPS { get; private set; }
    public IReadOnlyList<string> HealthIssues { get; private set; }
    public IReadOnlyDictionary<string, object> AdditionalMetrics { get; private set; }
    public bool IsHealthy => HealthStatus == "Healthy" && !HealthIssues.Any();

    private EnterpriseGatewayHealth(
        string gatewayId,
        string healthStatus,
        DateTime lastHealthCheck,
        double cpuUtilization,
        double memoryUtilization,
        double diskUtilization,
        int activeConnections,
        double responseTimeAverage,
        double throughputRPS,
        IReadOnlyList<string> healthIssues,
        IReadOnlyDictionary<string, object> additionalMetrics)
    {
        GatewayId = gatewayId;
        HealthStatus = healthStatus;
        LastHealthCheck = lastHealthCheck;
        CpuUtilization = cpuUtilization;
        MemoryUtilization = memoryUtilization;
        DiskUtilization = diskUtilization;
        ActiveConnections = activeConnections;
        ResponseTimeAverage = responseTimeAverage;
        ThroughputRPS = throughputRPS;
        HealthIssues = healthIssues;
        AdditionalMetrics = additionalMetrics;
    }

    public static EnterpriseGatewayHealth Create(
        string gatewayId,
        string healthStatus,
        DateTime lastHealthCheck,
        double cpuUtilization,
        double memoryUtilization,
        double diskUtilization,
        int activeConnections,
        double responseTimeAverage,
        double throughputRPS,
        IEnumerable<string> healthIssues,
        IReadOnlyDictionary<string, object>? additionalMetrics = null)
    {
        if (string.IsNullOrWhiteSpace(gatewayId)) throw new ArgumentException("Gateway ID is required", nameof(gatewayId));
        if (string.IsNullOrWhiteSpace(healthStatus)) throw new ArgumentException("Health status is required", nameof(healthStatus));
        if (lastHealthCheck > DateTime.UtcNow) throw new ArgumentException("Last health check cannot be in the future", nameof(lastHealthCheck));
        if (cpuUtilization < 0 || cpuUtilization > 100) throw new ArgumentException("CPU utilization must be between 0 and 100", nameof(cpuUtilization));
        if (memoryUtilization < 0 || memoryUtilization > 100) throw new ArgumentException("Memory utilization must be between 0 and 100", nameof(memoryUtilization));
        if (diskUtilization < 0 || diskUtilization > 100) throw new ArgumentException("Disk utilization must be between 0 and 100", nameof(diskUtilization));
        if (activeConnections < 0) throw new ArgumentException("Active connections cannot be negative", nameof(activeConnections));
        if (responseTimeAverage < 0) throw new ArgumentException("Response time average cannot be negative", nameof(responseTimeAverage));
        if (throughputRPS < 0) throw new ArgumentException("Throughput RPS cannot be negative", nameof(throughputRPS));

        var issuesList = healthIssues?.ToList() ?? new List<string>();
        var metricsDict = additionalMetrics ?? new Dictionary<string, object>();

        return new EnterpriseGatewayHealth(
            gatewayId,
            healthStatus,
            lastHealthCheck,
            cpuUtilization,
            memoryUtilization,
            diskUtilization,
            activeConnections,
            responseTimeAverage,
            throughputRPS,
            issuesList.AsReadOnly(),
            metricsDict);
    }

    public bool HasCriticalIssues()
    {
        return CpuUtilization > 90 || 
               MemoryUtilization > 90 || 
               DiskUtilization > 95 ||
               ResponseTimeAverage > 5000;
    }

    public string GetOverallHealthGrade()
    {
        if (!IsHealthy || HasCriticalIssues()) return "Critical";
        if (CpuUtilization > 80 || MemoryUtilization > 80 || ResponseTimeAverage > 2000) return "Warning";
        return "Good";
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return GatewayId;
        yield return HealthStatus;
        yield return LastHealthCheck;
        yield return CpuUtilization;
        yield return MemoryUtilization;
        yield return DiskUtilization;
        yield return ActiveConnections;
        yield return ResponseTimeAverage;
        yield return ThroughputRPS;
        
        foreach (var issue in HealthIssues)
            yield return issue;
        
        foreach (var metric in AdditionalMetrics.OrderBy(x => x.Key))
        {
            yield return metric.Key;
            yield return metric.Value;
        }
    }
}