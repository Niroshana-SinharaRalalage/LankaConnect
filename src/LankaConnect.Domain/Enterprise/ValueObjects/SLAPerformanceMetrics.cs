using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class SLAPerformanceMetrics : ValueObject
{
    public double ResponseTimeP95 { get; private set; }
    public double ResponseTimeP99 { get; private set; }
    public double UpTimePercentage { get; private set; }
    public double ThroughputRPS { get; private set; }
    public double ErrorRate { get; private set; }
    public DateTime MeasurementPeriodStart { get; private set; }
    public DateTime MeasurementPeriodEnd { get; private set; }
    public bool SLACompliant => 
        UpTimePercentage >= 99.9 && 
        ResponseTimeP95 <= 500 && 
        ErrorRate <= 0.1;

    private SLAPerformanceMetrics(
        double responseTimeP95,
        double responseTimeP99,
        double upTimePercentage,
        double throughputRPS,
        double errorRate,
        DateTime measurementPeriodStart,
        DateTime measurementPeriodEnd)
    {
        ResponseTimeP95 = responseTimeP95;
        ResponseTimeP99 = responseTimeP99;
        UpTimePercentage = upTimePercentage;
        ThroughputRPS = throughputRPS;
        ErrorRate = errorRate;
        MeasurementPeriodStart = measurementPeriodStart;
        MeasurementPeriodEnd = measurementPeriodEnd;
    }

    public static SLAPerformanceMetrics Create(
        double responseTimeP95,
        double responseTimeP99,
        double upTimePercentage,
        double throughputRPS,
        double errorRate,
        DateTime measurementPeriodStart,
        DateTime measurementPeriodEnd)
    {
        if (responseTimeP95 < 0) throw new ArgumentException("Response time P95 cannot be negative", nameof(responseTimeP95));
        if (responseTimeP99 < 0) throw new ArgumentException("Response time P99 cannot be negative", nameof(responseTimeP99));
        if (upTimePercentage < 0 || upTimePercentage > 100) throw new ArgumentException("Uptime percentage must be between 0 and 100", nameof(upTimePercentage));
        if (throughputRPS < 0) throw new ArgumentException("Throughput cannot be negative", nameof(throughputRPS));
        if (errorRate < 0) throw new ArgumentException("Error rate cannot be negative", nameof(errorRate));
        if (measurementPeriodStart >= measurementPeriodEnd) throw new ArgumentException("Measurement period start must be before end", nameof(measurementPeriodStart));

        return new SLAPerformanceMetrics(
            responseTimeP95,
            responseTimeP99,
            upTimePercentage,
            throughputRPS,
            errorRate,
            measurementPeriodStart,
            measurementPeriodEnd);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ResponseTimeP95;
        yield return ResponseTimeP99;
        yield return UpTimePercentage;
        yield return ThroughputRPS;
        yield return ErrorRate;
        yield return MeasurementPeriodStart;
        yield return MeasurementPeriodEnd;
    }
}