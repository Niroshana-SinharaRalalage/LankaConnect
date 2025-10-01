using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class FailoverTestResult : ValueObject
{
    public string TestId { get; private set; }
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime TestExecutedAt { get; private set; }
    public TimeSpan FailoverTime { get; private set; }
    public TimeSpan RecoveryTime { get; private set; }
    public bool TestPassed { get; private set; }
    public string TestType { get; private set; }
    public string PrimaryRegion { get; private set; }
    public string FailoverRegion { get; private set; }
    public IReadOnlyList<string> TestSteps { get; private set; }
    public IReadOnlyList<string> Issues { get; private set; }
    public IReadOnlyDictionary<string, object> PerformanceMetrics { get; private set; }
    public double DataConsistencyScore { get; private set; }
    public string TestSeverity { get; private set; }
    public bool MeetsRTO => FailoverTime <= TimeSpan.FromMinutes(5); // Recovery Time Objective
    public bool MeetsRPO => DataConsistencyScore >= 0.99; // Recovery Point Objective

    private FailoverTestResult(
        string testId,
        EnterpriseClientId clientId,
        DateTime testExecutedAt,
        TimeSpan failoverTime,
        TimeSpan recoveryTime,
        bool testPassed,
        string testType,
        string primaryRegion,
        string failoverRegion,
        IReadOnlyList<string> testSteps,
        IReadOnlyList<string> issues,
        IReadOnlyDictionary<string, object> performanceMetrics,
        double dataConsistencyScore,
        string testSeverity)
    {
        TestId = testId;
        ClientId = clientId;
        TestExecutedAt = testExecutedAt;
        FailoverTime = failoverTime;
        RecoveryTime = recoveryTime;
        TestPassed = testPassed;
        TestType = testType;
        PrimaryRegion = primaryRegion;
        FailoverRegion = failoverRegion;
        TestSteps = testSteps;
        Issues = issues;
        PerformanceMetrics = performanceMetrics;
        DataConsistencyScore = dataConsistencyScore;
        TestSeverity = testSeverity;
    }

    public static FailoverTestResult Create(
        string testId,
        EnterpriseClientId clientId,
        DateTime testExecutedAt,
        TimeSpan failoverTime,
        TimeSpan recoveryTime,
        bool testPassed,
        string testType,
        string primaryRegion,
        string failoverRegion,
        IEnumerable<string> testSteps,
        IEnumerable<string> issues,
        IReadOnlyDictionary<string, object> performanceMetrics,
        double dataConsistencyScore,
        string testSeverity)
    {
        if (string.IsNullOrWhiteSpace(testId)) throw new ArgumentException("Test ID is required", nameof(testId));
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (testExecutedAt > DateTime.UtcNow) throw new ArgumentException("Test executed at cannot be in the future", nameof(testExecutedAt));
        if (failoverTime < TimeSpan.Zero) throw new ArgumentException("Failover time cannot be negative", nameof(failoverTime));
        if (recoveryTime < TimeSpan.Zero) throw new ArgumentException("Recovery time cannot be negative", nameof(recoveryTime));
        if (string.IsNullOrWhiteSpace(testType)) throw new ArgumentException("Test type is required", nameof(testType));
        if (string.IsNullOrWhiteSpace(primaryRegion)) throw new ArgumentException("Primary region is required", nameof(primaryRegion));
        if (string.IsNullOrWhiteSpace(failoverRegion)) throw new ArgumentException("Failover region is required", nameof(failoverRegion));
        if (dataConsistencyScore < 0 || dataConsistencyScore > 1) throw new ArgumentException("Data consistency score must be between 0 and 1", nameof(dataConsistencyScore));
        if (string.IsNullOrWhiteSpace(testSeverity)) throw new ArgumentException("Test severity is required", nameof(testSeverity));

        var stepsList = testSteps?.ToList() ?? throw new ArgumentNullException(nameof(testSteps));
        var issuesList = issues?.ToList() ?? new List<string>();
        
        if (!stepsList.Any()) throw new ArgumentException("At least one test step is required", nameof(testSteps));

        // Validate test severity
        var validSeverities = new[] { "Low", "Medium", "High", "Critical" };
        if (!validSeverities.Contains(testSeverity))
            throw new ArgumentException($"Test severity must be one of: {string.Join(", ", validSeverities)}", nameof(testSeverity));

        // Validate test type
        var validTestTypes = new[] { "Planned", "Unplanned", "Partial", "Full", "Network", "Database", "Application" };
        if (!validTestTypes.Contains(testType))
            throw new ArgumentException($"Test type must be one of: {string.Join(", ", validTestTypes)}", nameof(testType));

        return new FailoverTestResult(
            testId,
            clientId,
            testExecutedAt,
            failoverTime,
            recoveryTime,
            testPassed,
            testType,
            primaryRegion,
            failoverRegion,
            stepsList.AsReadOnly(),
            issuesList.AsReadOnly(),
            performanceMetrics ?? new Dictionary<string, object>(),
            dataConsistencyScore,
            testSeverity);
    }

    public string GetOverallGrade()
    {
        if (!TestPassed) return "Failed";
        if (!MeetsRTO || !MeetsRPO) return "Partial";
        if (Issues.Any()) return "Warning";
        return "Passed";
    }

    public bool RequiresImmediateAction()
    {
        return !TestPassed || 
               !MeetsRTO || 
               !MeetsRPO || 
               TestSeverity == "Critical" ||
               Issues.Any(i => i.Contains("critical", StringComparison.OrdinalIgnoreCase));
    }

    public TimeSpan GetTotalOutageTime() => FailoverTime + RecoveryTime;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TestId;
        yield return ClientId;
        yield return TestExecutedAt;
        yield return FailoverTime;
        yield return RecoveryTime;
        yield return TestPassed;
        yield return TestType;
        yield return PrimaryRegion;
        yield return FailoverRegion;
        yield return DataConsistencyScore;
        yield return TestSeverity;
        
        foreach (var step in TestSteps)
            yield return step;
        
        foreach (var issue in Issues)
            yield return issue;
        
        foreach (var metric in PerformanceMetrics.OrderBy(x => x.Key))
        {
            yield return metric.Key;
            yield return metric.Value;
        }
    }
}