using MediatR;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.BuildingBlocks.Domain;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Host.AllInOne.Controllers;

// Day 4 slot C sub-slice 4C.d.vi (2026-07-06): local copy of Host BaseController.
// Media.Api cannot ProjectReference LankaConnect.API (cycle - Host references
// Media.Api). Post-sprint: move BaseController to BuildingBlocks.Web + delete
// both copies (Media.Api's and LankaConnect.API's). Namespace preserved so
// PhotoAlbumsController's `BaseController<PhotoAlbumsController>` reference
// resolves without file rewrite.
// Sprint-Day 7 (2026-07-11) hotfix: restore [ApiController]/[Route]/[Produces]
// class-level attributes that were dropped during 4C.d.vi local-copy extraction.
// Without these, every derived controller registered as GET/POST at ROOT path
// (no /api/{controller} prefix) — /api/PhotoAlbums returned 404 on staging.
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
        => result.IsSuccess ? Ok(result.Value) : BuildProblem(result);

    protected IActionResult HandleResult(Result result)
        => result.IsSuccess ? Ok() : BuildProblem(result);

    protected IActionResult HandleResultWithCreated<TResult>(Result<TResult> result, string actionName, object routeValues)
        => result.IsSuccess ? CreatedAtAction(actionName, routeValues, result.Value) : BuildProblem(result);

    protected IActionResult HandleResultNoContent(Result result)
        => result.IsSuccess ? NoContent() : BuildProblem(result);

    private IActionResult BuildProblem(Result result)
    {
        var firstError = result.Errors.FirstOrDefault() ?? "Operation failed";
        return result.ErrorKind switch
        {
            ErrorKind.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            { Detail = firstError, Status = StatusCodes.Status403Forbidden, Title = "Forbidden" }),
            ErrorKind.NotFound => NotFound(new ProblemDetails
            { Detail = firstError, Status = StatusCodes.Status404NotFound, Title = "Not Found" }),
            ErrorKind.Conflict => Conflict(new ProblemDetails
            { Detail = firstError, Status = StatusCodes.Status409Conflict, Title = "Conflict" }),
            ErrorKind.StructuralEditRejected => UnprocessableEntity(new ProblemDetails
            { Detail = firstError, Status = StatusCodes.Status422UnprocessableEntity, Title = "layout.structural_edit_rejected" }),
            _ => BadRequest(new ProblemDetails
            { Detail = firstError, Status = StatusCodes.Status400BadRequest, Title = "Bad Request" })
        };
    }
}
