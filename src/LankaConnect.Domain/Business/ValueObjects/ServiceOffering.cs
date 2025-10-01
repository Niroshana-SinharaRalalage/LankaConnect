using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Business.ValueObjects;

public class ServiceOffering : ValueObject
{
    public string Name { get; }
    public string Description { get; }
    public ServiceType Type { get; }
    public Money Price { get; }
    public string? Duration { get; }
    public bool IsActive { get; }
    public List<string> Features { get; }

    private ServiceOffering(
        string name,
        string description,
        ServiceType type,
        Money price,
        string? duration,
        bool isActive,
        List<string> features)
    {
        Name = name;
        Description = description;
        Type = type;
        Price = price;
        Duration = duration;
        IsActive = isActive;
        Features = features;
    }

    public static Result<ServiceOffering> Create(
        string name,
        string description,
        ServiceType type,
        Money price,
        string? duration = null,
        bool isActive = true,
        List<string>? features = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<ServiceOffering>.Failure("Service name is required");

        if (string.IsNullOrWhiteSpace(description))
            return Result<ServiceOffering>.Failure("Service description is required");

        if (name.Length > 200)
            return Result<ServiceOffering>.Failure("Service name cannot exceed 200 characters");

        if (description.Length > 1000)
            return Result<ServiceOffering>.Failure("Service description cannot exceed 1000 characters");

        if (price == null)
            return Result<ServiceOffering>.Failure("Service price is required");

        if (!string.IsNullOrWhiteSpace(duration) && duration.Length > 100)
            return Result<ServiceOffering>.Failure("Service duration cannot exceed 100 characters");

        var cleanFeatures = features?.Where(f => !string.IsNullOrWhiteSpace(f))
                                   .Select(f => f.Trim())
                                   .ToList() ?? new List<string>();

        return Result<ServiceOffering>.Success(new ServiceOffering(
            name.Trim(),
            description.Trim(),
            type,
            price,
            duration?.Trim(),
            isActive,
            cleanFeatures
        ));
    }

    public ServiceOffering WithUpdatedPrice(Money newPrice)
    {
        return new ServiceOffering(Name, Description, Type, newPrice, Duration, IsActive, Features);
    }

    public ServiceOffering WithUpdatedStatus(bool isActive)
    {
        return new ServiceOffering(Name, Description, Type, Price, Duration, isActive, Features);
    }

    public ServiceOffering WithUpdatedFeatures(List<string> features)
    {
        var cleanFeatures = features?.Where(f => !string.IsNullOrWhiteSpace(f))
                                   .Select(f => f.Trim())
                                   .ToList() ?? new List<string>();
        
        return new ServiceOffering(Name, Description, Type, Price, Duration, IsActive, cleanFeatures);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Description;
        yield return Type;
        yield return Price;
        
        if (Duration != null)
            yield return Duration;
            
        yield return IsActive;
        
        foreach (var feature in Features)
            yield return feature;
    }

    public override string ToString()
    {
        var typeDisplayName = Type switch
        {
            ServiceType.Product => "RetailCommercial",
            ServiceType.Service => "ServiceBased",
            ServiceType.Consultation => "ConsultingProfessional",
            ServiceType.Installation => "InstallationService",
            ServiceType.Maintenance => "MaintenanceService",
            ServiceType.Repair => "RepairService",
            ServiceType.Delivery => "DeliveryService",
            ServiceType.Rental => "RentalService",
            ServiceType.Subscription => "SubscriptionService",
            ServiceType.Training => "TrainingService",
            ServiceType.Support => "SupportService",
            _ => "Other"
        };
        
        var details = new List<string> { $"{Name} ({typeDisplayName})", $"Price: {Price.Amount}" };
        
        if (!string.IsNullOrWhiteSpace(Duration))
            details.Add($"Duration: {Duration}");
            
        if (Features.Any())
            details.Add($"Features: {string.Join(", ", Features)}");
            
        return string.Join(" | ", details);
    }
}