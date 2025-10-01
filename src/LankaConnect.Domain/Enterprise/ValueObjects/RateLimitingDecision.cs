using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class RateLimitingDecision : ValueObject
{
    public bool IsAllowed { get; private set; }
    public int CurrentUsage { get; private set; }
    public int RateLimit { get; private set; }
    public TimeSpan ResetPeriod { get; private set; }
    public DateTime? ResetTime { get; private set; }
    public string? RejectionReason { get; private set; }
    public int RemainingRequests => IsAllowed ? Math.Max(0, RateLimit - CurrentUsage) : 0;

    private RateLimitingDecision(
        bool isAllowed,
        int currentUsage,
        int rateLimit,
        TimeSpan resetPeriod,
        DateTime? resetTime = null,
        string? rejectionReason = null)
    {
        IsAllowed = isAllowed;
        CurrentUsage = currentUsage;
        RateLimit = rateLimit;
        ResetPeriod = resetPeriod;
        ResetTime = resetTime;
        RejectionReason = rejectionReason;
    }

    public static RateLimitingDecision Allow(
        int currentUsage,
        int rateLimit,
        TimeSpan resetPeriod,
        DateTime? resetTime = null)
    {
        if (currentUsage < 0) throw new ArgumentException("Current usage cannot be negative", nameof(currentUsage));
        if (rateLimit <= 0) throw new ArgumentException("Rate limit must be positive", nameof(rateLimit));
        if (resetPeriod <= TimeSpan.Zero) throw new ArgumentException("Reset period must be positive", nameof(resetPeriod));

        return new RateLimitingDecision(
            true,
            currentUsage,
            rateLimit,
            resetPeriod,
            resetTime);
    }

    public static RateLimitingDecision Reject(
        int currentUsage,
        int rateLimit,
        TimeSpan resetPeriod,
        string rejectionReason,
        DateTime? resetTime = null)
    {
        if (currentUsage < 0) throw new ArgumentException("Current usage cannot be negative", nameof(currentUsage));
        if (rateLimit <= 0) throw new ArgumentException("Rate limit must be positive", nameof(rateLimit));
        if (resetPeriod <= TimeSpan.Zero) throw new ArgumentException("Reset period must be positive", nameof(resetPeriod));
        if (string.IsNullOrWhiteSpace(rejectionReason)) throw new ArgumentException("Rejection reason is required", nameof(rejectionReason));

        return new RateLimitingDecision(
            false,
            currentUsage,
            rateLimit,
            resetPeriod,
            resetTime,
            rejectionReason);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return IsAllowed;
        yield return CurrentUsage;
        yield return RateLimit;
        yield return ResetPeriod;
        yield return ResetTime ?? DateTime.MinValue;
        yield return RejectionReason ?? string.Empty;
    }
}