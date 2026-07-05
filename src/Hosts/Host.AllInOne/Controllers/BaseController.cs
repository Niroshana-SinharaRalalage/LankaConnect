using MediatR;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Host.AllInOne.Controllers;

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

        return BuildProblem(result);
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return BuildProblem(result);
    }

    protected IActionResult HandleResultWithCreated<TResult>(Result<TResult> result, string actionName, object routeValues)
    {
        if (result.IsSuccess)
        {
            return CreatedAtAction(actionName, routeValues, result.Value);
        }

        return BuildProblem(result);
    }

    /// <summary>
    /// Returns 204 on success (suited to PUT/PATCH/DELETE with no response body) and
    /// delegates failure mapping to <see cref="BuildProblem(Result)"/>.
    /// </summary>
    protected IActionResult HandleResultNoContent(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return BuildProblem(result);
    }

    /// <summary>
    /// Maps a <see cref="Result"/> failure to an HTTP ProblemDetails response.
    /// Slice 5 added non-Validation <see cref="ErrorKind"/> values — previously all failures
    /// became 400; now Forbidden/NotFound/Conflict/StructuralEditRejected map to 403/404/409/422.
    /// Legacy Failure(string) callers still map to 400 (ErrorKind.Validation default).
    /// </summary>
    private IActionResult BuildProblem(Result result)
    {
        var firstError = result.Errors.FirstOrDefault() ?? "Operation failed";

        return result.ErrorKind switch
        {
            ErrorKind.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Detail = firstError,
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden"
            }),

            ErrorKind.NotFound => NotFound(new ProblemDetails
            {
                Detail = firstError,
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found"
            }),

            ErrorKind.Conflict => Conflict(new ProblemDetails
            {
                Detail = firstError,
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict"
            }),

            ErrorKind.StructuralEditRejected => UnprocessableEntity(new ProblemDetails
            {
                Detail = firstError,
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "layout.structural_edit_rejected"
            }),

            // ErrorKind.Validation OR ErrorKind.None (legacy/unexpected) → 400
            _ => BadRequest(new ProblemDetails
            {
                Detail = firstError,
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request"
            })
        };
    }

    /// <summary>
    /// Phase 6A.141 F3 — extract the requesting client IP, preferring the first hop in
    /// <c>X-Forwarded-For</c> when present (Azure Front Door / Container Apps ingress fronts
    /// the API, so <see cref="HttpContext.Connection"/>.RemoteIpAddress would return the
    /// load balancer's internal IP — useless for forensics).
    ///
    /// Lifted from AuthController where it lived as a private helper. Reused now by the
    /// ticket-scan endpoint to populate the audit log's <c>client_ip</c> column. Other
    /// controllers can adopt it too.
    /// </summary>
    protected string? GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Forwarded chain: first hop is the originating client (per RFC 7239 conventions).
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedIps = Request.Headers["X-Forwarded-For"].ToString().Split(',');
            if (forwardedIps.Length > 0 && !string.IsNullOrWhiteSpace(forwardedIps[0]))
            {
                ipAddress = forwardedIps[0].Trim();
            }
        }

        return ipAddress;
    }
}
