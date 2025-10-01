using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Routing;

/// <summary>
/// TDD GREEN Phase: Application routing result types extending Domain Result pattern
/// Provides specialized result handling for routing operations
/// </summary>

/// <summary>
/// Result type for bulk profile update operations with cultural intelligence routing
/// </summary>
public class BulkProfileUpdateResult : Result<BulkProfileUpdateData>
{
    public int TotalProfiles => IsSuccess ? Value.TotalProfiles : 0;
    public int SuccessfulUpdates => IsSuccess ? Value.SuccessfulUpdates : 0;
    public int FailedUpdates => IsSuccess ? Value.FailedUpdates : 0;
    public IEnumerable<string> FailedProfileIds => IsSuccess ? Value.FailedProfileIds : Array.Empty<string>();
    public TimeSpan ProcessingTime => IsSuccess ? Value.ProcessingTime : TimeSpan.Zero;

    protected BulkProfileUpdateResult(bool isSuccess, IEnumerable<string> errors, BulkProfileUpdateData? value = null)
        : base(isSuccess, errors, value)
    {
    }

    public static BulkProfileUpdateResult Success(int totalProfiles, int successfulUpdates, int failedUpdates, 
        IEnumerable<string> failedProfileIds, TimeSpan processingTime)
    {
        var data = new BulkProfileUpdateData(
            totalProfiles,
            successfulUpdates,
            failedUpdates,
            failedProfileIds ?? Array.Empty<string>(),
            processingTime
        );
        return new BulkProfileUpdateResult(true, Array.Empty<string>(), data);
    }

    public static new BulkProfileUpdateResult Failure(string error)
    {
        return new BulkProfileUpdateResult(false, new[] { error });
    }

    public static new BulkProfileUpdateResult Failure(IEnumerable<string> errors)
    {
        return new BulkProfileUpdateResult(false, errors);
    }
}

/// <summary>
/// Data container for bulk profile update results
/// </summary>
public record BulkProfileUpdateData(
    int TotalProfiles,
    int SuccessfulUpdates, 
    int FailedUpdates,
    IEnumerable<string> FailedProfileIds,
    TimeSpan ProcessingTime
);