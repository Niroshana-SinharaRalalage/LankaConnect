using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.ValueObjects;

public class TicketType : ValueObject
{
    public string Name { get; }
    public bool IsFree { get; }
    public Money? Price { get; }
    public int MaxAvailable { get; }
    public int MaxPerUser { get; }

    private TicketType(string name, bool isFree, Money? price, int maxAvailable, int maxPerUser)
    {
        Name = name;
        IsFree = isFree;
        Price = price;
        MaxAvailable = maxAvailable;
        MaxPerUser = maxPerUser;
    }

    public static Result<TicketType> CreateFree(string name, int maxAvailable, int maxPerUser = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<TicketType>.Failure("Name is required");

        if (maxAvailable <= 0)
            return Result<TicketType>.Failure("Max available must be greater than 0");

        if (maxPerUser <= 0)
            return Result<TicketType>.Failure("Max per user must be greater than 0");

        return Result<TicketType>.Success(new TicketType(name.Trim(), true, null, maxAvailable, maxPerUser));
    }

    public static Result<TicketType> CreatePaid(string name, Money price, int maxAvailable, int maxPerUser = 1)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<TicketType>.Failure("Name is required");

        if (price == null)
            return Result<TicketType>.Failure("Price is required for paid tickets");

        if (price.IsZero)
            return Result<TicketType>.Failure("Price must be greater than zero for paid tickets");

        if (maxAvailable <= 0)
            return Result<TicketType>.Failure("Max available must be greater than 0");

        if (maxPerUser <= 0)
            return Result<TicketType>.Failure("Max per user must be greater than 0");

        return Result<TicketType>.Success(new TicketType(name.Trim(), false, price, maxAvailable, maxPerUser));
    }

    public bool HasCapacityFor(int requestedQuantity, int currentSold)
    {
        return currentSold + requestedQuantity <= MaxAvailable;
    }

    public bool CanUserPurchase(int requestedQuantity, int userCurrentQuantity)
    {
        return userCurrentQuantity + requestedQuantity <= MaxPerUser;
    }

    public override string ToString() => Name;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
    }
}