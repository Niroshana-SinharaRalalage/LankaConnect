using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing pass configuration settings for an event
/// Defines rules for how passes can be purchased and managed
/// </summary>
public class PassConfiguration : ValueObject
{
    public int MaxPassesPerUser { get; }
    public bool AllowMultiplePassTypes { get; }
    public bool IsRefundable { get; }
    public bool IsTransferable { get; }

    private PassConfiguration(
        int maxPassesPerUser,
        bool allowMultiplePassTypes,
        bool isRefundable,
        bool isTransferable)
    {
        MaxPassesPerUser = maxPassesPerUser;
        AllowMultiplePassTypes = allowMultiplePassTypes;
        IsRefundable = isRefundable;
        IsTransferable = isTransferable;
    }

    public static Result<PassConfiguration> Create(
        int maxPassesPerUser,
        bool allowMultiplePassTypes,
        bool refundable,
        bool transferable)
    {
        if (maxPassesPerUser <= 0)
            return Result<PassConfiguration>.Failure("MaxPassesPerUser must be greater than 0");

        return Result<PassConfiguration>.Success(new PassConfiguration(
            maxPassesPerUser,
            allowMultiplePassTypes,
            refundable,
            transferable));
    }

    public static PassConfiguration Default() =>
        new PassConfiguration(
            maxPassesPerUser: 10,
            allowMultiplePassTypes: true,
            isRefundable: true,
            isTransferable: false);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return MaxPassesPerUser;
        yield return AllowMultiplePassTypes;
        yield return IsRefundable;
        yield return IsTransferable;
    }
}
