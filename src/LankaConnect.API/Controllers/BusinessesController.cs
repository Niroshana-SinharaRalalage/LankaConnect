using LankaConnect.Application.Businesses.Commands.AddService;
using LankaConnect.Application.Businesses.Commands.CreateBusiness;
using LankaConnect.Application.Businesses.Commands.DeleteBusiness;
using LankaConnect.Application.Businesses.Commands.UpdateBusiness;
using LankaConnect.Application.Businesses.Commands.UploadBusinessImage;
using LankaConnect.Application.Businesses.Commands.DeleteBusinessImage;
using LankaConnect.Application.Businesses.Commands.ReorderBusinessImages;
using LankaConnect.Application.Businesses.Commands.SetPrimaryBusinessImage;
using LankaConnect.Application.Businesses.Common;
using LankaConnect.Application.Businesses.Queries.GetBusiness;
using LankaConnect.Application.Businesses.Queries.GetBusinessServices;
using LankaConnect.Application.Businesses.Queries.GetBusinessImages;
using LankaConnect.Application.Businesses.Queries.SearchBusinesses;
using LankaConnect.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BusinessesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BusinessesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new business
    /// </summary>
    /// <param name="command">Business creation data</param>
    /// <returns>The ID of the created business</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateBusinessResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateBusinessResponse>> CreateBusiness([FromBody] CreateBusinessCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }
        
        return CreatedAtAction(
            nameof(GetBusiness),
            new { id = result.Value },
            new CreateBusinessResponse(result.Value));
    }

    /// <summary>
    /// Gets a business by ID
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <returns>Business details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BusinessDto>> GetBusiness(Guid id)
    {
        var query = new GetBusinessQuery(id);
        var business = await _mediator.Send(query);

        if (business == null)
            return NotFound($"Business with ID {id} not found");

        return Ok(business);
    }

    /// <summary>
    /// Updates an existing business
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <param name="request">Business update data</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateBusiness(Guid id, [FromBody] UpdateBusinessRequest request)
    {
        var command = new UpdateBusinessCommand(
            id,
            request.Name,
            request.Description,
            request.ContactPhone,
            request.ContactEmail,
            request.Website,
            request.Address,
            request.City,
            request.Province,
            request.PostalCode,
            request.Latitude,
            request.Longitude,
            request.Categories,
            request.Tags
        );

        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Deletes a business
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBusiness(Guid id)
    {
        var command = new DeleteBusinessCommand(id);
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>
    /// Searches businesses with various filters
    /// </summary>
    /// <param name="searchTerm">Search term for business name or description</param>
    /// <param name="category">Filter by category</param>
    /// <param name="city">Filter by city</param>
    /// <param name="province">Filter by province</param>
    /// <param name="latitude">Latitude for location-based search</param>
    /// <param name="longitude">Longitude for location-based search</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="minRating">Minimum rating filter</param>
    /// <param name="isVerified">Filter by verification status</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 50)</param>
    /// <returns>Paginated list of businesses</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginatedList<BusinessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedList<BusinessDto>>> SearchBusinesses(
        [FromQuery] string? searchTerm,
        [FromQuery] string? category,
        [FromQuery] string? city,
        [FromQuery] string? province,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? radiusKm,
        [FromQuery] decimal? minRating,
        [FromQuery] bool? isVerified,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageSize > 50)
            pageSize = 50;

        var query = new SearchBusinessesQuery(
            searchTerm,
            category,
            city,
            province,
            latitude,
            longitude,
            radiusKm,
            minRating,
            isVerified,
            pageNumber,
            pageSize
        );

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Gets all businesses (paginated)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 50)</param>
    /// <returns>Paginated list of all businesses</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<BusinessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedList<BusinessDto>>> GetAllBusinesses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageSize > 50)
            pageSize = 50;

        var query = new SearchBusinessesQuery(
            null, null, null, null, null, null, null, null, null, pageNumber, pageSize);

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Adds a service to a business
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <param name="request">Service data</param>
    /// <returns>The ID of the created service</returns>
    [HttpPost("{id:guid}/services")]
    [ProducesResponseType(typeof(AddServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AddServiceResponse>> AddService(Guid id, [FromBody] AddServiceRequest request)
    {
        var command = new AddServiceCommand(
            id,
            request.Name,
            request.Description,
            request.Price,
            request.Duration,
            request.IsAvailable
        );

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }
        
        return CreatedAtAction(
            nameof(GetBusinessServices),
            new { id },
            new AddServiceResponse(result.Value));
    }

    /// <summary>
    /// Gets all services for a business
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <returns>List of business services</returns>
    [HttpGet("{id:guid}/services")]
    [ProducesResponseType(typeof(List<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ServiceDto>>> GetBusinessServices(Guid id)
    {
        var query = new GetBusinessServicesQuery(id);
        var services = await _mediator.Send(query);
        return Ok(services);
    }

    /// <summary>
    /// Uploads an image for a business
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <param name="image">Image file to upload</param>
    /// <param name="altText">Alternative text for the image</param>
    /// <param name="caption">Image caption</param>
    /// <param name="isPrimary">Whether this should be the primary image</param>
    /// <param name="displayOrder">Display order for the image</param>
    /// <returns>Uploaded image details</returns>
    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(typeof(UploadBusinessImageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<ActionResult<UploadBusinessImageResponse>> UploadBusinessImage(
        Guid id,
        IFormFile image,
        [FromForm] string? altText = null,
        [FromForm] string? caption = null,
        [FromForm] bool isPrimary = false,
        [FromForm] int displayOrder = 0)
    {
        if (image == null || image.Length == 0)
            return BadRequest("Image file is required");

        var command = new UploadBusinessImageCommand
        {
            BusinessId = id,
            Image = image,
            AltText = altText,
            Caption = caption,
            IsPrimary = isPrimary,
            DisplayOrder = displayOrder
        };

        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }
        
        return CreatedAtAction(
            nameof(GetBusinessImages),
            new { id },
            result.Value);
    }

    /// <summary>
    /// Gets all images for a business
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <returns>List of business images</returns>
    [HttpGet("{id:guid}/images")]
    [ProducesResponseType(typeof(List<BusinessImageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<BusinessImageDto>>> GetBusinessImages(Guid id)
    {
        var query = new GetBusinessImagesQuery(id);
        var images = await _mediator.Send(query);
        return Ok(images);
    }

    /// <summary>
    /// Deletes a business image
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <param name="imageId">Image ID to delete</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}/images/{imageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBusinessImage(Guid id, string imageId)
    {
        var command = new DeleteBusinessImageCommand(id, imageId);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }
        
        return NoContent();
    }

    /// <summary>
    /// Sets a business image as primary
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <param name="imageId">Image ID to set as primary</param>
    /// <returns>No content on success</returns>
    [HttpPatch("{id:guid}/images/{imageId}/set-primary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetPrimaryBusinessImage(Guid id, string imageId)
    {
        var command = new SetPrimaryBusinessImageCommand(id, imageId);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }
        
        return NoContent();
    }

    /// <summary>
    /// Reorders business images
    /// </summary>
    /// <param name="id">Business ID</param>
    /// <param name="request">New order of image IDs</param>
    /// <returns>No content on success</returns>
    [HttpPatch("{id:guid}/images/reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReorderBusinessImages(Guid id, [FromBody] ReorderImagesRequest request)
    {
        var command = new ReorderBusinessImagesCommand(id, request.ImageIds);
        var result = await _mediator.Send(command);
        
        if (!result.IsSuccess)
        {
            return BadRequest(result.Errors);
        }
        
        return NoContent();
    }
}

// Request/Response DTOs
public record CreateBusinessResponse(Guid BusinessId);

public record UpdateBusinessRequest(
    string Name,
    string Description,
    string ContactPhone,
    string ContactEmail,
    string Website,
    string Address,
    string City,
    string Province,
    string PostalCode,
    double Latitude,
    double Longitude,
    List<string> Categories,
    List<string> Tags);

public record AddServiceRequest(
    string Name,
    string Description,
    decimal Price,
    string Duration,
    bool IsAvailable);

public record AddServiceResponse(Guid ServiceId);

public record ReorderImagesRequest(List<string> ImageIds);