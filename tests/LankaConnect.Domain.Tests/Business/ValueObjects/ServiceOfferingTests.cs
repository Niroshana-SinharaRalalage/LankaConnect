using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class ServiceOfferingTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        var name = "Web Development";
        var description = "Full-stack web development services including frontend and backend";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;
        var duration = "2-4 weeks";
        var features = new List<string> { "Responsive Design", "Database Integration", "API Development" };

        var result = ServiceOffering.Create(name, description, type, price, duration, true, features);

        Assert.True(result.IsSuccess);
        var service = result.Value;
        Assert.Equal(name, service.Name);
        Assert.Equal(description, service.Description);
        Assert.Equal(type, service.Type);
        Assert.Equal(price, service.Price);
        Assert.Equal(duration, service.Duration);
        Assert.True(service.IsActive);
        Assert.Equal(features, service.Features);
    }

    [Fact]
    public void Create_WithMinimalData_ShouldReturnSuccess()
    {
        var name = "Basic Service";
        var description = "Basic service description";
        var type = ServiceType.Product;
        var price = Money.Create(500, Currency.USD).Value;

        var result = ServiceOffering.Create(name, description, type, price);

        Assert.True(result.IsSuccess);
        var service = result.Value;
        Assert.Equal(name, service.Name);
        Assert.Equal(description, service.Description);
        Assert.Equal(type, service.Type);
        Assert.Equal(price, service.Price);
        Assert.Null(service.Duration);
        Assert.True(service.IsActive); // Default
        Assert.Empty(service.Features); // Default empty list
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldReturnFailure(string name)
    {
        var description = "Valid description";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;

        var result = ServiceOffering.Create(name, description, type, price);

        Assert.True(result.IsFailure);
        Assert.Contains("Service name is required", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidDescription_ShouldReturnFailure(string description)
    {
        var name = "Valid Name";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;

        var result = ServiceOffering.Create(name, description, type, price);

        Assert.True(result.IsFailure);
        Assert.Contains("Service description is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongName_ShouldReturnFailure()
    {
        var name = new string('a', 201);
        var description = "Valid description";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;

        var result = ServiceOffering.Create(name, description, type, price);

        Assert.True(result.IsFailure);
        Assert.Contains("Service name cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthName_ShouldReturnSuccess()
    {
        var name = new string('a', 200);
        var description = "Valid description";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;

        var result = ServiceOffering.Create(name, description, type, price);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
    }

    [Fact]
    public void Create_WithTooLongDescription_ShouldReturnFailure()
    {
        var name = "Valid Name";
        var description = new string('a', 1001);
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;

        var result = ServiceOffering.Create(name, description, type, price);

        Assert.True(result.IsFailure);
        Assert.Contains("Service description cannot exceed 1000 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthDescription_ShouldReturnSuccess()
    {
        var name = "Valid Name";
        var description = new string('a', 1000);
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;

        var result = ServiceOffering.Create(name, description, type, price);

        Assert.True(result.IsSuccess);
        Assert.Equal(description, result.Value.Description);
    }

    [Fact]
    public void Create_WithNullPrice_ShouldReturnFailure()
    {
        var name = "Valid Name";
        var description = "Valid description";
        var type = ServiceType.Consultation;

        var result = ServiceOffering.Create(name, description, type, null!);

        Assert.True(result.IsFailure);
        Assert.Contains("Service price is required", result.Errors);
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldReturnFailure()
    {
        // Test that Money creation fails for negative amounts
        var priceResult = Money.Create(-100, Currency.USD);
        
        Assert.True(priceResult.IsFailure);
        Assert.Contains("Amount cannot be negative", priceResult.Errors);
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldReturnSuccess()
    {
        var name = "Free Service";
        var description = "Free consultation service";
        var type = ServiceType.Consultation;
        var price = Money.Create(0, Currency.USD).Value;

        var result = ServiceOffering.Create(name, description, type, price);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Price.Amount);
    }

    [Fact]
    public void Create_WithTooLongDuration_ShouldReturnFailure()
    {
        var name = "Valid Name";
        var description = "Valid description";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;
        var duration = new string('a', 101);

        var result = ServiceOffering.Create(name, description, type, price, duration);

        Assert.True(result.IsFailure);
        Assert.Contains("Service duration cannot exceed 100 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthDuration_ShouldReturnSuccess()
    {
        var name = "Valid Name";
        var description = "Valid description";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;
        var duration = new string('a', 100);

        var result = ServiceOffering.Create(name, description, type, price, duration);

        Assert.True(result.IsSuccess);
        Assert.Equal(duration, result.Value.Duration);
    }

    [Fact]
    public void Create_WithNullDuration_ShouldReturnSuccess()
    {
        var name = "Valid Name";
        var description = "Valid description";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;

        var result = ServiceOffering.Create(name, description, type, price, null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Duration);
    }

    [Fact]
    public void Create_WithWhitespaceDuration_ShouldTrimAndSucceed()
    {
        var name = "Valid Name";
        var description = "Valid description";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;
        var duration = "  2-3 weeks  ";

        var result = ServiceOffering.Create(name, description, type, price, duration);

        Assert.True(result.IsSuccess);
        Assert.Equal("2-3 weeks", result.Value.Duration);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromInputs()
    {
        var name = "  Service Name  ";
        var description = "  Service Description  ";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;
        var features = new List<string> { "  Feature 1  ", "  Feature 2  " };

        var result = ServiceOffering.Create(name, description, type, price, features: features);

        Assert.True(result.IsSuccess);
        var service = result.Value;
        Assert.Equal("Service Name", service.Name);
        Assert.Equal("Service Description", service.Description);
        Assert.Equal("Feature 1", service.Features[0]);
        Assert.Equal("Feature 2", service.Features[1]);
    }

    [Fact]
    public void Create_WithEmptyAndWhitespaceFeatures_ShouldFilterThem()
    {
        var name = "Valid Name";
        var description = "Valid description";
        var type = ServiceType.Consultation;
        var price = Money.Create(1000, Currency.USD).Value;
        var features = new List<string> { "Valid Feature", "", "  ", null!, "Another Feature" };

        var result = ServiceOffering.Create(name, description, type, price, features: features);

        Assert.True(result.IsSuccess);
        var service = result.Value;
        Assert.Equal(2, service.Features.Count);
        Assert.Contains("Valid Feature", service.Features);
        Assert.Contains("Another Feature", service.Features);
    }

    [Fact]
    public void WithUpdatedPrice_ShouldReturnNewInstanceWithUpdatedPrice()
    {
        var service = CreateValidServiceOffering();
        var newPrice = Money.Create(2000, Currency.USD).Value;

        var updatedService = service.WithUpdatedPrice(newPrice);

        Assert.NotSame(service, updatedService);
        Assert.Equal(newPrice, updatedService.Price);
        Assert.Equal(service.Name, updatedService.Name);
        Assert.Equal(service.Description, updatedService.Description);
        Assert.Equal(service.Type, updatedService.Type);
        Assert.Equal(service.Duration, updatedService.Duration);
        Assert.Equal(service.IsActive, updatedService.IsActive);
        Assert.Equal(service.Features, updatedService.Features);
    }

    [Fact]
    public void WithUpdatedStatus_ShouldReturnNewInstanceWithUpdatedStatus()
    {
        var service = CreateValidServiceOffering();

        var updatedService = service.WithUpdatedStatus(false);

        Assert.NotSame(service, updatedService);
        Assert.False(updatedService.IsActive);
        Assert.Equal(service.Name, updatedService.Name);
        Assert.Equal(service.Description, updatedService.Description);
        Assert.Equal(service.Type, updatedService.Type);
        Assert.Equal(service.Price, updatedService.Price);
        Assert.Equal(service.Duration, updatedService.Duration);
        Assert.Equal(service.Features, updatedService.Features);
    }

    [Fact]
    public void WithUpdatedFeatures_ShouldReturnNewInstanceWithUpdatedFeatures()
    {
        var service = CreateValidServiceOffering();
        var newFeatures = new List<string> { "New Feature 1", "New Feature 2" };

        var updatedService = service.WithUpdatedFeatures(newFeatures);

        Assert.NotSame(service, updatedService);
        Assert.Equal(newFeatures, updatedService.Features);
        Assert.Equal(service.Name, updatedService.Name);
        Assert.Equal(service.Description, updatedService.Description);
        Assert.Equal(service.Type, updatedService.Type);
        Assert.Equal(service.Price, updatedService.Price);
        Assert.Equal(service.Duration, updatedService.Duration);
        Assert.Equal(service.IsActive, updatedService.IsActive);
    }

    [Fact]
    public void WithUpdatedFeatures_WithWhitespaceFeatures_ShouldFilterThem()
    {
        var service = CreateValidServiceOffering();
        var newFeatures = new List<string> { "Valid Feature", "", "  ", null!, "Another Feature" };

        var updatedService = service.WithUpdatedFeatures(newFeatures);

        Assert.Equal(2, updatedService.Features.Count);
        Assert.Contains("Valid Feature", updatedService.Features);
        Assert.Contains("Another Feature", updatedService.Features);
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        var price = Money.Create(1000, Currency.USD).Value;
        var features = new List<string> { "Feature 1", "Feature 2" };

        var service1 = ServiceOffering.Create("Name", "Description", ServiceType.Consultation, 
            price, "Duration", true, features).Value;
        var service2 = ServiceOffering.Create("Name", "Description", ServiceType.Consultation, 
            price, "Duration", true, features).Value;

        Assert.Equal(service1, service2);
    }

    [Fact]
    public void Equality_WithDifferentNames_ShouldNotBeEqual()
    {
        var price = Money.Create(1000, Currency.USD).Value;

        var service1 = ServiceOffering.Create("Name 1", "Description", ServiceType.Consultation, price).Value;
        var service2 = ServiceOffering.Create("Name 2", "Description", ServiceType.Consultation, price).Value;

        Assert.NotEqual(service1, service2);
    }

    [Fact]
    public void Equality_WithDifferentTypes_ShouldNotBeEqual()
    {
        var price = Money.Create(1000, Currency.USD).Value;

        var service1 = ServiceOffering.Create("Name", "Description", ServiceType.Consultation, price).Value;
        var service2 = ServiceOffering.Create("Name", "Description", ServiceType.Product, price).Value;

        Assert.NotEqual(service1, service2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        var price = Money.Create(1000, Currency.USD).Value;

        var service1 = ServiceOffering.Create("Name", "Description", ServiceType.Consultation, price).Value;
        var service2 = ServiceOffering.Create("Name", "Description", ServiceType.Consultation, price).Value;

        Assert.Equal(service1.GetHashCode(), service2.GetHashCode());
    }

    [Fact]
    public void ToString_WithAllProperties_ShouldFormatCorrectly()
    {
        var price = Money.Create(1500, Currency.USD).Value;
        var features = new List<string> { "Feature 1", "Feature 2" };
        var service = ServiceOffering.Create("Web Development", "Full-stack development", 
            ServiceType.Consultation, price, "2-4 weeks", true, features).Value;

        var result = service.ToString();

        Assert.Contains("Web Development", result);
        Assert.Contains("ConsultingProfessional", result);
        Assert.Contains("1500", result);
        Assert.Contains("2-4 weeks", result);
        Assert.Contains("Feature 1, Feature 2", result);
    }

    [Fact]
    public void ToString_WithMinimalData_ShouldFormatCorrectly()
    {
        var price = Money.Create(1000, Currency.USD).Value;
        var service = ServiceOffering.Create("Basic Service", "Description", ServiceType.Product, price).Value;

        var result = service.ToString();

        Assert.Contains("Basic Service", result);
        Assert.Contains("RetailCommercial", result);
        Assert.Contains("1000", result);
        Assert.DoesNotContain("Duration:", result);
        Assert.DoesNotContain("Features:", result);
    }

    [Theory]
    [InlineData(ServiceType.Product)]
    [InlineData(ServiceType.Service)]
    [InlineData(ServiceType.Consultation)]
    [InlineData(ServiceType.Installation)]
    [InlineData(ServiceType.Maintenance)]
    [InlineData(ServiceType.Repair)]
    [InlineData(ServiceType.Delivery)]
    [InlineData(ServiceType.Rental)]
    [InlineData(ServiceType.Subscription)]
    [InlineData(ServiceType.Other)]
    public void Create_WithAllServiceTypes_ShouldReturnSuccess(ServiceType serviceType)
    {
        var price = Money.Create(1000, Currency.USD).Value;

        var result = ServiceOffering.Create("Service Name", "Service description", serviceType, price);

        Assert.True(result.IsSuccess);
        Assert.Equal(serviceType, result.Value.Type);
    }

    private static ServiceOffering CreateValidServiceOffering()
    {
        var price = Money.Create(1000, Currency.USD).Value;
        var features = new List<string> { "Feature 1", "Feature 2" };
        return ServiceOffering.Create("Service Name", "Service Description", 
            ServiceType.Consultation, price, "1-2 weeks", true, features).Value;
    }
}