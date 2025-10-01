using MediatR;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Application.Users.Commands.CreateUser;
using LankaConnect.Application.Users.Queries.GetUserById;
using LankaConnect.Application.Users.DTOs;
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
}