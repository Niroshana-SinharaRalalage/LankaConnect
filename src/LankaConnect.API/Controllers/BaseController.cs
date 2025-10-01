using MediatR;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;

namespace LankaConnect.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseController<T> : ControllerBase where T : class
{
    protected readonly IMediator Mediator;
    protected readonly ILogger<T> Logger;

    protected BaseController(IMediator mediator, ILogger<T> logger)
    {
        Mediator = mediator;
        Logger = logger;
    }

    protected IActionResult HandleResult<TResult>(Result<TResult> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var firstError = result.Errors.FirstOrDefault();
        return BadRequest(new ProblemDetails 
        { 
            Detail = firstError,
            Status = 400,
            Title = "Bad Request"
        });
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        var firstError = result.Errors.FirstOrDefault();
        return BadRequest(new ProblemDetails 
        { 
            Detail = firstError,
            Status = 400,
            Title = "Bad Request"
        });
    }

    protected IActionResult HandleResultWithCreated<TResult>(Result<TResult> result, string actionName, object routeValues)
    {
        if (result.IsSuccess)
        {
            return CreatedAtAction(actionName, routeValues, result.Value);
        }

        var firstError = result.Errors.FirstOrDefault();
        return BadRequest(new ProblemDetails 
        { 
            Detail = firstError,
            Status = 400,
            Title = "Bad Request"
        });
    }
}