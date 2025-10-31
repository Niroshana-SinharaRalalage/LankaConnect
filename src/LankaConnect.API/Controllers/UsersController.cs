using MediatR;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Application.Users.Commands.CreateUser;
using LankaConnect.Application.Users.Commands.UploadProfilePhoto;
using LankaConnect.Application.Users.Commands.DeleteProfilePhoto;
using LankaConnect.Application.Users.Commands.UpdateUserLocation;
using LankaConnect.Application.Users.Commands.UpdateCulturalInterests;
using LankaConnect.Application.Users.Commands.UpdateLanguages;
using LankaConnect.Application.Users.Queries.GetUserById;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Users.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.API.Controllers;

public class UsersController : BaseController<UsersController>
{
    public UsersController(IMediator mediator, ILogger<UsersController> logger) : base(mediator, logger)
    {
        Logger.LogInformation("UsersController initialized");
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="command">User creation details</param>
    /// <returns>The created user ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        using (Logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "CreateUser", ["Email"] = command.Email, ["FirstName"] = command.FirstName, ["LastName"] = command.LastName }))
        {
            Logger.LogInformation("Creating new user with email {Email}", command.Email);
            
            var result = await Mediator.Send(command);
            
            if (result.IsSuccess)
            {
                Logger.LogInformation("User created successfully with ID {UserId}", result.Value);
            }
            
            return HandleResultWithCreated(result, nameof(GetUserById), new { id = result.Value });
        }
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        using (Logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "GetUserById", ["UserId"] = id }))
        {
            Logger.LogInformation("Retrieving user with ID {UserId}", id);
            
            var query = new GetUserByIdQuery(id);
            var result = await Mediator.Send(query);
            
            if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
            {
                Logger.LogInformation("User not found with ID {UserId}", id);
                return NotFound();
            }
            
            if (result.IsSuccess)
            {
                Logger.LogInformation("User retrieved successfully with ID {UserId}", id);
            }

            return HandleResult(result);
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>OK if service is healthy</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Uploads a profile photo for a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="image">Profile photo image file</param>
    /// <returns>Upload details including photo URL</returns>
    [HttpPost("{id:guid}/profile-photo")]
    [ProducesResponseType(typeof(UploadProfilePhotoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB limit for profile photos
    public async Task<IActionResult> UploadProfilePhoto(Guid id, IFormFile image)
    {
        using (Logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "UploadProfilePhoto", ["UserId"] = id }))
        {
            if (image == null || image.Length == 0)
            {
                Logger.LogWarning("Profile photo upload failed: No image provided for user {UserId}", id);
                return BadRequest(new ProblemDetails
                {
                    Detail = "Image file is required",
                    Status = 400,
                    Title = "Bad Request"
                });
            }

            Logger.LogInformation("Uploading profile photo for user {UserId}, file size: {FileSize} bytes",
                id, image.Length);

            var command = new UploadProfilePhotoCommand
            {
                UserId = id,
                ImageFile = image
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                Logger.LogInformation("Profile photo uploaded successfully for user {UserId}, URL: {PhotoUrl}",
                    id, result.Value.PhotoUrl);
            }
            else
            {
                Logger.LogWarning("Profile photo upload failed for user {UserId}: {Error}",
                    id, result.Errors.FirstOrDefault());
            }

            return HandleResult(result);
        }
    }

    /// <summary>
    /// Deletes a user's profile photo
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}/profile-photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfilePhoto(Guid id)
    {
        using (Logger.BeginScope(new Dictionary<string, object> { ["Operation"] = "DeleteProfilePhoto", ["UserId"] = id }))
        {
            Logger.LogInformation("Deleting profile photo for user {UserId}", id);

            var command = new DeleteProfilePhotoCommand { UserId = id };
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                Logger.LogInformation("Profile photo deleted successfully for user {UserId}", id);
                return NoContent();
            }

            var firstError = result.Errors.FirstOrDefault();
            Logger.LogWarning("Profile photo deletion failed for user {UserId}: {Error}", id, firstError);

            // Check if it's a "not found" error
            if (firstError?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new ProblemDetails
                {
                    Detail = firstError,
                    Status = 404,
                    Title = "Not Found"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Detail = firstError,
                Status = 400,
                Title = "Bad Request"
            });
        }
    }

    /// <summary>
    /// Updates a user's location
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Location details (city, state, zipCode, country). Pass all null values to clear location.</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}/location")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request)
    {
        using (Logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "UpdateLocation",
            ["UserId"] = id,
            ["City"] = request.City ?? "null",
            ["Country"] = request.Country ?? "null"
        }))
        {
            Logger.LogInformation("Updating location for user {UserId}", id);

            var command = new UpdateUserLocationCommand
            {
                UserId = id,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                Country = request.Country
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                Logger.LogInformation("Location updated successfully for user {UserId}", id);
                return NoContent();
            }

            var firstError = result.Errors.FirstOrDefault();
            Logger.LogWarning("Location update failed for user {UserId}: {Error}", id, firstError);

            // Check if it's a "not found" error
            if (firstError?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new ProblemDetails
                {
                    Detail = firstError,
                    Status = 404,
                    Title = "Not Found"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Detail = firstError,
                Status = 400,
                Title = "Bad Request"
            });
        }
    }

    /// <summary>
    /// Updates a user's cultural interests
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Cultural interests (0-10 allowed). Pass empty list to clear all interests.</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}/cultural-interests")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCulturalInterests(Guid id, [FromBody] UpdateCulturalInterestsRequest request)
    {
        using (Logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "UpdateCulturalInterests",
            ["UserId"] = id,
            ["InterestCount"] = request.InterestCodes.Count
        }))
        {
            Logger.LogInformation("Updating cultural interests for user {UserId} with {Count} interests",
                id, request.InterestCodes.Count);

            var command = new UpdateCulturalInterestsCommand
            {
                UserId = id,
                InterestCodes = request.InterestCodes
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                Logger.LogInformation("Cultural interests updated successfully for user {UserId}", id);
                return NoContent();
            }

            var firstError = result.Errors.FirstOrDefault();
            Logger.LogWarning("Cultural interests update failed for user {UserId}: {Error}", id, firstError);

            // Check if it's a "not found" error
            if (firstError?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new ProblemDetails
                {
                    Detail = firstError,
                    Status = 404,
                    Title = "Not Found"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Detail = firstError,
                Status = 400,
                Title = "Bad Request"
            });
        }
    }

    /// <summary>
    /// Updates a user's languages
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Languages with proficiency levels (1-5 required)</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}/languages")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLanguages(Guid id, [FromBody] UpdateLanguagesRequest request)
    {
        using (Logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "UpdateLanguages",
            ["UserId"] = id,
            ["LanguageCount"] = request.Languages.Count
        }))
        {
            Logger.LogInformation("Updating languages for user {UserId} with {Count} languages",
                id, request.Languages.Count);

            var command = new UpdateLanguagesCommand
            {
                UserId = id,
                Languages = request.Languages.Select(l => new LanguageDto
                {
                    LanguageCode = l.LanguageCode,
                    ProficiencyLevel = l.ProficiencyLevel
                }).ToList()
            };

            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                Logger.LogInformation("Languages updated successfully for user {UserId}", id);
                return NoContent();
            }

            var firstError = result.Errors.FirstOrDefault();
            Logger.LogWarning("Languages update failed for user {UserId}: {Error}", id, firstError);

            // Check if it's a "not found" error
            if (firstError?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new ProblemDetails
                {
                    Detail = firstError,
                    Status = 404,
                    Title = "Not Found"
                });
            }

            return BadRequest(new ProblemDetails
            {
                Detail = firstError,
                Status = 400,
                Title = "Bad Request"
            });
        }
    }
}

/// <summary>
/// Request model for updating user location
/// </summary>
public record UpdateLocationRequest
{
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
}

/// <summary>
/// Request model for updating user's cultural interests
/// </summary>
public record UpdateCulturalInterestsRequest
{
    public List<string> InterestCodes { get; init; } = new();
}

/// <summary>
/// Request model for updating user's languages
/// </summary>
public record UpdateLanguagesRequest
{
    public List<LanguageRequestDto> Languages { get; init; } = new();
}

/// <summary>
/// DTO for language with proficiency level in API requests
/// </summary>
public record LanguageRequestDto
{
    public string LanguageCode { get; init; } = null!;
    public ProficiencyLevel ProficiencyLevel { get; init; }
}