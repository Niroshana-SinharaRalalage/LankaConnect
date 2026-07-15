namespace LankaConnect.Products.LankaTemples.Api.Controllers;

/// <summary>
/// LankaTemples product controller. Phase B scaffolding per Consult #27 Q5 (2026-07-15) —
/// contract-first stubs return HTTP 501 Not Implemented until command / query handlers
/// land. When Wave 8.5.f + 8.5.h close (Phase A.5), cross-module write handlers can be
/// authored here per the LankaEventsController pattern.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class TemplesController : ControllerBase
{
    private readonly ILogger<TemplesController> _logger;

    public TemplesController(ILogger<TemplesController> logger)
    {
        _logger = logger;
    }

    /// <summary>Placeholder GET all temples endpoint. Returns 501 until handler lands.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetAll()
    {
        _logger.LogInformation("TemplesController.GetAll invoked — Phase B scaffolding stub.");
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            title = "Not Implemented",
            status = 501,
            detail = "LankaTemples product scaffolded but no handlers landed yet. Awaiting Phase B execution + Wave 8.5.f + 8.5.h close.",
            reference = "docs/PHASE_A_5_PLAN.md Phase B Kick-Off Ruling"
        });
    }

    /// <summary>Placeholder GET by id. Returns 501.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetById(Guid id)
    {
        _logger.LogInformation("TemplesController.GetById invoked for {TempleId} — Phase B scaffolding stub.", id);
        return StatusCode(StatusCodes.Status501NotImplemented);
    }
}
