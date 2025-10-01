using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Business;

public class Service : BaseEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money? Price { get; private set; }
    public string? Duration { get; private set; }
    public bool IsActive { get; private set; }
    public Guid BusinessId { get; private set; }

    // Navigation property
    public Business Business { get; private set; } = null!;

    private Service() // EF Core constructor
    {
        Name = null!;
        Description = null!;
    }

    private Service(
        string name,
        string description,
        Money? price,
        string? duration,
        Guid businessId) : base()
    {
        Name = name;
        Description = description;
        Price = price;
        Duration = duration;
        BusinessId = businessId;
        IsActive = true;
    }

    public static Result<Service> Create(
        string name,
        string description,
        Money? price = null,
        string? duration = null,
        Guid? businessId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Service>.Failure("Service name is required");

        if (string.IsNullOrWhiteSpace(description))
            return Result<Service>.Failure("Service description is required");

        if (name.Length > 200)
            return Result<Service>.Failure("Service name cannot exceed 200 characters");

        if (description.Length > 1000)
            return Result<Service>.Failure("Service description cannot exceed 1000 characters");

        if (!string.IsNullOrWhiteSpace(duration) && duration.Length > 100)
            return Result<Service>.Failure("Duration cannot exceed 100 characters");

        if (businessId.HasValue && businessId.Value == Guid.Empty)
            return Result<Service>.Failure("Invalid business ID");

        var service = new Service(
            name.Trim(),
            description.Trim(),
            price,
            duration?.Trim(),
            businessId ?? Guid.Empty);

        return Result<Service>.Success(service);
    }

    public Result Update(string name, string description, Money? price = null, string? duration = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Service name is required");

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("Service description is required");

        if (name.Length > 200)
            return Result.Failure("Service name cannot exceed 200 characters");

        if (description.Length > 1000)
            return Result.Failure("Service description cannot exceed 1000 characters");

        if (!string.IsNullOrWhiteSpace(duration) && duration.Length > 100)
            return Result.Failure("Duration cannot exceed 100 characters");

        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        Duration = duration?.Trim();
        MarkAsUpdated();

        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
            return Result.Failure("Service is already active");

        IsActive = true;
        MarkAsUpdated();

        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure("Service is already inactive");

        IsActive = false;
        MarkAsUpdated();

        return Result.Success();
    }
}