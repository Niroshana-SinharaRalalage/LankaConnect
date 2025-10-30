using FluentAssertions;
using LankaConnect.Application.Businesses.Commands.CreateBusiness;
using LankaConnect.Application.Businesses.Commands.UploadBusinessImage;
using LankaConnect.Application.Businesses.Queries.GetBusinessImages;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.IntegrationTests.Common;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace LankaConnect.IntegrationTests.Controllers;

public sealed class BusinessImagesControllerTests : DockerComposeWebApiTestBase
{
    private readonly JsonSerializerOptions _jsonOptions;

    public BusinessImagesControllerTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task UploadBusinessImage_WithValidImage_ShouldReturnCreated()
    {
        // Arrange - First create a business
        var businessId = await CreateTestBusinessAsync();

        // Create form data with image
        using var content = new MultipartFormDataContent();
        
        var imageBytes = CreateValidJpegBytes();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "image", "test.jpg");
        content.Add(new StringContent("Test alt text"), "altText");
        content.Add(new StringContent("Test caption"), "caption");
        content.Add(new StringContent("true"), "isPrimary");
        content.Add(new StringContent("1"), "displayOrder");

        // Act
        var response = await HttpClient.PostAsync($"/api/businesses/{businessId}/images", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var uploadResponse = JsonSerializer.Deserialize<UploadBusinessImageResponse>(responseContent, _jsonOptions);

        uploadResponse.Should().NotBeNull();
        uploadResponse!.ImageId.Should().NotBeEmpty();
        uploadResponse.OriginalUrl.Should().NotBeEmpty();
        uploadResponse.ThumbnailUrl.Should().NotBeEmpty();
        uploadResponse.MediumUrl.Should().NotBeEmpty();
        uploadResponse.LargeUrl.Should().NotBeEmpty();
        uploadResponse.FileSizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetBusinessImages_WithExistingImages_ShouldReturnImages()
    {
        // Arrange - Create business and upload images
        var businessId = await CreateTestBusinessAsync();
        await UploadTestImageAsync(businessId, "image1.jpg", isPrimary: true);
        await UploadTestImageAsync(businessId, "image2.jpg", isPrimary: false);

        // Act
        var response = await HttpClient.GetAsync($"/api/businesses/{businessId}/images");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var images = JsonSerializer.Deserialize<List<BusinessImageDto>>(responseContent, _jsonOptions);

        images.Should().NotBeNull();
        images!.Should().HaveCount(2);
        images.Should().Contain(img => img.IsPrimary);
        images.Should().OnlyContain(img => !string.IsNullOrEmpty(img.Id));
        images.Should().OnlyContain(img => !string.IsNullOrEmpty(img.OriginalUrl));
    }

    [Fact]
    public async Task DeleteBusinessImage_WithExistingImage_ShouldReturnNoContent()
    {
        // Arrange
        var businessId = await CreateTestBusinessAsync();
        var imageId = await UploadTestImageAsync(businessId, "test.jpg");

        // Act
        var response = await HttpClient.DeleteAsync($"/api/businesses/{businessId}/images/{imageId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify image was deleted
        var getResponse = await HttpClient.GetAsync($"/api/businesses/{businessId}/images");
        var images = JsonSerializer.Deserialize<List<BusinessImageDto>>(
            await getResponse.Content.ReadAsStringAsync(), _jsonOptions);
        
        images.Should().NotBeNull();
        images!.Should().BeEmpty();
    }

    [Fact]
    public async Task SetPrimaryBusinessImage_WithExistingImage_ShouldReturnNoContent()
    {
        // Arrange
        var businessId = await CreateTestBusinessAsync();
        var imageId1 = await UploadTestImageAsync(businessId, "image1.jpg", isPrimary: true);
        var imageId2 = await UploadTestImageAsync(businessId, "image2.jpg", isPrimary: false);

        // Act - Set second image as primary
        var response = await HttpClient.PatchAsync($"/api/businesses/{businessId}/images/{imageId2}/set-primary", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the change
        var getResponse = await HttpClient.GetAsync($"/api/businesses/{businessId}/images");
        var images = JsonSerializer.Deserialize<List<BusinessImageDto>>(
            await getResponse.Content.ReadAsStringAsync(), _jsonOptions);

        images.Should().NotBeNull();
        images!.Should().HaveCount(2);
        images!.Single(img => img.IsPrimary).Id.Should().Be(imageId2);
    }

    [Fact]
    public async Task ReorderBusinessImages_WithValidOrder_ShouldReturnNoContent()
    {
        // Arrange
        var businessId = await CreateTestBusinessAsync();
        var imageId1 = await UploadTestImageAsync(businessId, "image1.jpg");
        var imageId2 = await UploadTestImageAsync(businessId, "image2.jpg");
        var imageId3 = await UploadTestImageAsync(businessId, "image3.jpg");

        var reorderRequest = new { ImageIds = new[] { imageId3, imageId1, imageId2 } };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(reorderRequest, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await HttpClient.PatchAsync($"/api/businesses/{businessId}/images/reorder", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the new order
        var getResponse = await HttpClient.GetAsync($"/api/businesses/{businessId}/images");
        var images = JsonSerializer.Deserialize<List<BusinessImageDto>>(
            await getResponse.Content.ReadAsStringAsync(), _jsonOptions);

        images.Should().NotBeNull();
        images!.Should().HaveCount(3);
        images![0].Id.Should().Be(imageId3);
        images[0].DisplayOrder.Should().Be(0);
        images[1].Id.Should().Be(imageId1);
        images[1].DisplayOrder.Should().Be(1);
        images[2].Id.Should().Be(imageId2);
        images[2].DisplayOrder.Should().Be(2);
    }

    [Fact]
    public async Task UploadBusinessImage_WithInvalidFileType_ShouldReturnBadRequest()
    {
        // Arrange
        var businessId = await CreateTestBusinessAsync();

        using var content = new MultipartFormDataContent();
        var textContent = new ByteArrayContent(Encoding.UTF8.GetBytes("This is not an image"));
        textContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        content.Add(textContent, "image", "test.txt");

        // Act
        var response = await HttpClient.PostAsync($"/api/businesses/{businessId}/images", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadBusinessImage_WithNonExistentBusiness_ShouldReturnBadRequest()
    {
        // Arrange
        var nonExistentBusinessId = Guid.NewGuid();

        using var content = new MultipartFormDataContent();
        var imageBytes = CreateValidJpegBytes();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "image", "test.jpg");

        // Act
        var response = await HttpClient.PostAsync($"/api/businesses/{nonExistentBusinessId}/images", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadBusinessImage_WithLargeFile_ShouldReturnBadRequest()
    {
        // Arrange
        var businessId = await CreateTestBusinessAsync();

        using var content = new MultipartFormDataContent();
        var largeImageBytes = new byte[15 * 1024 * 1024]; // 15MB
        var imageContent = new ByteArrayContent(largeImageBytes);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "image", "large-test.jpg");

        // Act
        var response = await HttpClient.PostAsync($"/api/businesses/{businessId}/images", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<Guid> CreateTestBusinessAsync()
    {
        var createBusinessCommand = new CreateBusinessCommand(
            "Test Business",
            "Test Description",
            "+1-555-0123",
            "test@test.com",
            "https://test.com",
            "123 Test St",
            "Test City",
            "Test Province",
            "12345",
            40.7128,
            -74.0060,
            BusinessCategory.Restaurant,
            Guid.NewGuid(),
            new List<string> { "food" },
            new List<string> { "restaurant", "food" });

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(createBusinessCommand, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await HttpClient.PostAsync("/api/businesses", jsonContent);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var createResponse = JsonSerializer.Deserialize<dynamic>(responseContent);
        
        return Guid.Parse(createResponse?.GetProperty("businessId").GetString() ?? throw new InvalidOperationException());
    }

    private async Task<string> UploadTestImageAsync(Guid businessId, string fileName, bool isPrimary = false)
    {
        using var content = new MultipartFormDataContent();
        
        var imageBytes = CreateValidJpegBytes();
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "image", fileName);
        content.Add(new StringContent($"Alt text for {fileName}"), "altText");
        content.Add(new StringContent($"Caption for {fileName}"), "caption");
        content.Add(new StringContent(isPrimary.ToString().ToLower()), "isPrimary");
        content.Add(new StringContent("0"), "displayOrder");

        var response = await HttpClient.PostAsync($"/api/businesses/{businessId}/images", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var uploadResponse = JsonSerializer.Deserialize<UploadBusinessImageResponse>(responseContent, _jsonOptions);

        return uploadResponse?.ImageId ?? throw new InvalidOperationException();
    }

    private static byte[] CreateValidJpegBytes()
    {
        // Minimal valid JPEG structure for testing
        return new byte[]
        {
            0xFF, 0xD8, // SOI
            0xFF, 0xE0, // APP0
            0x00, 0x10, // Length
            0x4A, 0x46, 0x49, 0x46, 0x00, // "JFIF\0"
            0x01, 0x01, // Version
            0x01, // Units
            0x00, 0x48, // X density
            0x00, 0x48, // Y density
            0x00, 0x00, // Thumbnail dimensions
            0xFF, 0xD9  // EOI
        };
    }
}