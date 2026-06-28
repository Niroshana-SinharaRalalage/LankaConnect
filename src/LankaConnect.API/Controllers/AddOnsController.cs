using LankaConnect.API.Extensions;
using LankaConnect.Products.LankaEvents.Application.Commands.ClearAddOnDefinitionImage;
using LankaConnect.Products.LankaEvents.Application.Commands.CreateAddOnDefinition;
using LankaConnect.Products.LankaEvents.Application.Commands.SetAddOnDefinitionImage;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateAddOnDefinition;
using LankaConnect.Products.LankaEvents.Application.Commands.PurchaseAddOn;
using LankaConnect.Products.LankaEvents.Application.Commands.PurchaseAddOnCart;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.ExportAddOnPurchases;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;
using LankaConnect.Application.Events.Queries.GetEventById;
using LankaConnect.Application.Events.Queries.GetEventAddOnPurchases;
using LankaConnect.Application.Events.Queries.GetMyAddOnPurchases;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Add-On Feature: REST endpoints for event add-on definitions and purchases.
/// Route: api/events/{eventId}/add-ons
/// </summary>
[ApiController]
[Route("api/events/{eventId}/add-ons")]
[Produces("application/json")]
public class AddOnsController : BaseController<AddOnsController>
{
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;

    public AddOnsController(
        IMediator mediator,
        ILogger<AddOnsController> logger,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository)
        : base(mediator, logger)
    {
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
    }

    // ──────────────────────────────────────────────
    // Organizer CRUD for add-on definitions
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all add-on definitions for an event (public).
    /// Returns only the definitions list, not purchases or summary.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<AddOnDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAddOnDefinitions(Guid eventId)
    {
        Logger.LogInformation("GetAddOnDefinitions: EventId={EventId}", eventId);

        var result = await Mediator.Send(new GetEventAddOnPurchasesQuery(eventId));

        if (result.IsSuccess)
        {
            return Ok(result.Value.Definitions);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Creates a new add-on definition for an event (organizer only).
    /// Returns the new definition ID.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAddOnDefinition(
        Guid eventId,
        [FromBody] CreateAddOnDefinitionRequest request)
    {
        Logger.LogInformation(
            "CreateAddOnDefinition: EventId={EventId}, Name={Name}, Price={Price}",
            eventId, request.Name, request.Price);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var command = new CreateAddOnDefinitionCommand(
            EventId: eventId,
            Name: request.Name,
            Description: request.Description,
            Price: request.Price,
            Currency: request.Currency ?? "USD",
            QuantityLimit: request.QuantityLimit,
            SortOrder: request.SortOrder);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates an existing add-on definition (organizer only).
    /// Can also be used to deactivate a definition by setting IsActive = false.
    /// </summary>
    [HttpPut("{definitionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateAddOnDefinition(
        Guid eventId,
        Guid definitionId,
        [FromBody] UpdateAddOnDefinitionRequest request)
    {
        Logger.LogInformation(
            "UpdateAddOnDefinition: EventId={EventId}, DefinitionId={DefinitionId}, Name={Name}, IsActive={IsActive}",
            eventId, definitionId, request.Name, request.IsActive);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var command = new UpdateAddOnDefinitionCommand(
            EventId: eventId,
            DefinitionId: definitionId,
            Name: request.Name,
            Description: request.Description,
            Price: request.Price,
            Currency: request.Currency ?? "USD",
            QuantityLimit: request.QuantityLimit,
            SortOrder: request.SortOrder,
            IsActive: request.IsActive);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // ──────────────────────────────────────────────
    // Phase 6A.143 — Add-on image upload (organizer only)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Phase 6A.143 — uploads (or replaces) the display image for a single
    /// add-on definition. Returns the new image URL + blob name.
    /// </summary>
    [HttpPost("{definitionId}/image")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SetAddOnDefinitionImageResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAddOnImage(
        Guid eventId,
        Guid definitionId,
        IFormFile image)
    {
        Logger.LogInformation(
            "SetAddOnImage: EventId={EventId}, DefinitionId={DefinitionId}, FileName={FileName}, Size={Size}",
            eventId, definitionId, image?.FileName, image?.Length);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        if (image is null || image.Length == 0)
            return BadRequest(new ProblemDetails { Title = "An image file is required." });

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);

        var command = new SetAddOnDefinitionImageCommand
        {
            EventId = eventId,
            DefinitionId = definitionId,
            ImageData = ms.ToArray(),
            FileName = image.FileName
        };

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.143 — clears the display image from a single add-on definition.
    /// Idempotent.
    /// </summary>
    [HttpDelete("{definitionId}/image")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearAddOnImage(Guid eventId, Guid definitionId)
    {
        Logger.LogInformation(
            "ClearAddOnImage: EventId={EventId}, DefinitionId={DefinitionId}",
            eventId, definitionId);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new ClearAddOnDefinitionImageCommand
        {
            EventId = eventId,
            DefinitionId = definitionId
        });
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    // ──────────────────────────────────────────────
    // Buyer purchase endpoint
    // ──────────────────────────────────────────────

    /// <summary>
    /// Purchases an add-on for an event.
    /// Returns a Stripe checkout URL for payment.
    /// Anonymous users can purchase without authentication.
    /// </summary>
    [HttpPost("{definitionId}/purchase")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurchaseAddOn(
        Guid eventId,
        Guid definitionId,
        [FromBody] PurchaseAddOnRequest request)
    {
        Logger.LogInformation(
            "PurchaseAddOn: EventId={EventId}, DefinitionId={DefinitionId}, BuyerEmail={BuyerEmail}, Quantity={Quantity}",
            eventId, definitionId, request.BuyerEmail, request.Quantity);

        var userId = User.Identity?.IsAuthenticated == true
            ? User.TryGetUserId()
            : null;

        var command = new PurchaseAddOnCommand(
            EventId: eventId,
            AddOnDefinitionId: definitionId,
            BuyerName: request.BuyerName,
            BuyerEmail: request.BuyerEmail,
            BuyerPhone: request.BuyerPhone,
            Quantity: request.Quantity,
            SuccessUrl: request.SuccessUrl,
            CancelUrl: request.CancelUrl,
            UserId: userId);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Purchases multiple add-ons in a single cart checkout.
    /// Creates one Stripe session with N line items (one per add-on).
    /// Free items complete immediately; only paid items go to Stripe.
    /// Returns: checkout URL if any paid items, or success URL if all free.
    /// </summary>
    [HttpPost("purchase-cart")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurchaseAddOnCart(
        Guid eventId,
        [FromBody] PurchaseAddOnCartRequest request)
    {
        Logger.LogInformation(
            "PurchaseAddOnCart: EventId={EventId}, BuyerEmail={BuyerEmail}, ItemCount={ItemCount}",
            eventId, request.BuyerEmail, request.Items?.Count ?? 0);

        var userId = User.Identity?.IsAuthenticated == true
            ? User.TryGetUserId()
            : null;

        var cartItems = request.Items?.Select(i => new AddOnCartItem(
            i.AddOnDefinitionId,
            i.Quantity)).ToList() ?? new List<AddOnCartItem>();

        var command = new PurchaseAddOnCartCommand(
            EventId: eventId,
            Items: cartItems,
            BuyerName: request.BuyerName,
            BuyerEmail: request.BuyerEmail,
            BuyerPhone: request.BuyerPhone,
            SuccessUrl: request.SuccessUrl,
            CancelUrl: request.CancelUrl,
            UserId: userId);

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // ──────────────────────────────────────────────
    // Public buyer lookup
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets add-on purchases for a specific buyer email (public).
    /// Allows buyers to see their purchase history on the event details page.
    /// Only returns Completed and Pending purchases.
    /// </summary>
    [HttpGet("my-purchases")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<AddOnPurchaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyAddOnPurchases(Guid eventId, [FromQuery] string email)
    {
        Logger.LogInformation(
            "GetMyAddOnPurchases: EventId={EventId}, BuyerEmail={BuyerEmail}",
            eventId, email);

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { Error = "Email parameter is required" });
        }

        var result = await Mediator.Send(new GetMyAddOnPurchasesQuery(eventId, email));
        return HandleResult(result);
    }

    /// <summary>
    /// Gets the authenticated user's own add-on purchases for an event.
    /// Returns individual purchase line items for the logged-in user.
    /// Mirrors the /sponsors/mine pattern for consistent UX.
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(List<AddOnPurchaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAddOns(Guid eventId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "GetMyAddOns: EventId={EventId}, UserId={UserId}", eventId, userId);

        try
        {
            var purchases = await _addOnPurchaseRepository.GetByUserIdAndEventIdAsync(userId, eventId);

            // Build definition name lookup
            var definitions = await _addOnDefinitionRepository.GetByEventIdAsync(eventId);
            var definitionLookup = definitions.ToDictionary(d => d.Id, d => d.Name);

            var purchaseDtos = purchases.Select(p => new AddOnPurchaseDto
            {
                Id = p.Id,
                EventId = p.EventId,
                AddOnDefinitionId = p.AddOnDefinitionId,
                AddOnName = definitionLookup.TryGetValue(p.AddOnDefinitionId, out var name) ? name : "Unknown",
                RegistrationId = p.RegistrationId,
                BuyerUserId = p.BuyerUserId,
                BuyerName = p.BuyerName,
                BuyerEmail = p.BuyerEmail,
                BuyerPhone = p.BuyerPhone,
                Quantity = p.Quantity,
                UnitPrice = p.UnitPrice.Amount,
                TotalAmount = p.TotalAmount.Amount,
                Currency = p.UnitPrice.Currency.ToString(),
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                PaymentCompletedAt = p.PaymentCompletedAt
            }).ToList();

            Logger.LogInformation(
                "GetMyAddOns: Found {Count} purchases for UserId={UserId}, EventId={EventId}",
                purchaseDtos.Count, userId, eventId);

            return Ok(purchaseDtos);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "GetMyAddOns FAILED: EventId={EventId}, UserId={UserId}",
                eventId, userId);
            return StatusCode(500, new { Error = "Failed to retrieve your add-on purchases" });
        }
    }

    // ──────────────────────────────────────────────
    // Organizer views
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gets all add-on purchases for an event (organizer only).
    /// Returns full response with definitions, purchases, and summary.
    /// </summary>
    [HttpGet("purchases")]
    [Authorize]
    [ProducesResponseType(typeof(EventAddOnPurchasesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAddOnPurchases(Guid eventId)
    {
        Logger.LogInformation("GetAddOnPurchases: EventId={EventId}", eventId);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new GetEventAddOnPurchasesQuery(eventId));
        return HandleResult(result);
    }

    /// <summary>
    /// Gets add-on purchase summary for an event (organizer only).
    /// </summary>
    [HttpGet("purchases/summary")]
    [Authorize]
    [ProducesResponseType(typeof(AddOnPurchaseSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAddOnPurchaseSummary(Guid eventId)
    {
        Logger.LogInformation("GetAddOnPurchaseSummary: EventId={EventId}", eventId);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var result = await Mediator.Send(new GetEventAddOnPurchasesQuery(eventId));
        if (result.IsSuccess)
        {
            return Ok(result.Value.Summary);
        }

        return HandleResult(result);
    }

    // ──────────────────────────────────────────────
    // Export
    // ──────────────────────────────────────────────

    /// <summary>
    /// Exports add-on purchases for an event in Excel or CSV format (organizer only).
    /// </summary>
    [HttpGet("purchases/export")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAddOnPurchases(
        Guid eventId,
        [FromQuery] string format = "excel")
    {
        Logger.LogInformation(
            "ExportAddOnPurchases: EventId={EventId}, Format={Format}", eventId, format);

        var authResult = await VerifyOrganizerAsync(eventId);
        if (authResult != null) return authResult;

        var exportFormat = format.ToLowerInvariant() switch
        {
            "csv" => ExportFormat.Csv,
            _ => ExportFormat.Excel
        };

        var result = await Mediator.Send(new ExportAddOnPurchasesQuery(eventId, exportFormat));

        if (result.IsSuccess)
        {
            return File(
                result.Value.FileContent,
                result.Value.ContentType,
                result.Value.FileName);
        }

        return HandleResult(result);
    }

    // ──────────────────────────────────────────────
    // Authorization helper
    // ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that the authenticated user is the organizer of the specified event.
    /// Returns null if authorized, or an IActionResult (Forbid/NotFound) if not.
    /// </summary>
    private async Task<IActionResult?> VerifyOrganizerAsync(Guid eventId)
    {
        var userId = User.GetUserId();

        var eventResult = await Mediator.Send(new GetEventByIdQuery(eventId));
        if (eventResult.IsFailure)
        {
            Logger.LogWarning(
                "AddOns authorization failed: Event not found - EventId={EventId}, UserId={UserId}",
                eventId, userId);
            return NotFound(new { Error = "Event not found" });
        }

        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning(
                "User {UserId} attempted to access add-ons for event {EventId} without authorization (organizer: {OrganizerId})",
                userId, eventId, eventResult.Value.OrganizerId);
            return Forbid();
        }

        return null; // Authorized
    }
}

// ──────────────────────────────────────────────
// Request DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Request body for creating an add-on definition.
/// </summary>
public class CreateAddOnDefinitionRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Currency { get; init; }
    public int? QuantityLimit { get; init; }
    public int SortOrder { get; init; }
}

/// <summary>
/// Request body for updating an add-on definition.
/// Set IsActive = false to soft-delete/deactivate.
/// </summary>
public class UpdateAddOnDefinitionRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? Currency { get; init; }
    public int? QuantityLimit { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Request body for purchasing an add-on.
/// </summary>
public class PurchaseAddOnRequest
{
    public required string BuyerName { get; init; }
    public required string BuyerEmail { get; init; }
    public string? BuyerPhone { get; init; }
    public int Quantity { get; init; } = 1;
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
}

/// <summary>
/// Request body for purchasing multiple add-ons in a cart.
/// </summary>
public class PurchaseAddOnCartRequest
{
    public required string BuyerName { get; init; }
    public required string BuyerEmail { get; init; }
    public string? BuyerPhone { get; init; }
    public required List<PurchaseAddOnCartItemRequest> Items { get; init; }
    public required string SuccessUrl { get; init; }
    public required string CancelUrl { get; init; }
}

/// <summary>
/// Single item in a cart purchase request.
/// </summary>
public class PurchaseAddOnCartItemRequest
{
    public Guid AddOnDefinitionId { get; init; }
    public int Quantity { get; init; } = 1;
}
