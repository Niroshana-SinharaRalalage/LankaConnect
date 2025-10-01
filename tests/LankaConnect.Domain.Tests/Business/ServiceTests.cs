using LankaConnect.Domain.Business;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Domain.Tests.Business;

public class ServiceTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        var name = "Web Development";
        var description = "Full-stack web development services";
        var price = Money.Create(50000, Currency.LKR).Value;
        var duration = "2-4 weeks";
        var businessId = Guid.NewGuid();

        var result = Service.Create(name, description, price, duration, businessId);

        Assert.True(result.IsSuccess);
        var service = result.Value;
        Assert.Equal(name, service.Name);
        Assert.Equal(description, service.Description);
        Assert.Equal(price, service.Price);
        Assert.Equal(duration, service.Duration);
        Assert.Equal(businessId, service.BusinessId);
        Assert.True(service.IsActive);
        Assert.NotEqual(Guid.Empty, service.Id);
    }

    [Fact]
    public void Create_WithMinimalData_ShouldReturnSuccess()
    {
        var name = "Basic Service";
        var description = "Basic service description";

        var result = Service.Create(name, description);

        Assert.True(result.IsSuccess);
        var service = result.Value;
        Assert.Equal(name, service.Name);
        Assert.Equal(description, service.Description);
        Assert.Null(service.Price);
        Assert.Null(service.Duration);
        Assert.Equal(Guid.Empty, service.BusinessId);
        Assert.True(service.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldReturnFailure(string name)
    {
        var description = "Valid description";

        var result = Service.Create(name, description);

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

        var result = Service.Create(name, description);

        Assert.True(result.IsFailure);
        Assert.Contains("Service description is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongName_ShouldReturnFailure()
    {
        var name = new string('a', 201);
        var description = "Valid description";

        var result = Service.Create(name, description);

        Assert.True(result.IsFailure);
        Assert.Contains("Service name cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthName_ShouldReturnSuccess()
    {
        var name = new string('a', 200);
        var description = "Valid description";

        var result = Service.Create(name, description);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
    }

    [Fact]
    public void Create_WithTooLongDescription_ShouldReturnFailure()
    {
        var name = "Valid Name";
        var description = new string('a', 1001);

        var result = Service.Create(name, description);

        Assert.True(result.IsFailure);
        Assert.Contains("Service description cannot exceed 1000 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthDescription_ShouldReturnSuccess()
    {
        var name = "Valid Name";
        var description = new string('a', 1000);

        var result = Service.Create(name, description);

        Assert.True(result.IsSuccess);
        Assert.Equal(description, result.Value.Description);
    }

    [Fact]
    public void Create_WithTooLongDuration_ShouldReturnFailure()
    {
        var name = "Valid Name";
        var description = "Valid description";
        var duration = new string('a', 101);

        var result = Service.Create(name, description, duration: duration);

        Assert.True(result.IsFailure);
        Assert.Contains("Duration cannot exceed 100 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthDuration_ShouldReturnSuccess()
    {
        var name = "Valid Name";
        var description = "Valid description";
        var duration = new string('a', 100);

        var result = Service.Create(name, description, duration: duration);

        Assert.True(result.IsSuccess);
        Assert.Equal(duration, result.Value.Duration);
    }

    [Fact]
    public void Create_WithEmptyGuidBusinessId_ShouldReturnFailure()
    {
        var name = "Valid Name";
        var description = "Valid description";

        var result = Service.Create(name, description, businessId: Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid business ID", result.Errors);
    }

    [Fact]
    public void Create_WithValidBusinessId_ShouldReturnSuccess()
    {
        var name = "Valid Name";
        var description = "Valid description";
        var businessId = Guid.NewGuid();

        var result = Service.Create(name, description, businessId: businessId);

        Assert.True(result.IsSuccess);
        Assert.Equal(businessId, result.Value.BusinessId);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromInputs()
    {
        var name = "  Service Name  ";
        var description = "  Service Description  ";
        var duration = "  2-3 weeks  ";

        var result = Service.Create(name, description, duration: duration);

        Assert.True(result.IsSuccess);
        var service = result.Value;
        Assert.Equal("Service Name", service.Name);
        Assert.Equal("Service Description", service.Description);
        Assert.Equal("2-3 weeks", service.Duration);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateSuccessfully()
    {
        var service = CreateValidService();
        var newName = "Updated Service Name";
        var newDescription = "Updated service description";
        var newPrice = Money.Create(75000, Currency.LKR).Value;
        var newDuration = "3-5 weeks";

        var result = service.Update(newName, newDescription, newPrice, newDuration);

        Assert.True(result.IsSuccess);
        Assert.Equal(newName, service.Name);
        Assert.Equal(newDescription, service.Description);
        Assert.Equal(newPrice, service.Price);
        Assert.Equal(newDuration, service.Duration);
        Assert.NotNull(service.UpdatedAt);
    }

    [Fact]
    public void Update_WithMinimalData_ShouldUpdateSuccessfully()
    {
        var service = CreateValidService();
        var newName = "Updated Name";
        var newDescription = "Updated description";

        var result = service.Update(newName, newDescription);

        Assert.True(result.IsSuccess);
        Assert.Equal(newName, service.Name);
        Assert.Equal(newDescription, service.Description);
        Assert.Null(service.Price);
        Assert.Null(service.Duration);
        Assert.NotNull(service.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Update_WithInvalidName_ShouldReturnFailure(string name)
    {
        var service = CreateValidService();

        var result = service.Update(name, "Valid description");

        Assert.True(result.IsFailure);
        Assert.Contains("Service name is required", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Update_WithInvalidDescription_ShouldReturnFailure(string description)
    {
        var service = CreateValidService();

        var result = service.Update("Valid Name", description);

        Assert.True(result.IsFailure);
        Assert.Contains("Service description is required", result.Errors);
    }

    [Fact]
    public void Update_WithTooLongName_ShouldReturnFailure()
    {
        var service = CreateValidService();
        var name = new string('a', 201);

        var result = service.Update(name, "Valid description");

        Assert.True(result.IsFailure);
        Assert.Contains("Service name cannot exceed 200 characters", result.Errors);
    }

    [Fact]
    public void Update_WithTooLongDescription_ShouldReturnFailure()
    {
        var service = CreateValidService();
        var description = new string('a', 1001);

        var result = service.Update("Valid Name", description);

        Assert.True(result.IsFailure);
        Assert.Contains("Service description cannot exceed 1000 characters", result.Errors);
    }

    [Fact]
    public void Update_WithTooLongDuration_ShouldReturnFailure()
    {
        var service = CreateValidService();
        var duration = new string('a', 101);

        var result = service.Update("Valid Name", "Valid description", duration: duration);

        Assert.True(result.IsFailure);
        Assert.Contains("Duration cannot exceed 100 characters", result.Errors);
    }

    [Fact]
    public void Update_ShouldTrimWhitespaceFromInputs()
    {
        var service = CreateValidService();
        var name = "  Updated Name  ";
        var description = "  Updated Description  ";
        var duration = "  Updated Duration  ";

        var result = service.Update(name, description, duration: duration);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", service.Name);
        Assert.Equal("Updated Description", service.Description);
        Assert.Equal("Updated Duration", service.Duration);
    }

    [Fact]
    public void Activate_WhenInactive_ShouldActivateSuccessfully()
    {
        var service = CreateValidService();
        service.Deactivate(); // Make it inactive first

        var result = service.Activate();

        Assert.True(result.IsSuccess);
        Assert.True(service.IsActive);
        Assert.NotNull(service.UpdatedAt);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldReturnFailure()
    {
        var service = CreateValidService(); // Active by default

        var result = service.Activate();

        Assert.True(result.IsFailure);
        Assert.Contains("Service is already active", result.Errors);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivateSuccessfully()
    {
        var service = CreateValidService(); // Active by default

        var result = service.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.False(service.IsActive);
        Assert.NotNull(service.UpdatedAt);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldReturnFailure()
    {
        var service = CreateValidService();
        service.Deactivate(); // Make it inactive first

        var result = service.Deactivate();

        Assert.True(result.IsFailure);
        Assert.Contains("Service is already inactive", result.Errors);
    }

    [Fact]
    public void Create_DefaultValues_ShouldBeSetCorrectly()
    {
        var service = CreateValidService();

        Assert.True(service.IsActive);
        Assert.True(service.CreatedAt > DateTime.MinValue);
        Assert.Null(service.UpdatedAt);
        Assert.NotEqual(Guid.Empty, service.Id);
    }

    [Fact]
    public void Update_ShouldSetUpdatedAt()
    {
        var service = CreateValidService();
        var originalUpdatedAt = service.UpdatedAt;

        service.Update("Updated Name", "Updated Description");

        Assert.NotEqual(originalUpdatedAt, service.UpdatedAt);
        Assert.NotNull(service.UpdatedAt);
    }

    [Fact]
    public void Activate_ShouldSetUpdatedAt()
    {
        var service = CreateValidService();
        service.Deactivate();
        var originalUpdatedAt = service.UpdatedAt;
        
        // Small delay to ensure different timestamps
        Thread.Sleep(1);

        service.Activate();

        Assert.NotEqual(originalUpdatedAt, service.UpdatedAt);
        Assert.NotNull(service.UpdatedAt);
    }

    [Fact]
    public void Deactivate_ShouldSetUpdatedAt()
    {
        var service = CreateValidService();
        var originalUpdatedAt = service.UpdatedAt;

        service.Deactivate();

        Assert.NotEqual(originalUpdatedAt, service.UpdatedAt);
        Assert.NotNull(service.UpdatedAt);
    }

    [Fact]
    public void Create_WithNullPrice_ShouldAllowNullPrice()
    {
        var result = Service.Create("Service Name", "Service Description", price: null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Price);
    }

    [Fact]
    public void Create_WithNullDuration_ShouldAllowNullDuration()
    {
        var result = Service.Create("Service Name", "Service Description", duration: null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Duration);
    }

    [Fact]
    public void Create_WithDefaultBusinessId_ShouldAllowDefaultGuid()
    {
        var result = Service.Create("Service Name", "Service Description", businessId: default);

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Empty, result.Value.BusinessId);
    }

    [Fact]
    public void Update_WithNullPrice_ShouldAllowNullPrice()
    {
        var service = CreateValidService();

        var result = service.Update("Updated Name", "Updated Description", price: null);

        Assert.True(result.IsSuccess);
        Assert.Null(service.Price);
    }

    [Fact]
    public void Update_WithNullDuration_ShouldAllowNullDuration()
    {
        var service = CreateValidService();

        var result = service.Update("Updated Name", "Updated Description", duration: null);

        Assert.True(result.IsSuccess);
        Assert.Null(service.Duration);
    }

    [Fact]
    public void BusinessId_ShouldBeSetCorrectly()
    {
        var businessId = Guid.NewGuid();
        var service = Service.Create("Service Name", "Service Description", businessId: businessId).Value;

        Assert.Equal(businessId, service.BusinessId);
    }

    [Fact]
    public void Create_WithComplexServiceName_ShouldHandleSpecialCharacters()
    {
        var name = "Web Development & Design (Full-Stack)";
        var description = "Comprehensive web development including frontend, backend, and database design";

        var result = Service.Create(name, description);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value.Name);
    }

    [Fact]
    public void Create_WithMultilineDescription_ShouldAcceptMultilineText()
    {
        var name = "Consulting Service";
        var description = "Line 1 of description\nLine 2 of description\nLine 3 of description";

        var result = Service.Create(name, description);

        Assert.True(result.IsSuccess);
        Assert.Equal(description, result.Value.Description);
    }

    private static Service CreateValidService()
    {
        var price = Money.Create(50000, Currency.LKR).Value;
        return Service.Create("Web Development", "Full-stack web development services", 
            price, "2-4 weeks", Guid.NewGuid()).Value;
    }
}