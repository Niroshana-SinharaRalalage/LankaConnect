using System.Net;
using System.Text;
using System.Text.Json;
using LankaConnect.Application.Businesses.Commands.CreateBusiness;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.IntegrationTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LankaConnect.IntegrationTests.Controllers;

public class BusinessesControllerTests : DockerComposeWebApiTestBase
{
    private readonly JsonSerializerOptions _jsonOptions;

    public BusinessesControllerTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }


    [Fact]
    public async Task CreateBusiness_Should_Return_Created_With_Valid_Data()
    {
        // Arrange
        var command = new CreateBusinessCommand(
            Name: "Test Restaurant",
            Description: "A great test restaurant",
            ContactPhone: "+94771234567",
            ContactEmail: "test@restaurant.com",
            Website: "https://www.testrestaurant.com",
            Address: "123 Test Street",
            City: "Colombo",
            Province: "Western Province",
            PostalCode: "00100",
            Latitude: 6.9271,
            Longitude: 79.8612,
            Category: BusinessCategory.Restaurant,
            OwnerId: Guid.NewGuid(),
            Categories: new List<string> { "Dining", "Takeaway" },
            Tags: new List<string> { "Sri Lankan Cuisine" }
        );

        var json = JsonSerializer.Serialize(command, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await HttpClient.PostAsync("/api/businesses", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent, _jsonOptions);
        
        Assert.True(result.TryGetProperty("id", out var idProperty));
        Assert.True(Guid.TryParse(idProperty.GetString(), out _));
    }

    [Fact]
    public async Task GetBusiness_Should_Return_NotFound_For_Invalid_Id()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/api/businesses/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBusinesses_Should_Return_Paginated_List()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/businesses?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
        
        Assert.True(result.TryGetProperty("items", out _));
        Assert.True(result.TryGetProperty("pageNumber", out _));
        Assert.True(result.TryGetProperty("totalPages", out _));
        Assert.True(result.TryGetProperty("totalCount", out _));
    }
}