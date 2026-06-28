using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.Products.LankaEvents.Application.Commands.CreateEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.DeleteEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.PublishEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.UnpublishEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.CancelEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.PostponeEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.SubmitEventForApproval;
using LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.CancelRsvp;
// Wave 4.4.c.1 (2026-06-23): refund command handlers moved to Payments.Application.
using LankaConnect.Modules.Payments.Application.Commands.WithdrawRefundRequest;
using LankaConnect.Modules.Payments.Application.Commands.ForceCancelStuckRefund;
using LankaConnect.Modules.Payments.Application.Commands.RefundRequests;
using LankaConnect.Modules.Payments.Application.Queries.RefundRequests; // Wave 4.4.c.2 (2026-06-23)
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateRsvp;
using LankaConnect.Products.LankaEvents.Application.Commands.ResendTicketEmail;
using LankaConnect.Products.LankaEvents.Application.Commands.ResendAttendeeConfirmation;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateRegistrationDetails;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateEventOrganizerContact;
using LankaConnect.Products.LankaEvents.Application.Commands.BatchLinkOrganizerContacts;
using LankaConnect.Products.LankaEvents.Application.Commands.UnlinkOrganizerContactUser;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateMaxAttendeesPerRegistration;
using LankaConnect.Products.LankaEvents.Application.Commands.RegisterAnonymousAttendee;
using LankaConnect.Products.LankaEvents.Application.Commands.AdminApproval;
using LankaConnect.Products.LankaEvents.Application.Commands.SendEventNotification;
using LankaConnect.Products.LankaEvents.Application.Commands.SendEventReminder;
using LankaConnect.Application.Events.Queries.GetAllowedRegistrationModes;
using LankaConnect.Application.Events.Queries.GetEventById;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Application.Events.Queries.GetEventsByOrganizer;
using LankaConnect.Application.Events.Queries.GetMyRegisteredEvents;
using LankaConnect.Application.Events.Queries.GetNearbyEvents;
using LankaConnect.Application.Events.Queries.GetUserRsvps;
using LankaConnect.Application.Events.Queries.GetUserRegistrationForEvent;
using LankaConnect.Application.Events.Queries.GetRegistrationById;
using LankaConnect.Application.Events.Queries.GetEventRegistrationByEmail;
using LankaConnect.Application.Events.Queries.GetUpcomingEventsForUser;
using LankaConnect.Application.Events.Queries.GetPendingEventsForApproval;
using LankaConnect.Application.Events.Queries.SearchEvents;
using LankaConnect.Application.Events.Queries.GetFeaturedEvents;
using LankaConnect.Application.Events.Queries.GetEventNotificationHistory;
using LankaConnect.Application.Events.Queries.GetEventReminderHistory;
using LankaConnect.Application.Common.Models;
using LankaConnect.Products.LankaEvents.Application.Commands.AddImageToEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.DeleteEventImage;
using LankaConnect.Products.LankaEvents.Application.Commands.ReorderEventImages;
using LankaConnect.Products.LankaEvents.Application.Commands.SetPrimaryImage;
using LankaConnect.Products.LankaEvents.Application.Commands.ReplaceEventImage;
using LankaConnect.Products.LankaEvents.Application.Commands.AddVideoToEvent;
using LankaConnect.Products.LankaEvents.Application.Commands.DeleteEventVideo;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Analytics.Commands.RecordEventView;
using LankaConnect.Application.Analytics.Commands.RecordEventShare;
using LankaConnect.Products.LankaEvents.Application.Commands.AddToWaitingList;
using LankaConnect.Products.LankaEvents.Application.Commands.RemoveFromWaitingList;
using LankaConnect.Products.LankaEvents.Application.Commands.PromoteFromWaitingList;
using LankaConnect.Application.Events.Queries.GetWaitingList;
using LankaConnect.Application.Events.Queries.GetEventIcs;
// W5.2.a-fix (2026-06-28): AddPassToEvent + RemovePassFromEvent usings removed -- feature deleted.
using LankaConnect.Application.Events.Queries.GetEventAttendees;
using LankaConnect.Application.Events.Queries.ExportEventAttendees;
using LankaConnect.Modules.Forms.Application.Queries.ExportFormResponses;
// W5.2.a-fix (2026-06-28): GetEventPasses using removed -- feature deleted.
using LankaConnect.Products.LankaEvents.Application.Commands.RemoveSignUpListFromEvent;
using LankaConnect.Application.Events.Queries.GetEventSignUpLists;
using LankaConnect.Products.LankaEvents.Application.Commands.CreateSignUpListWithItems;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateSignUpList;
using LankaConnect.Products.LankaEvents.Application.Commands.AddSignUpItem;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateSignUpItem;
using LankaConnect.Products.LankaEvents.Application.Commands.RemoveSignUpItem;
using LankaConnect.Products.LankaEvents.Application.Commands.ReorderSignUpItems;
using LankaConnect.Products.LankaEvents.Application.Commands.CommitToSignUpItem;
using LankaConnect.Products.LankaEvents.Application.Commands.CommitToSignUpItemAnonymous;
using LankaConnect.Products.LankaEvents.Application.Commands.AddOpenSignUpItem;
using LankaConnect.Products.LankaEvents.Application.Commands.AddOpenSignUpItemAnonymous;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateOpenSignUpItem;
using LankaConnect.Products.LankaEvents.Application.Commands.CancelOpenSignUpItem;
using LankaConnect.Application.Events.Queries.CheckEventRegistration;
using LankaConnect.Application.Events.Queries.GetTicket;
using LankaConnect.Application.Events.Queries.GetTicketPdf;
using LankaConnect.Application.Events.Queries.CalculateAdditionPrice;
using LankaConnect.Application.Events.Queries.GetPendingAddition;
using LankaConnect.Modules.Forms.Application.Commands.CreateEventForm;
using LankaConnect.Modules.Forms.Application.Commands.UpdateEventForm;
using LankaConnect.Modules.Forms.Application.Commands.DeleteEventForm;
using LankaConnect.Modules.Forms.Application.Commands.PublishEventForm;
using LankaConnect.Modules.Forms.Application.Commands.CloseEventForm;
using LankaConnect.Modules.Forms.Application.Commands.ReopenEventForm;
using LankaConnect.Modules.Forms.Application.Commands.AddFormQuestion;
using LankaConnect.Modules.Forms.Application.Commands.UpdateFormQuestion;
using LankaConnect.Modules.Forms.Application.Commands.DeleteFormQuestion;
using LankaConnect.Modules.Forms.Application.Commands.ReorderFormQuestions;
using LankaConnect.Modules.Forms.Application.Commands.SubmitFormResponse;
using LankaConnect.Modules.Forms.Application.Commands.UpdateFormResponse;
using LankaConnect.Modules.Forms.Application.Commands.DeleteFormResponse;
using LankaConnect.Modules.Forms.Application.Queries.GetEventForms;
using LankaConnect.Modules.Forms.Application.Queries.GetEventFormDetail;
using LankaConnect.Modules.Forms.Application.Queries.GetFormResponses;
using LankaConnect.Modules.Forms.Application.Queries.GetPublicFormResponses;  // Phase 6A.146
using LankaConnect.Modules.Forms.Application.Queries.GetMyFormResponse;
using LankaConnect.Modules.Forms.Application.Queries.GetMyFormResponseByUserId;
using LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddAttendees;
using LankaConnect.Products.LankaEvents.Application.Commands.CancelPendingAddition;
using LankaConnect.Products.LankaEvents.Application.Commands.AddTicketTier;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateTicketTier;
using LankaConnect.Products.LankaEvents.Application.Commands.RemoveTicketTier;
using LankaConnect.Products.LankaEvents.Application.Commands.SetTicketingMode;
using LankaConnect.Products.LankaEvents.Application.Commands.SetSeatingMode;
using LankaConnect.Products.LankaEvents.Application.Commands.ScanTicket; // Phase 6A.141
using LankaConnect.Application.Events.Queries.GetTicketTiers;
using LankaConnect.API.Extensions;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.API.Controllers;

public class EventsController : BaseController<EventsController>
{
    public EventsController(IMediator mediator, ILogger<EventsController> logger) : base(mediator, logger)
    {
    }

    // ==================== PUBLIC ENDPOINTS ====================

    /// <summary>
    /// Get all events with optional filtering and location-based sorting
    /// Phase 6A.47: Added searchTerm parameter for text-based search
    /// Issue #36: Added statusFilter parameter for user-friendly status group filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] EventStatus? status = null,
        [FromQuery] EventStatusFilter? statusFilter = null,
        [FromQuery] EventCategory? category = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] bool? isFreeOnly = null,
        [FromQuery] string? city = null,
        [FromQuery] string? state = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] decimal? latitude = null,
        [FromQuery] decimal? longitude = null,
        [FromQuery] List<Guid>? metroAreaIds = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool includeAllStatuses = false)
    {
        // Phase 6A.X: Get authenticated user's ID from JWT token to populate UserRegistrationStatus
        // This allows registration badge to display correctly on events listing page
        var authenticatedUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;

        // Use authenticated userId if available, otherwise fall back to query parameter (for location sorting)
        var effectiveUserId = authenticatedUserId ?? userId;

        Logger.LogInformation(
            "Getting events with filters: status={Status}, statusFilter={StatusFilter}, category={Category}, city={City}, state={State}, userId={UserId}, authenticatedUserId={AuthenticatedUserId}, searchTerm={SearchTerm}, includeAllStatuses={IncludeAllStatuses}",
            status, statusFilter, category, city, state, effectiveUserId, authenticatedUserId, searchTerm, includeAllStatuses);

        var query = new GetEventsQuery(
            status,
            statusFilter,
            category,
            startDateFrom,
            startDateTo,
            isFreeOnly,
            city,
            state,
            effectiveUserId,
            latitude,
            longitude,
            metroAreaIds,
            searchTerm,
            includeAllStatuses);

        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Search events using full-text search (Epic 2 Phase 3 - PostgreSQL FTS)
    /// Phase 6A.X Issue #36: Added excludeCancelled parameter to filter out cancelled events
    /// </summary>
    /// <param name="searchTerm">Search term to match against event titles and descriptions</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="isFreeOnly">Optional filter for free events only</param>
    /// <param name="startDateFrom">Optional filter for events starting from this date</param>
    /// <param name="excludeCancelled">If true, excludes cancelled events from results (default: false)</param>
    /// <returns>Paginated list of matching events ordered by relevance</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<EventSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchEvents(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] EventCategory? category = null,
        [FromQuery] bool? isFreeOnly = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] bool excludeCancelled = false)
    {
        Logger.LogInformation("Searching events: term='{SearchTerm}', page={Page}, pageSize={PageSize}, category={Category}, excludeCancelled={ExcludeCancelled}",
            searchTerm, page, pageSize, category, excludeCancelled);

        var query = new SearchEventsQuery(searchTerm, page, pageSize, category, isFreeOnly, startDateFrom, excludeCancelled);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.154: real-time vanity slug availability check.
    /// Returns { available, reason?, message }. Used by organizer form for instant feedback.
    /// </summary>
    [HttpGet("check-slug")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LankaConnect.Application.Events.Queries.CheckVanitySlugAvailability.VanitySlugAvailabilityResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckVanitySlugAvailability([FromQuery] string slug)
    {
        var query = new LankaConnect.Application.Events.Queries.CheckVanitySlugAvailability.CheckVanitySlugAvailabilityQuery(slug ?? string.Empty);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.154: returns the canonical EventDto by vanity slug, or 404.
    /// Used by the public-facing /{slug} Next.js route on lankaconnect.app.
    /// </summary>
    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventByVanitySlug(string slug)
    {
        var query = new LankaConnect.Application.Events.Queries.GetEventByVanitySlug.GetEventByVanitySlugQuery(slug);
        var result = await Mediator.Send(query);
        if (result.IsSuccess && result.Value == null) return NotFound();
        return HandleResult(result);
    }

    /// <summary>
    /// Get event details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        Logger.LogInformation("Getting event by ID: {EventId}", id);

        var query = new GetEventByIdQuery(id);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        // Fire-and-forget: Record event view for analytics (non-blocking).
        //
        // Phase 8 (post-prod-perf-RCA hygiene): the previous version of this
        // block read User.Identity, HttpContext.Connection, HttpContext.Request.Headers,
        // and Mediator INSIDE the Task.Run lambda — all scoped per request.
        // When the controller method returns, the request scope disposes; if
        // the analytics task hadn't finished yet, those reads raised
        // ObjectDisposedException, which surfaced as orphaned background
        // exceptions (architect flagged this in MASTER_TODO_PROD_PERF_RCA).
        //
        // Fix: capture all scope-bound values BEFORE the Task.Run, create a
        // fresh DI scope inside, and resolve a fresh IMediator from that
        // scope. Logger from BaseController is ILogger<T> which is registered
        // as singleton — safe to close over.
        if (result.IsSuccess && result.Value != null)
        {
            var capturedUserId = User.Identity?.IsAuthenticated == true ? User.TryGetUserId() : null;
            var capturedIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            var capturedUserAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var scopeFactory = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
            var loggerRef = Logger;
            var capturedEventId = id;

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var recordViewCommand = new RecordEventViewCommand(
                        capturedEventId, capturedUserId, capturedIpAddress, capturedUserAgent);
                    await scopedMediator.Send(recordViewCommand);

                    loggerRef.LogDebug("Event view recorded for: {EventId}, User: {UserId}, IP: {IpAddress}",
                        capturedEventId, capturedUserId, capturedIpAddress);
                }
                catch (Exception ex)
                {
                    // Fail-silent: don't let analytics errors affect the main request.
                    loggerRef.LogWarning(ex, "Failed to record event view for: {EventId}", capturedEventId);
                }
            });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Get nearby events within a specified radius of a location (Epic 2 Phase 3 - Spatial Queries)
    /// </summary>
    /// <param name="latitude">Latitude coordinate (-90 to 90)</param>
    /// <param name="longitude">Longitude coordinate (-180 to 180)</param>
    /// <param name="radiusKm">Search radius in kilometers (0.1 to 1000)</param>
    /// <param name="category">Optional event category filter</param>
    /// <param name="isFreeOnly">Optional filter for free events only</param>
    /// <param name="startDateFrom">Optional filter for events starting from this date</param>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNearbyEvents(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] double radiusKm,
        [FromQuery] EventCategory? category = null,
        [FromQuery] bool? isFreeOnly = null,
        [FromQuery] DateTime? startDateFrom = null)
    {
        Logger.LogInformation("Getting nearby events: lat={Latitude}, lon={Longitude}, radius={RadiusKm}km",
            latitude, longitude, radiusKm);

        var query = new GetNearbyEventsQuery(latitude, longitude, radiusKm, category, isFreeOnly, startDateFrom);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get featured events for the landing page
    /// Returns up to 4 events sorted by location relevance
    /// - For authenticated users: Uses preferred metro areas or user location
    /// - For anonymous users: Uses provided coordinates or default location
    /// </summary>
    /// <param name="userId">Optional authenticated user ID</param>
    /// <param name="latitude">Optional latitude for anonymous users</param>
    /// <param name="longitude">Optional longitude for anonymous users</param>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeaturedEvents(
        [FromQuery] Guid? userId = null,
        [FromQuery] decimal? latitude = null,
        [FromQuery] decimal? longitude = null)
    {
        Logger.LogInformation("Getting featured events: userId={UserId}, lat={Latitude}, lon={Longitude}",
            userId, latitude, longitude);

        var query = new GetFeaturedEventsQuery(userId, latitude, longitude);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 7E.2: Returns the set of <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode"/>
    /// values compatible with a given draft event shape. Drives the frontend mode picker so
    /// disabled options match server-side validation. All shape parameters default to <c>false</c>.
    /// Public endpoint — no auth needed (the response is shape-only metadata).
    /// </summary>
    [HttpGet("allowed-registration-modes")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllowedRegistrationModes(
        [FromQuery] bool isFreeAttendance = false,
        [FromQuery] bool hasSeating = false,
        [FromQuery] bool hasNamedSeating = false,
        [FromQuery] bool requiresAttendeeNameOnTicket = false,
        [FromQuery] bool hasDualPricing = false,
        [FromQuery] bool hasGroupTiers = false,
        [FromQuery] bool hasTicketTiers = false,
        [FromQuery] bool hasIdentityBoundAddOn = false,
        [FromQuery] bool hasMatrixPricing = false,
        // Phase 8X.11 — payment-mode axis. Defaults to Free; FE picker passes the
        // current paymentMode so External shows up exactly when ExternalPaid.
        [FromQuery] LankaConnect.Products.LankaEvents.Domain.Enums.EventPaymentMode paymentMode =
            LankaConnect.Products.LankaEvents.Domain.Enums.EventPaymentMode.Free)
    {
        var query = new GetAllowedRegistrationModesQuery(
            isFreeAttendance, hasSeating, hasNamedSeating, requiresAttendeeNameOnTicket,
            hasDualPricing, hasGroupTiers, hasTicketTiers, hasIdentityBoundAddOn, hasMatrixPricing,
            paymentMode);

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    // ==================== AUTHENTICATED ENDPOINTS ====================

    /// <summary>
    /// Create a new event (Event Organizers, Admins only)
    /// Phase 6A.3: Requires EventOrganizer, Admin, or AdminManager role with active subscription
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanCreateEvents")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventCommand command)
    {
        // PHASE 6A.10: Comprehensive diagnostic logging
        var userId = User.GetUserId(); // Get authenticated user ID
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

        // CRITICAL FIX: Override OrganizerId with authenticated user ID for security
        // The client should NOT be able to set OrganizerId - it must come from the JWT token
        var secureCommand = command with { OrganizerId = userId };

        Logger.LogInformation("🎯 CreateEvent - Request Details:");
        Logger.LogInformation("   User ID: {UserId}", userId);
        Logger.LogInformation("   User Role: {UserRole}", userRole);
        Logger.LogInformation("   Is Authenticated: {IsAuthenticated}", isAuthenticated);
        Logger.LogInformation("   Event Title: {Title}", secureCommand.Title);
        Logger.LogInformation("   Organizer ID (from JWT): {OrganizerId}", secureCommand.OrganizerId);
        Logger.LogInformation("   Authorization Policy: CanCreateEvents");

        // Log all user claims for debugging
        var claims = User.Claims.Select(c => $"{c.Type}={c.Value}");
        Logger.LogInformation("   User Claims: {Claims}", string.Join(", ", claims));

        Logger.LogInformation("⏳ Sending command to MediatR handler...");
        var result = await Mediator.Send(secureCommand);

        if (result.IsSuccess)
        {
            Logger.LogInformation("✅ Event created successfully: EventId={EventId}", result.Value);
            return HandleResultWithCreated(result, nameof(GetEventById), new { id = result.Value });
        }
        else
        {
            Logger.LogError("❌ Event creation failed: {Errors}", string.Join(", ", result.Errors));
            return HandleResult(result);
        }
    }

    /// <summary>
    /// Update an existing event (Owner only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventCommand command)
    {
        Logger.LogInformation("Updating event: {EventId}", id);

        // Ensure ID in route matches command
        if (id != command.EventId)
        {
            return BadRequest("Event ID mismatch");
        }

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.X: Update event organizer contact details (Owner only)
    /// </summary>
    [HttpPut("{id:guid}/organizer-contact")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEventOrganizerContact(Guid id, [FromBody] UpdateEventOrganizerContactCommand command)
    {
        Logger.LogInformation("Updating organizer contact for event: {EventId}", id);

        // Ensure ID in route matches command
        if (id != command.EventId)
        {
            return BadRequest("Event ID mismatch");
        }

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.133: Batch link registered users to organizer contacts as co-organizers.
    /// </summary>
    [HttpPost("{id:guid}/organizer-contacts/link")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BatchLinkOrganizerContacts(Guid id, [FromBody] BatchLinkRequest request)
    {
        Logger.LogInformation("Batch linking co-organizers for event: {EventId}, LinkCount: {LinkCount}",
            id, request.Links?.Count ?? 0);

        var links = request.Links?.Select(l => new ContactUserLink(l.ContactId, l.UserId)).ToList()
            ?? new List<ContactUserLink>();

        var command = new BatchLinkOrganizerContactsCommand(id, links);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.133: Unlink a user from an organizer contact, removing co-organizer access.
    /// </summary>
    [HttpDelete("{id:guid}/organizer-contacts/{contactId:guid}/link")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnlinkOrganizerContactUser(Guid id, Guid contactId)
    {
        Logger.LogInformation("Unlinking co-organizer from event: {EventId}, ContactId: {ContactId}",
            id, contactId);

        var command = new UnlinkOrganizerContactUserCommand(id, contactId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Issue #51: Update max attendees per registration for an event
    /// Allows event organizer to configure how many attendees can be added in a single registration
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="request">New max attendees value (1-50, cannot exceed event capacity)</param>
    [HttpPut("{id:guid}/max-attendees-per-registration")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMaxAttendeesPerRegistration(Guid id, [FromBody] UpdateMaxAttendeesPerRegistrationRequest request)
    {
        Logger.LogInformation("[Issue #51] Updating max attendees per registration for event: {EventId}, NewMax: {NewMax}",
            id, request.MaxAttendeesPerRegistration);

        var command = new UpdateMaxAttendeesPerRegistrationCommand(id, request.MaxAttendeesPerRegistration);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete an event (Owner only, draft/cancelled events only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} deleting event: {EventId}", userId, id);

        var command = new DeleteEventCommand(id, userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Submit event for approval (Owner only)
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitForApproval(Guid id)
    {
        Logger.LogInformation("Submitting event for approval: {EventId}", id);

        var command = new SubmitEventForApprovalCommand(id);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== STATUS CHANGE ENDPOINTS ====================

    /// <summary>
    /// Publish an event (Owner only)
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PublishEvent(Guid id)
    {
        Logger.LogInformation("Publishing event: {EventId}", id);

        var command = new PublishEventCommand(id);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.41: Unpublish an event (return to Draft status) (Owner only)
    /// Allows organizers to make corrections after premature publication.
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnpublishEvent(Guid id)
    {
        Logger.LogInformation("Unpublishing event: {EventId}", id);

        var command = new UnpublishEventCommand(id);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Cancel an event with reason (Owner only)
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelEvent(Guid id, [FromBody] CancelEventRequest request)
    {
        Logger.LogInformation("Cancelling event: {EventId}", id);

        var command = new CancelEventCommand(id, request.Reason);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 7F-B: Convert all active registrations on an event from one
    /// <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode"/> to another.
    /// Owner only. Pass <c>dryRun=true</c> to compute the conversion report without
    /// applying — drives the UI's diff-preview confirmation dialog.
    /// </summary>
    [HttpPost("{id:guid}/convert-registration-mode")]
    [Authorize]
    [ProducesResponseType(typeof(LankaConnect.Products.LankaEvents.Application.Commands.ConvertRegistrationMode.ConvertRegistrationModeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConvertRegistrationMode(
        Guid id, [FromBody] ConvertRegistrationModeRequest request)
    {
        Logger.LogInformation(
            "[7F-B] ConvertRegistrationMode endpoint hit — EventId={EventId} TargetMode={TargetMode} DryRun={DryRun}",
            id, request.TargetMode, request.DryRun);

        var command = new LankaConnect.Products.LankaEvents.Application.Commands.ConvertRegistrationMode.ConvertRegistrationModeCommand(
            EventId: id,
            TargetMode: request.TargetMode,
            DryRun: request.DryRun,
            NotifyAttendees: request.NotifyAttendees);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Postpone an event with reason (Owner only)
    /// </summary>
    [HttpPost("{id:guid}/postpone")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PostponeEvent(Guid id, [FromBody] PostponeEventRequest request)
    {
        Logger.LogInformation("Postponing event: {EventId}", id);

        var command = new PostponeEventCommand(id, request.Reason);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== RSVP ENDPOINTS ====================

    /// <summary>
    /// RSVP to an event (Authenticated users)
    /// Phase 6A.11: Updated to support multi-attendee registrations with detailed attendee information
    /// - Legacy format: { userId, quantity } - simple quantity-based RSVP
    /// - New format: { userId, attendees: [{name, age}, ...], email, phoneNumber, address, successUrl, cancelUrl } - multi-attendee with details
    /// </summary>
    [HttpPost("{id:guid}/rsvp")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RsvpToEvent(Guid id, [FromBody] RsvpRequest request)
    {
        // Phase 6A.11: Determine format and log appropriately
        if (request.Attendees?.Any() == true)
        {
            Logger.LogInformation("User {UserId} RSVPing to event {EventId} with {AttendeeCount} multi-attendee registrations (new format)",
                request.UserId, id, request.Attendees.Count);
        }
        else
        {
            Logger.LogInformation("User {UserId} RSVPing to event {EventId} with quantity {Quantity} (legacy format)",
                request.UserId, id, request.Quantity);
        }

        // Phase 6A.11: Map all DTO fields to command (including multi-attendee fields)
        // Donation Feature: Include donation fields for combined checkout
        var command = new RsvpToEventCommand(
            id,
            request.UserId,
            request.Quantity,
            request.Attendees,
            request.Email,
            request.PhoneNumber,
            request.Address,
            request.SuccessUrl,
            request.CancelUrl,
            // Donation Feature: Pass donation fields (C3 Guard: handler checks > 0)
            DonationAmount: request.DonationAmount,
            DonorName: request.DonorName,
            DonorPhone: request.DonorPhone,
            DonorNotes: request.DonorNotes,
            // Phase 6A.137F: Pass bundled add-on, collection, and sponsor fields
            AddOnSelections: request.AddOnSelections,
            CollectionAmount: request.CollectionAmount,
            CollectionNotes: request.CollectionNotes,
            SponsorAmount: request.SponsorAmount,
            SponsorOrganization: request.SponsorOrganization,
            SponsorNotes: request.SponsorNotes,
            // Phase 6A.151 C5: pre-staged sponsor image from POST /sponsors/staging-image
            SponsorStagingBlobName: request.SponsorStagingBlobName,
            SponsorStagingBlobUrl: request.SponsorStagingBlobUrl,
            // Phase 6A.148.W5.D10.c: optional sponsor-contact override fields
            SponsorName: request.SponsorName,
            SponsorEmail: request.SponsorEmail,
            SponsorPhone: request.SponsorPhone,
            // Phase 7A.6D: Pass WhatsApp phone for opt-in
            WhatsAppPhoneNumber: request.WhatsAppPhoneNumber,
            // Phase 7E.3a: Pass head-count payload for B-mode events
            LeadAttendeeName: request.LeadAttendeeName,
            HeadCount: request.HeadCount,
            // Phase 8 S8.2.B: Pass assigned-seating fields through to handler.
            SeatIds: request.SeatIds,
            SeatSessionId: request.SeatSessionId
        );
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.24: Resend ticket email to the registered user
    /// Only the registration owner can resend their ticket
    /// </summary>
    [HttpPost("registrations/{registrationId:guid}/resend-ticket")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResendTicket(Guid registrationId)
    {
        // Get current user ID from claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            Logger.LogWarning("Resend ticket attempted without valid user ID claim");
            return Unauthorized();
        }

        Logger.LogInformation("User {UserId} requesting to resend ticket for Registration {RegistrationId}",
            userId, registrationId);

        var command = new ResendTicketEmailCommand(registrationId, userId);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return Ok(new { message = "Ticket email resent successfully" });
        }

        // Check if it's an authorization error
        if (result.Errors.Any(e => e.Contains("Not authorized")))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Errors.First() });
        }

        return HandleResult(result);
    }

    // ============================================================
    // Phase 6A.141: Paid-Event Ticket Check-in / QR Scanner endpoints
    //
    // Two endpoints, one ScanTicketCommand backing both:
    //   POST .../tickets/scan          — QR-scan path (body: { qrPayload })
    //   POST .../tickets/scan-by-code  — Manual-entry fallback (body: { ticketCode })
    // Authorization is enforced inside the handler via Event.IsOrganizer(scannerUserId) —
    // see Phase 6A.133 organizer-link pattern. The handler returns Result.Forbidden which
    // BaseController.BuildProblem maps to HTTP 403.
    //
    // F3: client_ip extracted via BaseController.GetClientIpAddress (X-Forwarded-For aware).
    // ============================================================

    /// <summary>
    /// Scan a QR-encoded ticket payload at the event gate. Returns accepted with attendee
    /// + tier details, or rejected with a reason code (already_scanned, invalid_signature,
    /// expired, invalidated, ticket_not_found, wrong_event, malformed_payload).
    /// Both accepted and rejected business outcomes are HTTP 200 with the outcome on the
    /// body — HTTP 4xx is reserved for protocol/auth failures (per Plan-agent D5).
    /// </summary>
    [HttpPost("{eventId:guid}/tickets/scan")]
    [Authorize]
    [ProducesResponseType(typeof(ScanTicketResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScanTicket(
        Guid eventId,
        [FromBody] ScanTicketQrRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var scannerName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        // Phase 6A.116-style cache-prevention: scan endpoints must never be cached.
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        Logger.LogInformation(
            "ScanTicket (QR) endpoint: EventId={EventId}, ScannerUserId={ScannerUserId}, PayloadLength={Length}",
            eventId, userId, request.QrPayload?.Length ?? 0);

        var command = new LankaConnect.Products.LankaEvents.Application.Commands.ScanTicket.ScanTicketCommand(
            EventId: eventId,
            ScannerUserId: userId,
            ScannerName: scannerName,
            QrPayload: request.QrPayload,
            TicketCode: null,
            ClientIp: GetClientIpAddress(),
            UserAgent: Request.Headers.UserAgent.ToString());

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Manual-entry fallback for the gate scanner — staff types in the LC-YYYY-XXXXXX
    /// ticket code when the QR can't be scanned (e.g. damaged print, dead phone). No
    /// signature verification (trust comes from organizer auth).
    /// </summary>
    [HttpPost("{eventId:guid}/tickets/scan-by-code")]
    [Authorize]
    [ProducesResponseType(typeof(ScanTicketResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScanTicketByCode(
        Guid eventId,
        [FromBody] ScanTicketByCodeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var scannerName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        Logger.LogInformation(
            "ScanTicket (manual) endpoint: EventId={EventId}, ScannerUserId={ScannerUserId}, TicketCode={TicketCode}",
            eventId, userId, request.TicketCode);

        var command = new LankaConnect.Products.LankaEvents.Application.Commands.ScanTicket.ScanTicketCommand(
            EventId: eventId,
            ScannerUserId: userId,
            ScannerName: scannerName,
            QrPayload: null,
            TicketCode: request.TicketCode,
            ClientIp: GetClientIpAddress(),
            UserAgent: Request.Headers.UserAgent.ToString());

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.141 admin override — reverses a prior accepted scan. AdminOnly policy
    /// (event organizers do not have unmark privilege by default to limit abuse during
    /// disputes). Writes a new TicketScanLog row with scan_result='unmarked' carrying
    /// the admin's stated reason; the original accepted-scan row stays for forensic
    /// completeness.
    /// </summary>
    [HttpPost("{eventId:guid}/tickets/{ticketCode}/unmark-scanned")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(UnmarkScannedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnmarkScanned(
        Guid eventId,
        string ticketCode,
        [FromBody] UnmarkScannedRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var adminName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        Logger.LogInformation(
            "UnmarkScanned endpoint: EventId={EventId}, TicketCode={TicketCode}, AdminUserId={AdminUserId}",
            eventId, ticketCode, userId);

        var command = new UnmarkScannedCommand(
            EventId: eventId,
            TicketCode: ticketCode,
            AdminUserId: userId,
            AdminName: adminName,
            Reason: request.Reason,
            ClientIp: GetClientIpAddress(),
            UserAgent: Request.Headers.UserAgent.ToString());

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Register anonymous attendee for an event (No authentication required)
    /// Phase 6A.44: Returns checkout URL for paid events, null for free events
    /// </summary>
    [HttpPost("{id:guid}/register-anonymous")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AnonymousRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAnonymousAttendee(Guid id, [FromBody] AnonymousRegistrationRequest request)
    {
        Logger.LogInformation("Anonymous attendee {Email} registering for event {EventId}",
            request.Email, id);

        // Phase 6A.43: Support both legacy and multi-attendee formats
        // Convert AnonymousAttendeeDto to Application layer AttendeeDto if provided
        List<LankaConnect.Products.LankaEvents.Application.Commands.RegisterAnonymousAttendee.AttendeeDto>? attendees = null;
        if (request.Attendees != null && request.Attendees.Any())
        {
            attendees = request.Attendees.Select(a =>
                new LankaConnect.Products.LankaEvents.Application.Commands.RegisterAnonymousAttendee.AttendeeDto(
                    a.Name,
                    a.AgeCategory,
                    a.Gender,
                    a.TicketTierId  // Phase 8 S8.2.D: propagate optional tier id to handler
                )).ToList();
        }

        var command = new RegisterAnonymousAttendeeCommand(
            EventId: id,
            Name: request.Name,
            Age: request.Age,
            Attendees: attendees, // Phase 6A.43: Pass attendees array from request
            Email: request.Email,
            PhoneNumber: request.PhoneNumber,
            Address: request.Address,
            Quantity: request.Quantity,
            SuccessUrl: request.SuccessUrl, // Phase 6A.44: Stripe checkout URLs
            CancelUrl: request.CancelUrl,
            // Donation Feature: Pass donation fields for combined checkout
            DonationAmount: request.DonationAmount,
            DonorName: request.DonorName,
            DonorPhone: request.DonorPhone,
            DonorNotes: request.DonorNotes,
            // Phase 6A.137F: Pass bundled add-on, collection, and sponsor fields
            AddOnSelections: request.AddOnSelections?.Select(a =>
                new LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent.AddOnSelectionDto(a.DefinitionId, a.Quantity)).ToList(),
            CollectionAmount: request.CollectionAmount,
            CollectionNotes: request.CollectionNotes,
            SponsorAmount: request.SponsorAmount,
            SponsorOrganization: request.SponsorOrganization,
            SponsorNotes: request.SponsorNotes,
            // Phase 6A.151 C5: pre-staged sponsor image from POST /sponsors/staging-image
            SponsorStagingBlobName: request.SponsorStagingBlobName,
            SponsorStagingBlobUrl: request.SponsorStagingBlobUrl,
            // Phase 6A.148.W5.D10.c: optional sponsor-contact override fields
            SponsorName: request.SponsorName,
            SponsorEmail: request.SponsorEmail,
            SponsorPhone: request.SponsorPhone,
            // Phase 7A.6D: Pass WhatsApp phone for opt-in
            WhatsAppPhoneNumber: request.WhatsAppPhoneNumber,
            // Phase 7E.3a: Pass head-count payload for B-mode anonymous registrations
            LeadAttendeeName: request.LeadAttendeeName,
            HeadCount: request.HeadCount,
            // Phase 8 S8.2.B: Pass assigned-seating fields through to handler
            SeatIds: request.SeatIds,
            SeatSessionId: request.SeatSessionId
        );

        var result = await Mediator.Send(command);

        // Phase 6A.44: Return structured response with checkout URL for paid events
        if (result.IsSuccess)
        {
            var checkoutUrl = result.Value;
            return Ok(new AnonymousRegistrationResponse(
                Success: true,
                CheckoutUrl: checkoutUrl,
                Message: checkoutUrl != null
                    ? "Please complete payment to confirm your registration."
                    : "Registration successful! You will receive a confirmation email shortly."
            ));
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Cancel RSVP to an event (Authenticated users)
    /// Phase 6A.28: Added deleteSignUpCommitments parameter for user choice
    /// </summary>
    /// <param name="id">The event ID</param>
    /// <param name="deleteSignUpCommitments">
    /// If true, deletes all sign-up commitments and restores remaining quantities.
    /// If false (default), keeps sign-up commitments intact.
    /// </param>
    /// <param name="deleteFormResponses">
    /// If true, deletes all form submissions for this event. If false (default), keeps them.
    /// </param>
    /// <param name="refundAddOnPurchases">
    /// If true, refunds all completed add-on purchases via Stripe. If false (default), keeps them.
    /// </param>
    /// <param name="refundCollections">
    /// Phase 6A.137F: If true, refunds collection contribution via Stripe. If false (default), keeps it.
    /// </param>
    /// <param name="refundSponsors">
    /// Phase 6A.137F: If true, refunds money sponsorship via Stripe. If false (default), keeps it.
    /// </param>
    [HttpDelete("{id:guid}/rsvp")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelRsvp(
        Guid id,
        [FromQuery] bool deleteSignUpCommitments = false,
        [FromQuery] bool deleteFormResponses = false,
        [FromQuery] bool refundAddOnPurchases = false,
        // Phase 6A.137F: Collection and sponsor refund query parameters
        [FromQuery] bool refundCollections = false,
        [FromQuery] bool refundSponsors = false)
    {
        var userId = User.GetUserId();
        Logger.LogInformation(
            "User {UserId} cancelling RSVP to event {EventId}, DeleteSignUpCommitments={DeleteSignUpCommitments}, DeleteFormResponses={DeleteFormResponses}, RefundAddOnPurchases={RefundAddOnPurchases}, RefundCollections={RefundCollections}, RefundSponsors={RefundSponsors}",
            userId, id, deleteSignUpCommitments, deleteFormResponses, refundAddOnPurchases, refundCollections, refundSponsors);

        var command = new CancelRsvpCommand(id, userId, deleteSignUpCommitments, deleteFormResponses, refundAddOnPurchases, refundCollections, refundSponsors);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Withdraw a pending refund request and restore registration to Confirmed status.
    /// Phase 6A.91: Allows users to cancel their refund request before it completes.
    /// </summary>
    /// <param name="id">The event ID</param>
    /// <returns>Success if refund request was withdrawn</returns>
    [HttpPost("{id:guid}/rsvp/withdraw-refund")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawRefundRequest(Guid id)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("[Phase 6A.91] User {UserId} withdrawing refund request for event {EventId}",
            userId, id);

        var command = new WithdrawRefundRequestCommand(id, userId);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            Logger.LogInformation("[Phase 6A.91] Refund request withdrawn successfully - EventId: {EventId}, UserId: {UserId}",
                id, userId);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 7E follow-up: Organiser-initiated force-cancellation of a registration that
    /// is stuck in <c>RefundRequested</c> status because the Stripe webhook never confirmed
    /// the refund. Authorization mirrors <see cref="ExportEventAttendees"/>: caller must be
    /// the event organiser (owner or co-organizer).
    ///
    /// Effect: the row's status is moved <c>RefundRequested → Cancelled</c>. We don't move
    /// it to <c>Refunded</c> because no refund is being issued by us — this is a clean-up
    /// for off-platform / abandoned refund flows.
    /// </summary>
    [HttpPost("{eventId:guid}/registrations/{registrationId:guid}/force-cancel-stuck-refund")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForceCancelStuckRefund(Guid eventId, Guid registrationId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation(
            "User {UserId} requesting force-cancel of stuck refund - EventId={EventId}, RegistrationId={RegistrationId}",
            userId, eventId, registrationId);

        // Authorization: load event via the GetEventByIdQuery so we get the populated
        // IsCurrentUserOrganizer flag (mirrors ExportEventAttendees pattern, Phase 6A.133).
        var eventQuery = new GetEventByIdQuery(eventId);
        var eventResult = await Mediator.Send(eventQuery);
        if (eventResult.IsFailure)
        {
            if (eventResult.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound();
            }
            return HandleResult(eventResult);
        }

        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning(
                "User {UserId} attempted to force-cancel a registration without organizer privileges - EventId={EventId}, RegistrationId={RegistrationId}",
                userId, eventId, registrationId);
            return Forbid();
        }

        var command = new ForceCancelStuckRefundCommand(eventId, registrationId);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            Logger.LogInformation(
                "Force-cancel succeeded - Organizer={UserId}, EventId={EventId}, RegistrationId={RegistrationId}",
                userId, eventId, registrationId);
        }

        return HandleResult(result);
    }

    // =====================================================================================
    // Phase 6A.148 — Refund Approval Workflow endpoints
    //
    // Feature-flagged via Refund:ApprovalWorkflow:Enabled (false by default; true in
    // staging). When disabled all 7 endpoints below return 404. Legacy refund routes
    // (/rsvp/withdraw-refund, /rsvp/cancel paid-refund branch, force-cancel-stuck-refund)
    // remain available regardless of the flag for in-flight rows + audit cleanup.
    //
    // Authorization model:
    //   - Attendee endpoints (POST /refund-requests, GET /me, POST /me/withdraw): require
    //     authenticated user; handler verifies registration ownership.
    //   - Organizer endpoints (GET, organizer-initiated POST, approve, reject): handler
    //     verifies Event.IsOrganizer(callerUserId).
    // =====================================================================================

    private bool RefundApprovalWorkflowEnabled =>
        HttpContext.RequestServices
            .GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration))
            is Microsoft.Extensions.Configuration.IConfiguration cfg &&
        cfg.GetValue<bool>("Refund:ApprovalWorkflow:Enabled");

    /// <summary>
    /// Phase 6A.148: Attendee creates a refund request (Pending). An organizer must
    /// approve it before any Stripe call is made (the GATE — rule #6).
    /// </summary>
    [HttpPost("{eventId:guid}/refund-requests")]
    [Authorize]
    [ProducesResponseType(typeof(CreateRefundRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRefundRequest(
        Guid eventId, [FromBody] CreateRefundRequestPayload payload)
    {
        if (!RefundApprovalWorkflowEnabled) return NotFound();
        var userId = User.GetUserId();
        Logger.LogInformation(
            "[6A.148] POST /refund-requests: EventId={EventId} UserId={UserId} LineCount={LineCount}",
            eventId, userId, payload?.LineItems?.Count ?? 0);

        var command = new CreateRefundRequestCommand(
            eventId, userId, payload?.RequesterReason, payload?.LineItems ?? Array.Empty<RefundLineItemInputDto>());
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.148: Attendee fetches their own most-recent refund request for an event,
    /// or null. OrganizerNotes are intentionally excluded from this projection (architect F6).
    /// </summary>
    [HttpGet("{eventId:guid}/refund-requests/me")]
    [Authorize]
    [ProducesResponseType(typeof(AttendeeRefundRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyRefundRequest(Guid eventId)
    {
        if (!RefundApprovalWorkflowEnabled) return NotFound();
        var userId = User.GetUserId();
        var query = new GetMyRefundRequestQuery(eventId, userId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.148: Attendee withdraws their own Pending refund request.
    /// Distinct from legacy 6A.91 /rsvp/withdraw-refund (which operates on legacy
    /// RefundRequested registrations — kept available for in-flight Stripe rows).
    /// </summary>
    [HttpPost("{eventId:guid}/refund-requests/me/withdraw")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawMyRefundRequest(Guid eventId)
    {
        if (!RefundApprovalWorkflowEnabled) return NotFound();
        var userId = User.GetUserId();
        Logger.LogInformation(
            "[6A.148] POST /refund-requests/me/withdraw: EventId={EventId} UserId={UserId}",
            eventId, userId);

        var command = new WithdrawRefundRequestV2Command(eventId, userId);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.148: Organizer lists refund requests for an event, optionally filtered by status.
    /// </summary>
    [HttpGet("{eventId:guid}/refund-requests")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizerRefundRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListEventRefundRequests(
        Guid eventId, [FromQuery] LankaConnect.Products.LankaEvents.Domain.Enums.RefundRequestStatus? status = null)
    {
        if (!RefundApprovalWorkflowEnabled) return NotFound();
        var userId = User.GetUserId();
        var query = new GetEventRefundRequestsQuery(eventId, userId, status);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.148: Organizer initiates a refund on behalf of an attendee. Skips
    /// Pending — request is created directly in Approved and Stripe dispatch is queued.
    /// </summary>
    [HttpPost("{eventId:guid}/refund-requests/organizer-initiated")]
    [Authorize]
    [ProducesResponseType(typeof(CreateRefundRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateOrganizerInitiatedRefund(
        Guid eventId, [FromBody] CreateOrganizerInitiatedRefundPayload payload)
    {
        if (!RefundApprovalWorkflowEnabled) return NotFound();
        var userId = User.GetUserId();
        Logger.LogInformation(
            "[6A.148] POST /refund-requests/organizer-initiated: EventId={EventId} RegId={RegId} CallerUserId={UserId} Override={Override}",
            eventId, payload?.RegistrationId, userId, payload?.OverrideScanGuard);

        if (payload is null || payload.RegistrationId == Guid.Empty)
            return BadRequest(new ProblemDetails { Title = "RegistrationId is required" });

        var command = new CreateOrganizerInitiatedRefundCommand(
            eventId, payload.RegistrationId, userId,
            payload.OrganizerNotes, payload.OverrideScanGuard,
            payload.LineItems ?? Array.Empty<RefundLineItemInputDto>());
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.148: Organizer approves a pending refund request with per-line approved
    /// amounts. All-zero approvals are rejected (use Reject instead). Concurrency conflicts
    /// surface as 409 — refresh the queue and try again.
    /// </summary>
    [HttpPost("{eventId:guid}/refund-requests/{refundRequestId:guid}/approve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveRefundRequest(
        Guid eventId, Guid refundRequestId, [FromBody] ApproveRefundRequestPayload payload)
    {
        if (!RefundApprovalWorkflowEnabled) return NotFound();
        var userId = User.GetUserId();
        Logger.LogInformation(
            "[6A.148] POST /refund-requests/{RrId}/approve: EventId={EventId} CallerUserId={UserId} Lines={LineCount}",
            refundRequestId, eventId, userId, payload?.PerLineApprovedAmounts?.Count ?? 0);

        var command = new ApproveRefundRequestCommand(
            eventId, refundRequestId, userId,
            payload?.OrganizerNotes,
            payload?.PerLineApprovedAmounts ?? Array.Empty<ApproveLineItemInputDto>());
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.148: Organizer declines a pending refund request. Reason is mandatory
    /// and is sent to the attendee in the rejection email.
    /// </summary>
    [HttpPost("{eventId:guid}/refund-requests/{refundRequestId:guid}/reject")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectRefundRequest(
        Guid eventId, Guid refundRequestId, [FromBody] RejectRefundRequestPayload payload)
    {
        if (!RefundApprovalWorkflowEnabled) return NotFound();
        var userId = User.GetUserId();
        Logger.LogInformation(
            "[6A.148] POST /refund-requests/{RrId}/reject: EventId={EventId} CallerUserId={UserId}",
            refundRequestId, eventId, userId);

        if (payload is null || string.IsNullOrWhiteSpace(payload.RejectionReason))
            return BadRequest(new ProblemDetails { Title = "RejectionReason is required" });

        var command = new RejectRefundRequestCommand(
            eventId, refundRequestId, userId, payload.RejectionReason);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    // ============== Phase 6A.148 request payload records ==============
    public record CreateRefundRequestPayload(
        string? RequesterReason,
        IReadOnlyList<RefundLineItemInputDto>? LineItems);

    public record CreateOrganizerInitiatedRefundPayload(
        Guid RegistrationId,
        string? OrganizerNotes,
        bool OverrideScanGuard,
        IReadOnlyList<RefundLineItemInputDto>? LineItems);

    public record ApproveRefundRequestPayload(
        string? OrganizerNotes,
        IReadOnlyList<ApproveLineItemInputDto>? PerLineApprovedAmounts);

    public record RejectRefundRequestPayload(string RejectionReason);

    /// <summary>
    /// Update RSVP quantity (Authenticated users)
    /// </summary>
    [HttpPut("{id:guid}/rsvp")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateRsvp(Guid id, [FromBody] UpdateRsvpRequest request)
    {
        Logger.LogInformation("User {UserId} updating RSVP quantity for event {EventId} to {Quantity}",
            request.UserId, id, request.NewQuantity);

        var command = new UpdateRsvpCommand(id, request.UserId, request.NewQuantity);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== TICKET ENDPOINTS (Phase 6A.24) ====================

    /// <summary>
    /// Get ticket details for a user's registration
    /// Phase 6A.24: Ticket viewing for paid events
    /// </summary>
    [HttpGet("{eventId:guid}/my-registration/ticket")]
    [Authorize]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTicket(Guid eventId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Getting ticket for user {UserId} for event {EventId}", userId, eventId);

        // First get the registration to get the registration ID
        var registrationQuery = new GetUserRegistrationForEventQuery(eventId, userId);
        var registrationResult = await Mediator.Send(registrationQuery);

        if (registrationResult.IsFailure || registrationResult.Value == null)
        {
            Logger.LogInformation("No registration found for user {UserId} for event {EventId}", userId, eventId);
            return NotFound(new { message = "You are not registered for this event" });
        }

        var query = new GetTicketQuery(eventId, registrationResult.Value.Id, userId);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound(new { message = result.Error });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Download ticket as PDF
    /// Phase 6A.24: Ticket PDF download for paid events
    /// </summary>
    [HttpGet("{eventId:guid}/my-registration/ticket/pdf")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadMyTicketPdf(Guid eventId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Downloading ticket PDF for user {UserId} for event {EventId}", userId, eventId);

        // First get the registration to get the registration ID
        var registrationQuery = new GetUserRegistrationForEventQuery(eventId, userId);
        var registrationResult = await Mediator.Send(registrationQuery);

        if (registrationResult.IsFailure || registrationResult.Value == null)
        {
            Logger.LogInformation("No registration found for user {UserId} for event {EventId}", userId, eventId);
            return NotFound(new { message = "You are not registered for this event" });
        }

        var query = new GetTicketPdfQuery(eventId, registrationResult.Value.Id, userId);
        var result = await Mediator.Send(query);

        if (result.IsFailure)
        {
            if (result.Errors.FirstOrDefault()?.Contains("not found") == true)
            {
                return NotFound(new { message = result.Error });
            }
            return HandleResult(result);
        }

        return File(result.Value.PdfBytes, "application/pdf", result.Value.FileName);
    }

    /// <summary>
    /// Resend ticket email to registration contact
    /// Phase 6A.24: Allows users to request ticket email resend
    /// </summary>
    [HttpPost("{eventId:guid}/my-registration/ticket/resend-email")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResendTicketEmail(Guid eventId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Resending ticket email for user {UserId} for event {EventId}", userId, eventId);

        // First get the registration to get the registration ID
        var registrationQuery = new GetUserRegistrationForEventQuery(eventId, userId);
        var registrationResult = await Mediator.Send(registrationQuery);

        if (registrationResult.IsFailure || registrationResult.Value == null)
        {
            Logger.LogInformation("No registration found for user {UserId} for event {EventId}", userId, eventId);
            return NotFound(new { message = "You are not registered for this event" });
        }

        var command = new ResendTicketEmailCommand(registrationResult.Value.Id, userId);
        var result = await Mediator.Send(command);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound(new { message = result.Error });
        }

        return HandleResult(result);
    }

    // ==================== USER DASHBOARD ENDPOINTS ====================

    /// <summary>
    /// Get events created by current user (Authenticated Event Organizers/Admins)
    /// Epic 1: Dashboard my-events endpoint
    /// Phase 6A.47: Added filters (searchTerm, category, date range, location) for Event Management tab
    /// Issue #36: Added statusFilter for user-friendly status filtering
    /// </summary>
    [HttpGet("my-events")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyEvents(
        [FromQuery] string? searchTerm = null,
        [FromQuery] EventCategory? category = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] string? state = null,
        [FromQuery] List<Guid>? metroAreaIds = null,
        [FromQuery] EventStatusFilter? statusFilter = null)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Getting events created by user: {UserId} with filters: searchTerm={SearchTerm}, category={Category}, state={State}, statusFilter={StatusFilter}",
            userId, searchTerm, category, state, statusFilter);

        var query = new GetEventsByOrganizerQuery(
            userId,
            searchTerm,
            category,
            startDateFrom,
            startDateTo,
            state,
            metroAreaIds,
            statusFilter);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get events user has registered for (Authenticated users)
    /// Epic 1: Returns full EventDto instead of RsvpDto for better dashboard UX
    /// Phase 6A.47: Added filters (searchTerm, category, date range, location) for My Registered Events tab
    /// </summary>
    [HttpGet("my-rsvps")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRsvps(
        [FromQuery] string? searchTerm = null,
        [FromQuery] EventCategory? category = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] string? state = null,
        [FromQuery] List<Guid>? metroAreaIds = null)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Getting registered events for user: {UserId} with filters: searchTerm={SearchTerm}, category={Category}, state={State}",
            userId, searchTerm, category, state);

        var query = new GetMyRegisteredEventsQuery(
            userId,
            searchTerm,
            category,
            startDateFrom,
            startDateTo,
            state,
            metroAreaIds);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get user's registration details for a specific event
    /// Returns full registration with attendee names and ages
    /// Fix 1: Registration Status Detection Enhancement
    /// </summary>
    [HttpGet("{eventId}/my-registration")]
    [Authorize]
    [ProducesResponseType(typeof(RegistrationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRegistrationForEvent(Guid eventId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Getting registration details for user {UserId} for event {EventId}", userId, eventId);

        var query = new GetUserRegistrationForEventQuery(eventId, userId);
        var result = await Mediator.Send(query);

        if (result == null)
        {
            Logger.LogInformation("No registration found for user {UserId} for event {EventId}", userId, eventId);
            return NotFound(new { message = "You are not registered for this event" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Phase 6A.44: Gets registration details by registration ID (for anonymous users after payment)
    /// </summary>
    [HttpGet("registrations/{registrationId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegistrationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRegistrationById(Guid registrationId)
    {
        Logger.LogInformation("Getting registration details for registration {RegistrationId}", registrationId);

        var query = new GetRegistrationByIdQuery(registrationId);
        var result = await Mediator.Send(query);

        if (result.IsFailure || result.Value == null)
        {
            Logger.LogInformation("Registration {RegistrationId} not found", registrationId);
            return NotFound();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Phase 6A.14: Update user's registration details (attendees and contact information)
    /// Allows users to edit their registration after initial RSVP
    /// Business Rules:
    /// - User must have an active registration
    /// - Cannot change attendee count on paid registrations (only names/ages)
    /// - Maximum 10 attendees per registration
    /// - Cannot update cancelled or refunded registrations
    /// </summary>
    [HttpPut("{eventId}/my-registration")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyRegistration(Guid eventId, [FromBody] UpdateRegistrationRequest request)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} updating registration for event {EventId} with {AttendeeCount} attendees",
            userId, eventId, request.Attendees?.Count ?? 0);

        var command = new UpdateRegistrationDetailsCommand(
            eventId,
            userId,
            request.Attendees?.Select(a => new LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent.AttendeeDto(a.Name, a.AgeCategory, a.Gender)).ToList()
                ?? new List<LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent.AttendeeDto>(),
            request.Email,
            request.PhoneNumber,
            request.Address);

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            Logger.LogWarning("Failed to update registration for user {UserId} for event {EventId}: {Errors}",
                userId, eventId, string.Join(", ", result.Errors));
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Get upcoming events for user (Authenticated users)
    /// </summary>
    [HttpGet("upcoming")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUpcomingEvents()
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Getting upcoming events for user: {UserId}", userId);

        var query = new GetUpcomingEventsForUserQuery(userId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    // ==================== ADMIN ENDPOINTS ====================

    /// <summary>
    /// Get events pending approval (Admins only)
    /// </summary>
    [HttpGet("admin/pending")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingEvents()
    {
        Logger.LogInformation("Getting events pending approval");

        var query = new GetPendingEventsForApprovalQuery();
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Approve event (Admins only)
    /// </summary>
    [HttpPost("admin/{id:guid}/approve")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveEvent(Guid id, [FromBody] ApproveEventRequest request)
    {
        Logger.LogInformation("Approving event {EventId} by admin {AdminId}", id, request.ApprovedByAdminId);

        var command = new ApproveEventCommand(id, request.ApprovedByAdminId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Reject event with reason (Admins only)
    /// </summary>
    [HttpPost("admin/{id:guid}/reject")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectEvent(Guid id, [FromBody] RejectEventRequest request)
    {
        Logger.LogInformation("Rejecting event {EventId} by admin {AdminId}", id, request.RejectedByAdminId);

        var command = new RejectEventCommand(id, request.RejectedByAdminId, request.Reason);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #region Event Images (Epic 2 Phase 2)

    /// <summary>
    /// Add image to event gallery
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventImage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddImageToEvent(Guid id, IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest("Image file is required");

        // Read image data
        using var memoryStream = new MemoryStream();
        await image.CopyToAsync(memoryStream);
        var imageData = memoryStream.ToArray();

        Logger.LogInformation("Adding image to event {EventId}, FileName={FileName}, Size={Size}",
            id, image.FileName, imageData.Length);

        var command = new AddImageToEventCommand
        {
            EventId = id,
            ImageData = imageData,
            FileName = image.FileName
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Replace an existing event image with a new one
    /// </summary>
    [HttpPut("{eventId:guid}/images/{imageId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceEventImage(Guid eventId, Guid imageId, IFormFile image)
    {
        Logger.LogInformation("Replacing image {ImageId} in event {EventId}", imageId, eventId);

        if (image == null || image.Length == 0)
        {
            return BadRequest("Image file is required");
        }

        using var memoryStream = new MemoryStream();
        await image.CopyToAsync(memoryStream);

        var command = new ReplaceEventImageCommand
        {
            EventId = eventId,
            ImageId = imageId,
            ImageData = memoryStream.ToArray(),
            FileName = image.FileName
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound(result.Error);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Delete image from event gallery
    /// </summary>
    [HttpDelete("{eventId:guid}/images/{imageId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteEventImage(Guid eventId, Guid imageId)
    {
        Logger.LogInformation("Deleting image {ImageId} from event {EventId}", imageId, eventId);

        var command = new DeleteEventImageCommand
        {
            EventId = eventId,
            ImageId = imageId
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Reorder event images
    /// </summary>
    [HttpPut("{id:guid}/images/reorder")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReorderEventImages(Guid id, [FromBody] EventReorderImagesRequest request)
    {
        Logger.LogInformation("Reordering images for event {EventId}", id);

        var command = new ReorderEventImagesCommand
        {
            EventId = id,
            NewOrders = request.NewOrders
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Set an image as the primary/main thumbnail for the event
    /// Phase 6A.13: Primary image selection feature
    /// </summary>
    [HttpPost("{id:guid}/images/{imageId:guid}/set-primary")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId)
    {
        Logger.LogInformation("Setting image {ImageId} as primary for event {EventId}", imageId, id);

        var command = new SetPrimaryImageCommand
        {
            EventId = id,
            ImageId = imageId
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Event Videos (Epic 2 Phase 2)

    /// <summary>
    /// Add video to event gallery
    /// </summary>
    [HttpPost("{id:guid}/videos")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit for video uploads
    [ProducesResponseType(typeof(EventVideo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddVideoToEvent(Guid id, IFormFile video, IFormFile thumbnail)
    {
        if (video == null || video.Length == 0)
            return BadRequest("Video file is required");

        if (thumbnail == null || thumbnail.Length == 0)
            return BadRequest("Thumbnail image is required");

        // Read video data
        using var videoStream = new MemoryStream();
        await video.CopyToAsync(videoStream);
        var videoData = videoStream.ToArray();

        // Read thumbnail data
        using var thumbnailStream = new MemoryStream();
        await thumbnail.CopyToAsync(thumbnailStream);
        var thumbnailData = thumbnailStream.ToArray();

        Logger.LogInformation("Adding video to event {EventId}, VideoFile={VideoFile}, ThumbnailFile={ThumbnailFile}, VideoSize={VideoSize}, ThumbSize={ThumbSize}",
            id, video.FileName, thumbnail.FileName, videoData.Length, thumbnailData.Length);

        var command = new AddVideoToEventCommand
        {
            EventId = id,
            VideoData = videoData,
            VideoFileName = video.FileName,
            ThumbnailData = thumbnailData,
            ThumbnailFileName = thumbnail.FileName,
            // TODO: Extract video metadata (duration, format) from file
            Duration = null,
            Format = Path.GetExtension(video.FileName).TrimStart('.')
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete video from event gallery
    /// </summary>
    [HttpDelete("{eventId:guid}/videos/{videoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteEventVideo(Guid eventId, Guid videoId)
    {
        Logger.LogInformation("Deleting video {VideoId} from event {EventId}", videoId, eventId);

        var command = new DeleteEventVideoCommand
        {
            EventId = eventId,
            VideoId = videoId
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Waiting List Endpoints (Epic 2)

    /// <summary>
    /// Add user to event waiting list (Authenticated users)
    /// </summary>
    [HttpPost("{id:guid}/waiting-list")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddToWaitingList(Guid id)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Adding user {UserId} to waiting list for event {EventId}", userId, id);

        var command = new AddToWaitingListCommand(id, userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Remove user from event waiting list (Authenticated users)
    /// </summary>
    [HttpDelete("{id:guid}/waiting-list")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromWaitingList(Guid id)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Removing user {UserId} from waiting list for event {EventId}", userId, id);

        var command = new RemoveFromWaitingListCommand(id, userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Promote user from waiting list to confirmed registration (Authenticated users)
    /// </summary>
    [HttpPost("{id:guid}/waiting-list/promote")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PromoteFromWaitingList(Guid id)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Promoting user {UserId} from waiting list for event {EventId}", userId, id);

        var command = new PromoteFromWaitingListCommand(id, userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Get waiting list for an event
    /// </summary>
    [HttpGet("{id:guid}/waiting-list")]
    [ProducesResponseType(typeof(IReadOnlyList<WaitingListEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWaitingList(Guid id)
    {
        Logger.LogInformation("Getting waiting list for event {EventId}", id);

        var query = new GetWaitingListQuery(id);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        return HandleResult(result);
    }

    #endregion

    #region Calendar Export (Epic 2)

    /// <summary>
    /// Export event as ICS calendar file (for Google Calendar, Apple Calendar, Outlook)
    /// </summary>
    [HttpGet("{id:guid}/ics")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetEventIcs(Guid id)
    {
        Logger.LogInformation("Generating ICS file for event {EventId}", id);

        var query = new GetEventIcsQuery(id);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        // Phase 8YA.2: TBD events have no DTSTART/DTEND; the iCalendar spec has no
        // "Date TBD" representation. Return 422 Unprocessable Entity (architect-locked)
        // so callers (mobile apps, calendar UIs) know the event exists but can't be
        // exported until the organiser sets dates — distinct from 404 (not found) and
        // from a 400 BadRequest on a malformed request.
        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("Date TBD", StringComparison.OrdinalIgnoreCase) == true)
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Event has no confirmed dates",
                Detail = result.Errors.First(),
                Status = StatusCodes.Status422UnprocessableEntity,
            });
        }

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        // Return ICS file as downloadable content
        var icsContent = System.Text.Encoding.UTF8.GetBytes(result.Value);
        return File(icsContent, "text/calendar", $"event-{id}.ics");
    }

    #endregion

    #region Social Sharing Analytics (Epic 2)

    /// <summary>
    /// Record a social share of an event (for analytics tracking)
    /// </summary>
    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordEventShare(Guid id, [FromBody] RecordShareRequest? request = null)
    {
        Logger.LogInformation("Recording social share for event {EventId}", id);

        var userId = User.Identity?.IsAuthenticated == true ? User.TryGetUserId() : null;
        var command = new RecordEventShareCommand(id, userId, request?.Platform);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Event Pass Management

    // W5.2.a-fix (2026-06-28): GetEventPasses + AddPassToEvent + RemovePassFromEvent
    // endpoints removed. EventPass feature was superseded by TicketTier (multi-tier
    // ticketing). Per founder ruling on 2026-06-28, EventPass tables never existed in
    // staging DB, were never wired to a UI, and the feature is dead code from early
    // exploration. See docs/architecture/W52A_TABLE_DRIFT_INVESTIGATION.md.

    #endregion

    #region Ticket Tier Management (Phase 8)

    /// <summary>
    /// Get all ticket tiers for an event with availability info
    /// </summary>
    [HttpGet("{id:guid}/ticket-tiers")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<TicketTierDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTicketTiers(Guid id)
    {
        Logger.LogInformation("Getting ticket tiers for event {EventId}", id);

        var query = new GetTicketTiersQuery(id);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Set the ticketing mode for an event (SingleTier or Tiered)
    /// Must be set to Tiered before adding ticket tiers.
    /// </summary>
    [HttpPut("{id:guid}/ticketing-mode")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetTicketingMode(Guid id, [FromBody] SetTicketingModeRequest request)
    {
        Logger.LogInformation("Setting ticketing mode for event {EventId} to {Mode}", id, request.TicketingMode);

        var command = new SetTicketingModeCommand(id, request.TicketingMode);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Seating Redesign Slice 1: Set the seating mode for an event
    /// (GeneralAdmission or AssignedSeating). AssignedSeating requires the
    /// event to already be in TicketingMode.Tiered. Venue layout creation
    /// comes in Slice 2+3 — this endpoint only flips the enum.
    /// </summary>
    [HttpPut("{id:guid}/seating-mode")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetSeatingMode(Guid id, [FromBody] SetSeatingModeRequest request)
    {
        Logger.LogInformation("Setting seating mode for event {EventId} to {Mode}", id, request.SeatingMode);

        var command = new SetSeatingModeCommand(id, request.SeatingMode);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Add a new ticket tier to an event (Event Organizer/Admin only)
    /// Event must be in Tiered ticketing mode.
    /// </summary>
    [HttpPost("{id:guid}/ticket-tiers")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddTicketTier(Guid id, [FromBody] AddTicketTierRequest request)
    {
        Logger.LogInformation("Adding ticket tier '{TierName}' to event {EventId}", request.Name, id);

        var command = new AddTicketTierCommand(
            id,
            request.Name,
            request.Description,
            request.AdultPriceAmount,
            request.AdultPriceCurrency,
            request.ChildPriceAmount,
            request.ChildPriceCurrency,
            request.ChildAgeLimit,
            request.Capacity,
            request.MaxPerUser,
            request.SortOrder);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update an existing ticket tier (Event Organizer/Admin only)
    /// </summary>
    [HttpPut("{eventId:guid}/ticket-tiers/{tierId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateTicketTier(Guid eventId, Guid tierId, [FromBody] UpdateTicketTierRequest request)
    {
        Logger.LogInformation("Updating ticket tier {TierId} for event {EventId}", tierId, eventId);

        var command = new UpdateTicketTierCommand(
            eventId,
            tierId,
            request.Name,
            request.Description,
            request.AdultPriceAmount,
            request.AdultPriceCurrency,
            request.ChildPriceAmount,
            request.ChildPriceCurrency,
            request.ChildAgeLimit,
            request.Capacity,
            request.MaxPerUser,
            request.SortOrder);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Remove a ticket tier from an event (Event Organizer/Admin only)
    /// Cannot remove a tier that has reservations.
    /// </summary>
    [HttpDelete("{eventId:guid}/ticket-tiers/{tierId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveTicketTier(Guid eventId, Guid tierId)
    {
        Logger.LogInformation("Removing ticket tier {TierId} from event {EventId}", tierId, eventId);

        var command = new RemoveTicketTierCommand(eventId, tierId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Sign-Up Lists Management

    /// <summary>
    /// Get all sign-up lists for an event.
    /// Phase 6A.76: Added [AllowAnonymous] to allow non-members to view sign-up lists.
    /// Phase 7D.1: Optional ?kind= filter — pass <c>Items</c> or <c>Volunteers</c>
    /// to restrict results; omit for everything.
    /// </summary>
    [HttpGet("{id:guid}/signups")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<SignUpListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventSignUpLists(Guid id, [FromQuery] SignUpKind? kind = null)
    {
        Logger.LogInformation("Getting sign-up lists for event {EventId} (KindFilter={KindFilter})",
            id, kind?.ToString() ?? "All");

        var query = new GetEventSignUpListsQuery(id, kind);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Create a new sign-up list with items (Event Organizer/Admin only)
    /// Matches requirement: Create list WITH items in single API call
    /// </summary>
    [HttpPost("{id:guid}/signups")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSignUpList(Guid id, [FromBody] CreateSignUpListRequest request)
    {
        Logger.LogInformation("Creating sign-up list '{Category}' with {ItemCount} items for event {EventId}",
            request.Category, request.Items.Count, id);

        // Phase 6A.131: Map API DTOs to Application layer DTOs with dual-field support
        var items = request.Items.Select(item => new LankaConnect.Products.LankaEvents.Application.Commands.CreateSignUpListWithItems.SignUpItemDto(
            item.ItemDescription,
            item.ItemType,
            item.ItemCategory,
            item.TargetQuantity,
            item.AvailableSlots,
            item.SuggestedPerSlot,
            item.Notes)).ToList();

        var command = new CreateSignUpListWithItemsCommand(
            id,
            request.Category,
            request.Description,
            request.HasMandatoryItems,
            request.HasPreferredItems,
            request.HasSuggestedItems,
            items,
            request.HasOpenItems, // Phase 6A.28: Open Items support
            request.Kind);        // Phase 7D.1: Items (default) or Volunteers

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update sign-up list details (category, description, and category flags) (Event Organizer/Admin only)
    /// Phase 6A.13: Edit Sign-Up List feature
    /// </summary>
    [HttpPut("{eventId:guid}/signups/{signupId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSignUpList(Guid eventId, Guid signupId, [FromBody] UpdateSignUpListRequest request)
    {
        Logger.LogInformation("Updating sign-up list {SignUpId} for event {EventId} with category '{Category}'",
            signupId, eventId, request.Category);

        var command = new UpdateSignUpListCommand(
            eventId,
            signupId,
            request.Category,
            request.Description,
            request.HasMandatoryItems,
            request.HasPreferredItems,
            request.HasSuggestedItems,
            request.HasOpenItems); // Phase 6A.28: Open Items support

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Remove a sign-up list from an event (Event Organizer/Admin only)
    /// </summary>
    [HttpDelete("{eventId:guid}/signups/{signupId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSignUpListFromEvent(Guid eventId, Guid signupId)
    {
        Logger.LogInformation("Removing sign-up list {SignUpId} from event {EventId}", signupId, eventId);

        var command = new RemoveSignUpListFromEventCommand(eventId, signupId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #region Category-Based Sign-Up Item Management

    /// <summary>
    /// Add an item to a category-based sign-up list (Event Organizer/Admin only)
    /// </summary>
    [HttpPost("{eventId:guid}/signups/{signupId:guid}/items")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSignUpItem(Guid eventId, Guid signupId, [FromBody] AddSignUpItemRequest request)
    {
        Logger.LogInformation("Adding item '{ItemDescription}' to sign-up list {SignUpId} for event {EventId}",
            request.ItemDescription, signupId, eventId);

        var command = new AddSignUpItemCommand(
            eventId,
            signupId,
            request.ItemDescription,
            request.ItemType,
            request.ItemCategory,
            request.TargetQuantity,
            request.AvailableSlots,
            request.SuggestedPerSlot,
            request.Notes);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update an item in a category-based sign-up list (Event Organizer/Admin only)
    /// Phase 6A.14: Edit Sign-Up Item feature
    /// </summary>
    [HttpPut("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSignUpItem(Guid eventId, Guid signupId, Guid itemId, [FromBody] UpdateSignUpItemRequest request)
    {
        Logger.LogInformation("Updating item {ItemId} in sign-up list {SignUpId} for event {EventId}",
            itemId, signupId, eventId);

        var command = new UpdateSignUpItemCommand(
            eventId,
            signupId,
            itemId,
            request.ItemDescription,
            request.TargetQuantity,
            request.AvailableSlots,
            request.SuggestedPerSlot,
            request.Notes);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Remove an item from a category-based sign-up list (Event Organizer/Admin only)
    /// </summary>
    [HttpDelete("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSignUpItem(Guid eventId, Guid signupId, Guid itemId)
    {
        Logger.LogInformation("Removing item {ItemId} from sign-up list {SignUpId} for event {EventId}",
            itemId, signupId, eventId);

        var command = new RemoveSignUpItemCommand(eventId, signupId, itemId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.132: Reorder all items in a sign-up list. The client sends the full ordered ID
    /// list; the aggregate enforces exact-set equality (missing/extra/duplicate/unknown IDs all 400).
    /// Works for both Items and Volunteers Kind — the API is Kind-agnostic.
    /// </summary>
    [HttpPut("{eventId:guid}/signups/{signupId:guid}/items/reorder")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderSignUpItems(
        Guid eventId,
        Guid signupId,
        [FromBody] ReorderSignUpItemsRequest request)
    {
        Logger.LogInformation(
            "Reordering {Count} items in sign-up list {SignUpId} for event {EventId}",
            request?.OrderedItemIds?.Count ?? 0, signupId, eventId);

        var command = new ReorderSignUpItemsCommand(
            eventId,
            signupId,
            request?.OrderedItemIds ?? Array.Empty<Guid>());
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// User commits to bringing a specific item from a category-based sign-up list
    /// </summary>
    [HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitToSignUpItem(Guid eventId, Guid signupId, Guid itemId, [FromBody] CommitToSignUpItemRequest request)
    {
        // Phase 6A.116: Prevent Azure Container Apps ingress from caching POST responses
        // Root cause: Ingress layer was caching HTTP 200 responses, causing requests to never reach application
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        // Phase 6A.125: Log both quantity and slot fields for observability
        Logger.LogInformation(
            "CommitToSignUpItem: EventId={EventId}, ItemId={ItemId}, UserId={UserId}, Quantity={Quantity}, PhysicalQuantity={PhysicalQuantity}, SlotsClaimed={SlotsClaimed}",
            eventId, itemId, request.UserId, request.Quantity, request.PhysicalQuantity, request.SlotsClaimed);

        var command = new CommitToSignUpItemCommand(
            eventId,
            signupId,
            itemId,
            request.UserId,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone,
            PhysicalQuantity: request.PhysicalQuantity,
            SlotsClaimed: request.SlotsClaimed);

        // Phase 6A.114 DEBUG: Log before MediatR call
        Logger.LogWarning("[DEBUG-CONTROLLER-MEDIATOR] About to send CommitToSignUpItemCommand to MediatR");

        var result = await Mediator.Send(command);

        // Phase 6A.114 DEBUG: Log after MediatR call
        Logger.LogWarning("[DEBUG-CONTROLLER-RESULT] MediatR returned - IsSuccess: {IsSuccess}, Error: {Error}",
            result.IsSuccess, result.Error ?? "none");

        return HandleResult(result);
    }

    /// <summary>
    /// Check if an email has registered for an event (for sign-up validation)
    /// Phase 6A.15: Enhanced sign-up list UX with email validation
    /// Phase 6A.23: Updated to return detailed member/registration status for proper UX flow
    /// </summary>
    [HttpPost("{eventId:guid}/check-registration")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventRegistrationCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckEventRegistrationByEmail(Guid eventId, [FromBody] CheckRegistrationRequest request)
    {
        Logger.LogInformation("Checking if email {Email} is registered for event {EventId}", request.Email, eventId);

        var query = new CheckEventRegistrationQuery(eventId, request.Email);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Anonymous user commits to bringing a specific item from a category-based sign-up list
    /// Phase 6A.23: Supports anonymous sign-up workflow
    /// Flow: Check member status → Check event registration → Allow/Deny commitment
    /// </summary>
    [HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit-anonymous")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitToSignUpItemAnonymous(
        Guid eventId,
        Guid signupId,
        Guid itemId,
        [FromBody] CommitToSignUpItemAnonymousRequest request)
    {
        // Phase 6A.116: Prevent Azure Container Apps ingress from caching POST responses
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        Logger.LogInformation("Anonymous user with email {Email} committing to item {ItemId} in sign-up list {SignUpId} for event {EventId}",
            request.ContactEmail, itemId, signupId, eventId);

        var command = new CommitToSignUpItemAnonymousCommand(
            eventId,
            signupId,
            itemId,
            request.ContactEmail,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactPhone,
            PhysicalQuantity: request.PhysicalQuantity,
            SlotsClaimed: request.SlotsClaimed);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Open Sign-Up Items (Phase 6A.27)

    /// <summary>
    /// Add a user-submitted Open item to a sign-up list
    /// Phase 6A.27: Allows authenticated users to add their own items to Open sign-up lists
    /// The user who creates the item is automatically committed to bringing it
    /// </summary>
    [HttpPost("{eventId:guid}/signups/{signupId:guid}/open-items")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddOpenSignUpItem(
        Guid eventId,
        Guid signupId,
        [FromBody] AddOpenSignUpItemRequest request)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} adding Open item '{ItemName}' to sign-up list {SignUpId} for event {EventId}",
            userId, request.ItemName, signupId, eventId);

        var command = new AddOpenSignUpItemCommand(
            eventId,
            signupId,
            userId,
            request.ItemName,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Add a user-submitted Open item to a sign-up list for anonymous users
    /// Phase 6A.44: Allows anonymous users (registered for event) to add Open items
    /// The user who creates the item is automatically committed to bringing it
    /// </summary>
    [HttpPost("{eventId:guid}/signups/{signupId:guid}/open-items-anonymous")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddOpenSignUpItemAnonymous(
        Guid eventId,
        Guid signupId,
        [FromBody] AddOpenSignUpItemAnonymousRequest request)
    {
        Logger.LogInformation("Anonymous user with email {Email} adding Open item '{ItemName}' to sign-up list {SignUpId} for event {EventId}",
            request.ContactEmail, request.ItemName, signupId, eventId);

        var command = new AddOpenSignUpItemAnonymousCommand(
            eventId,
            signupId,
            request.ContactEmail,
            request.ItemName,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactPhone);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update a user-submitted Open item
    /// Phase 6A.27: Allows users to update their own Open items
    /// </summary>
    [HttpPut("{eventId:guid}/signups/{signupId:guid}/open-items/{itemId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOpenSignUpItem(
        Guid eventId,
        Guid signupId,
        Guid itemId,
        [FromBody] UpdateOpenSignUpItemRequest request)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} updating Open item {ItemId} in sign-up list {SignUpId} for event {EventId}",
            userId, itemId, signupId, eventId);

        var command = new UpdateOpenSignUpItemCommand(
            eventId,
            signupId,
            itemId,
            userId,
            request.ItemName,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Cancel (delete) a user-submitted Open item
    /// Phase 6A.27: Allows users to cancel their own Open items
    /// </summary>
    [HttpDelete("{eventId:guid}/signups/{signupId:guid}/open-items/{itemId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOpenSignUpItem(
        Guid eventId,
        Guid signupId,
        Guid itemId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} canceling Open item {ItemId} in sign-up list {SignUpId} for event {EventId}",
            userId, itemId, signupId, eventId);

        var command = new CancelOpenSignUpItemCommand(
            eventId,
            signupId,
            itemId,
            userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #endregion

    #region Custom Forms (Survey/Form Sign-Up Type)

    // ==================== FORM MANAGEMENT (ORGANIZER) ====================

    /// <summary>
    /// Get all custom forms for an event (summary view)
    /// </summary>
    [HttpGet("{id:guid}/forms")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventFormDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventForms(Guid id)
    {
        Logger.LogInformation("Getting custom forms for event {EventId}", id);

        var query = new GetEventFormsQuery(id);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get form detail with questions (needed by attendees to fill out the form)
    /// </summary>
    [HttpGet("{id:guid}/forms/{formId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventFormDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventFormDetail(Guid id, Guid formId)
    {
        Logger.LogInformation("Getting form detail {FormId} for event {EventId}", formId, id);

        var query = new GetEventFormDetailQuery(id, formId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Create a new custom form with initial questions (Organizer only)
    /// </summary>
    [HttpPost("{id:guid}/forms")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateEventForm(Guid id, [FromBody] CreateEventFormRequest request)
    {
        Logger.LogInformation("Creating custom form '{Title}' with {QuestionCount} questions for event {EventId}",
            request.Title, request.Questions?.Count ?? 0, id);

        var questions = request.Questions?.Select(q => new CreateFormQuestionItem(
            q.QuestionText,
            q.QuestionType,
            q.IsRequired,
            q.SortOrder,
            q.HelpText,
            q.Options?.Select(o => new CreateQuestionOptionItem(o.Text, o.SortOrder)).ToList()
        )).ToList() ?? new List<CreateFormQuestionItem>();

        var command = new CreateEventFormCommand(
            id,
            request.Title,
            request.Description,
            request.AllowMultipleResponses,
            request.ResponseDeadline,
            request.MaxResponses,
            questions,
            request.AllowAttendeesToViewResponses);  // Phase 6A.146

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update form title, description, settings (Organizer only)
    /// </summary>
    [HttpPut("{id:guid}/forms/{formId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateEventForm(Guid id, Guid formId, [FromBody] UpdateEventFormRequest request)
    {
        Logger.LogInformation("Updating form {FormId} for event {EventId}", formId, id);

        var command = new UpdateEventFormCommand(
            id, formId,
            request.Title,
            request.Description,
            request.AllowMultipleResponses,
            request.ResponseDeadline,
            request.MaxResponses,
            request.AllowAttendeesToViewResponses);  // Phase 6A.146 (nullable; null = leave unchanged)

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete a form (only if no responses exist) (Organizer only)
    /// </summary>
    [HttpDelete("{id:guid}/forms/{formId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteEventForm(Guid id, Guid formId)
    {
        Logger.LogInformation("Deleting form {FormId} for event {EventId}", formId, id);

        var command = new DeleteEventFormCommand(id, formId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Publish form (Draft -> Active) (Organizer only)
    /// </summary>
    [HttpPost("{id:guid}/forms/{formId:guid}/publish")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PublishEventForm(Guid id, Guid formId)
    {
        Logger.LogInformation("Publishing form {FormId} for event {EventId}", formId, id);

        var command = new PublishEventFormCommand(id, formId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Close form (Active -> Closed) (Organizer only)
    /// </summary>
    [HttpPost("{id:guid}/forms/{formId:guid}/close")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CloseEventForm(Guid id, Guid formId)
    {
        Logger.LogInformation("Closing form {FormId} for event {EventId}", formId, id);

        var command = new CloseEventFormCommand(id, formId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Reopen form (Closed -> Active) (Organizer only)
    /// </summary>
    [HttpPost("{id:guid}/forms/{formId:guid}/reopen")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReopenEventForm(Guid id, Guid formId)
    {
        Logger.LogInformation("Reopening form {FormId} for event {EventId}", formId, id);

        var command = new ReopenEventFormCommand(id, formId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== QUESTION MANAGEMENT (ORGANIZER) ====================

    /// <summary>
    /// Add a question to a form (Organizer only)
    /// </summary>
    [HttpPost("{id:guid}/forms/{formId:guid}/questions")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddFormQuestion(Guid id, Guid formId, [FromBody] AddFormQuestionRequest request)
    {
        Logger.LogInformation("Adding question to form {FormId} for event {EventId}", formId, id);

        var command = new AddFormQuestionCommand(
            id, formId,
            request.QuestionText,
            request.QuestionType,
            request.IsRequired,
            request.SortOrder,
            request.HelpText,
            request.Options?.Select(o => new AddQuestionOptionItem(o.Text, o.SortOrder)).ToList());

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update a question (Organizer only)
    /// </summary>
    [HttpPut("{id:guid}/forms/{formId:guid}/questions/{questionId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateFormQuestion(Guid id, Guid formId, Guid questionId, [FromBody] UpdateFormQuestionRequest request)
    {
        Logger.LogInformation("Updating question {QuestionId} on form {FormId} for event {EventId}", questionId, formId, id);

        var command = new UpdateFormQuestionCommand(
            id, formId, questionId,
            request.QuestionText,
            request.QuestionType,
            request.IsRequired,
            request.SortOrder,
            request.HelpText,
            request.Options?.Select(o => new UpdateQuestionOptionItem(o.Id, o.Text, o.SortOrder)).ToList());

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete a question (blocked if form has responses) (Organizer only)
    /// </summary>
    [HttpDelete("{id:guid}/forms/{formId:guid}/questions/{questionId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteFormQuestion(Guid id, Guid formId, Guid questionId)
    {
        Logger.LogInformation("Deleting question {QuestionId} from form {FormId} for event {EventId}", questionId, formId, id);

        var command = new DeleteFormQuestionCommand(id, formId, questionId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Reorder questions within a form (Organizer only)
    /// </summary>
    [HttpPut("{id:guid}/forms/{formId:guid}/questions/reorder")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReorderFormQuestions(Guid id, Guid formId, [FromBody] ReorderFormQuestionsRequest request)
    {
        Logger.LogInformation("Reordering questions on form {FormId} for event {EventId}", formId, id);

        var command = new ReorderFormQuestionsCommand(id, formId, request.QuestionIdsInOrder);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== RESPONSE SUBMISSION (ATTENDEE - ANONYMOUS) ====================

    /// <summary>
    /// Submit a response to a form (AllowAnonymous - anyone with the link)
    /// Returns responseId and access token for editing
    /// </summary>
    [HttpPost("{id:guid}/forms/{formId:guid}/responses")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SubmitFormResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitFormResponse(Guid id, Guid formId, [FromBody] SubmitFormResponseRequest request)
    {
        Logger.LogInformation("Submitting response to form {FormId} for event {EventId}", formId, id);

        var answers = request.Answers.Select(a => new SubmitFormAnswerItem(
            a.QuestionId,
            a.TextValue,
            a.SelectedOptionIds,
            a.BooleanValue
        )).ToList();

        Guid? userId = null;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            userId = parsedUserId;
        }

        var command = new SubmitFormResponseCommand(
            id, formId,
            request.RespondentEmail,
            request.RespondentName,
            userId,
            answers);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update own response (Phase 6A.106-110 Fix: Supports both token and userId auth)
    /// Phase 6A.116 Issue #3: Added X-Access-Token header support for anonymous users
    /// Anonymous users: Requires access token (query string ?token= OR X-Access-Token header)
    /// Logged-in users: Uses userId from JWT token (no access token needed)
    /// </summary>
    [HttpPut("{id:guid}/forms/{formId:guid}/responses/{responseId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateFormResponse(
        Guid id,
        Guid formId,
        Guid responseId,
        [FromBody] UpdateFormResponseRequest request,
        [FromQuery] string? token = null,
        [FromHeader(Name = "X-Access-Token")] string? headerToken = null)
    {
        var userId = User.GetUserId();

        // Phase 6A.116 Issue #3: Prefer header token over query string token
        var accessToken = headerToken ?? token;

        Logger.LogInformation(
            "UpdateFormResponse START: ResponseId={ResponseId}, FormId={FormId}, EventId={EventId}, UserId={UserId}, HasToken={HasToken}, TokenSource={TokenSource}, AnswerCount={AnswerCount}",
            responseId, formId, id, userId, !string.IsNullOrEmpty(accessToken),
            headerToken != null ? "Header" : (token != null ? "QueryString" : "None"),
            request?.Answers?.Count ?? 0);

        // Validate request body
        if (request == null || request.Answers == null || request.Answers.Count == 0)
        {
            Logger.LogWarning("UpdateFormResponse FAILED: Invalid request body - ResponseId={ResponseId}, RequestNull={RequestNull}, AnswersNull={AnswersNull}",
                responseId, request == null, request?.Answers == null);
            return BadRequest("Request body is required with at least one answer");
        }

        var answers = request.Answers.Select(a => new UpdateFormAnswerItem(
            a.QuestionId,
            a.TextValue,
            a.SelectedOptionIds,
            a.BooleanValue
        )).ToList();

        var command = new UpdateFormResponseCommand(id, formId, responseId, accessToken, userId, answers);
        var result = await Mediator.Send(command);

        Logger.LogInformation("UpdateFormResponse COMPLETE: ResponseId={ResponseId}, Success={Success}",
            responseId, result.IsSuccess);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete/cancel a form response (Phase 6A.106)
    /// Phase 6A.116 Issue #3: Added X-Access-Token header support
    /// Anonymous users: Requires access token (query string ?token= OR X-Access-Token header)
    /// Logged-in users: Uses userId from auth token
    /// </summary>
    [HttpDelete("{id:guid}/forms/{formId:guid}/responses/{responseId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteFormResponse(
        Guid id,
        Guid formId,
        Guid responseId,
        [FromQuery] string? token = null,
        [FromHeader(Name = "X-Access-Token")] string? headerToken = null)
    {
        var userId = User.GetUserId();  // null if anonymous

        // Phase 6A.116 Issue #3: Prefer header token over query string token
        var accessToken = headerToken ?? token;

        Logger.LogInformation(
            "DeleteFormResponse START: ResponseId={ResponseId}, FormId={FormId}, EventId={EventId}, UserId={UserId}, TokenSource={TokenSource}",
            responseId, formId, id, userId,
            headerToken != null ? "Header" : (token != null ? "QueryString" : "None"));

        var command = new DeleteFormResponseCommand(id, formId, responseId, accessToken, userId);
        var result = await Mediator.Send(command);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    /// <summary>
    /// Get own response by userId (for logged-in users in Signup Forms tab)
    /// Phase 6A.106-110 Fix: Enables Edit/Delete buttons for logged-in users
    /// </summary>
    [HttpGet("{id:guid}/forms/{formId:guid}/responses/my")]
    [Authorize]
    [ProducesResponseType(typeof(FormResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyFormResponseByUserId(Guid id, Guid formId)
    {
        var userId = User.GetUserId(); // Returns Guid (not nullable)

        Logger.LogInformation("Getting own response for form {FormId} by userId {UserId}", formId, userId);

        var query = new GetMyFormResponseByUserIdQuery(formId, userId);
        var result = await Mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        // Return 204 No Content if user has no response (not an error)
        if (result.Value == null)
        {
            return NoContent();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get own response by access token (for edit page)
    /// Phase 6A.116 Issue #3: Supports token from both query string and X-Access-Token header
    /// </summary>
    [HttpGet("{id:guid}/forms/{formId:guid}/responses/mine")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FormResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyFormResponse(
        Guid id,
        Guid formId,
        [FromQuery] string? token = null,
        [FromHeader(Name = "X-Access-Token")] string? headerToken = null)
    {
        // Phase 6A.116 Issue #3: Prefer header token over query string token
        var accessToken = headerToken ?? token;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            Logger.LogWarning("GetMyFormResponse FAILED: No access token provided - FormId={FormId}", formId);
            return BadRequest("Access token is required (via query string ?token= or X-Access-Token header)");
        }

        Logger.LogInformation(
            "GetMyFormResponse START: FormId={FormId}, TokenSource={TokenSource}",
            formId,
            headerToken != null ? "Header" : "QueryString");

        var query = new GetMyFormResponseQuery(formId, accessToken);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    // ==================== RESPONSE VIEWING (ORGANIZER) ====================

    /// <summary>
    /// Get paginated responses for a form (Organizer only)
    /// </summary>
    [HttpGet("{id:guid}/forms/{formId:guid}/responses")]
    [Authorize]
    [ProducesResponseType(typeof(FormResponsesPagedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFormResponses(Guid id, Guid formId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        Logger.LogInformation("Getting responses for form {FormId} event {EventId}, page {Page}", formId, id, page);

        var query = new GetFormResponsesQuery(id, formId, page, pageSize);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.146 — public, PII-redacted form responses. Visible to any
    /// event visitor when the organizer has flipped AllowAttendeesToViewResponses
    /// to true AND the form is in Active or Closed status. Returns 404 for every
    /// denial case (form not found / wrong event / flag off / Draft / Archived)
    /// to avoid leaking the toggle's state.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="formId">Form ID</param>
    /// <returns>PII-redacted response list (ordinal labels + DateOnly submitted dates)</returns>
    [HttpGet("{id:guid}/forms/{formId:guid}/responses/public")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicFormResponsesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicFormResponses(Guid id, Guid formId)
    {
        Logger.LogInformation(
            "GetPublicFormResponses START: EventId={EventId}, FormId={FormId}", id, formId);

        var query = new GetPublicFormResponsesQuery(id, formId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Export custom form responses to CSV or Excel (organizer only)
    /// Phase 6A.110: Form response export functionality
    /// </summary>
    /// <param name="id">Event ID (GUID)</param>
    /// <param name="formId">Form ID (GUID)</param>
    /// <param name="format">Export format: 'csv' or 'excel' (default: csv)</param>
    /// <returns>File download with form responses</returns>
    [HttpGet("{id:guid}/forms/{formId:guid}/responses/export")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportFormResponses(
        Guid id,
        Guid formId,
        [FromQuery] string format = "csv")
    {
        var userId = User.GetUserId();

        // 1. Verify event ownership (organizer only)
        var eventQuery = new GetEventByIdQuery(id);
        var eventResult = await Mediator.Send(eventQuery);

        if (eventResult.IsFailure)
            return HandleResult(eventResult);

        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning(
                "User {UserId} attempted unauthorized form export for Event {EventId}",
                userId, id);
            return Forbid();
        }

        // 2. Parse format (default to CSV if invalid)
        var exportFormat = format.ToLower() switch
        {
            "excel" => ExportFormat.Excel,
            _ => ExportFormat.Csv
        };

        // 3. Export form responses
        var query = new ExportFormResponsesQuery(id, formId, exportFormat);
        var result = await Mediator.Send(query);

        if (result.IsFailure)
            return HandleResult(result);

        Logger.LogInformation(
            "Exported form responses: EventId={EventId}, FormId={FormId}, FileName={FileName}",
            id, formId, result.Value!.FileName);

        return File(result.Value.FileContent, result.Value.ContentType, result.Value.FileName);
    }

    #endregion

    #region Attendee Management & Export (Phase 6A.45)

    /// <summary>
    /// Get all attendees for an event (organizer only)
    /// Phase 6A.45: Attendee management and export system
    /// </summary>
    [HttpGet("{eventId:guid}/attendees")]
    [Authorize]
    [ProducesResponseType(typeof(EventAttendeesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventAttendees(Guid eventId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} requesting attendees for event {EventId}", userId, eventId);

        // First, get event to check ownership
        var eventQuery = new GetEventByIdQuery(eventId);
        var eventResult = await Mediator.Send(eventQuery);

        if (eventResult.IsFailure)
        {
            if (eventResult.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound();
            }
            return HandleResult(eventResult);
        }

        // Authorization: Only event organizer (primary or co-organizer) can view attendees — Phase 6A.133
        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning("User {UserId} attempted to access attendees for event {EventId} without authorization",
                userId, eventId);
            return Forbid();
        }

        // Get attendees
        var query = new LankaConnect.Application.Events.Queries.GetEventAttendees.GetEventAttendeesQuery(eventId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Export event attendees to Excel or CSV (organizer only)
    /// Phase 6A.45: Multi-sheet Excel export with signup lists
    /// </summary>
    [HttpGet("{eventId:guid}/export")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportEventAttendees(
        Guid eventId,
        [FromQuery] string format = "excel")
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} requesting export for event {EventId} in format {Format}",
            userId, eventId, format);

        // First, get event to check ownership
        var eventQuery = new GetEventByIdQuery(eventId);
        var eventResult = await Mediator.Send(eventQuery);

        if (eventResult.IsFailure)
        {
            if (eventResult.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound();
            }
            return HandleResult(eventResult);
        }

        // Authorization: Only event organizer (primary or co-organizer) can export attendees — Phase 6A.133
        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning("User {UserId} attempted to export attendees for event {EventId} without authorization",
                userId, eventId);
            return Forbid();
        }

        // Phase 6A.69: Parse format (added signuplistszip support)
        // Phase 6A.73: Added signuplistsexcel support
        // Phase 7D.1 Step 17: Added volunteerszip / volunteersexcel for Kind=Volunteers exports
        var exportFormat = format.ToLower() switch
        {
            "csv" => LankaConnect.Application.Events.Queries.ExportEventAttendees.ExportFormat.Csv,
            "signuplistszip" => LankaConnect.Application.Events.Queries.ExportEventAttendees.ExportFormat.SignUpListsZip,
            "signuplistsexcel" => LankaConnect.Application.Events.Queries.ExportEventAttendees.ExportFormat.SignUpListsExcel,
            "volunteerszip" => LankaConnect.Application.Events.Queries.ExportEventAttendees.ExportFormat.VolunteersZip,
            "volunteersexcel" => LankaConnect.Application.Events.Queries.ExportEventAttendees.ExportFormat.VolunteersExcel,
            _ => LankaConnect.Application.Events.Queries.ExportEventAttendees.ExportFormat.Excel
        };

        // Export attendees
        var query = new LankaConnect.Application.Events.Queries.ExportEventAttendees.ExportEventAttendeesQuery(
            eventId,
            exportFormat);
        var result = await Mediator.Send(query);

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        Logger.LogInformation("Successfully exported attendees for event {EventId} in {Format} format. File: {FileName}",
            eventId, format, result.Value!.FileName);

        // Phase 6A.73: Force Content-Type to application/zip to prevent ASP.NET Core's
        // automatic MIME type detection from overriding based on filename
        var contentType = result.Value.ContentType == "application/zip"
            ? "application/zip"
            : result.Value.ContentType;

        return File(
            result.Value.FileContent,
            contentType,
            result.Value.FileName
        );
    }

    /// <summary>
    /// Export all financial data for an event (organizer only).
    /// Excel: Multi-sheet workbook (Attendees, Donations, Collections, Sponsors, Add-Ons).
    /// CSV: ZIP archive containing 5 CSV files.
    /// </summary>
    [HttpGet("{eventId:guid}/export-all")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportAllFinancials(
        Guid eventId,
        [FromQuery] string format = "excel")
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} requesting export-all for event {EventId} in format {Format}",
            userId, eventId, format);

        var eventQuery = new GetEventByIdQuery(eventId);
        var eventResult = await Mediator.Send(eventQuery);

        if (eventResult.IsFailure)
        {
            if (eventResult.Errors.Any(e => e.Contains("not found")))
                return NotFound();
            return HandleResult(eventResult);
        }

        if (eventResult.Value!.IsCurrentUserOrganizer != true)
        {
            Logger.LogWarning("User {UserId} attempted to export-all for event {EventId} without authorization",
                userId, eventId);
            return Forbid();
        }

        var exportFormat = format.ToLowerInvariant() switch
        {
            "csv" => LankaConnect.Application.Events.Queries.ExportEventAttendees.ExportFormat.Csv,
            _ => LankaConnect.Application.Events.Queries.ExportEventAttendees.ExportFormat.Excel
        };

        var result = await Mediator.Send(
            new LankaConnect.Application.Events.Queries.ExportAllFinancials.ExportAllFinancialsQuery(
                eventId, exportFormat));

        if (result.IsFailure)
            return HandleResult(result);

        var contentType = result.Value.ContentType == "application/zip"
            ? "application/zip"
            : result.Value.ContentType;

        return File(result.Value.FileContent, contentType, result.Value.FileName);
    }

    #endregion

    #region Communication

    /// <summary>
    /// Phase 6A.61: Send event notification email to all attendees
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>Accepted with recipient count</returns>
    [HttpPost("{id:guid}/send-notification")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(int), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendEventNotification(Guid id)
    {
        Logger.LogInformation("[Phase 6A.61] API: Sending event notification for event {EventId}", id);

        var command = new SendEventNotificationCommand(id);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            Logger.LogInformation("[Phase 6A.61] API: Event notification queued successfully for event {EventId}", id);
            return Accepted(new { recipientCount = result.Value });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.76: Send manual reminder email to all registered attendees.
    /// Allows organizers to trigger reminders at any time from the Communications tab.
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="reminderType">Type of reminder: "1day", "2day", "7day", or "custom"</param>
    /// <returns>Accepted with recipient count</returns>
    [HttpPost("{id:guid}/send-reminder")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(int), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendEventReminder(Guid id, [FromQuery] string reminderType = "1day")
    {
        Logger.LogInformation(
            "[Phase 6A.76] API: Sending manual event reminder for event {EventId}, type={ReminderType}",
            id, reminderType);

        var command = new SendEventReminderCommand(id, reminderType);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            Logger.LogInformation(
                "[Phase 6A.76] API: Event reminder queued successfully for event {EventId}, recipients={Count}",
                id, result.Value);
            return Accepted(new { recipientCount = result.Value });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.X: Resend registration confirmation email to specific attendee (Organizer action)
    /// Allows organizers to manually resend confirmation emails from Attendees tab
    /// Works for both free and paid event registrations via shared email service
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="registrationId">Registration ID</param>
    /// <returns>Success message if email sent</returns>
    [HttpPost("{id:guid}/attendees/{registrationId:guid}/resend-confirmation")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendAttendeeConfirmation(Guid id, Guid registrationId)
    {
        // Get organizer ID from claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var organizerId))
        {
            Logger.LogWarning("Resend attendee confirmation attempted without valid user ID claim");
            return Unauthorized();
        }

        Logger.LogInformation(
            "[Phase 6A.X] API: Resending attendee confirmation - EventId={EventId}, RegistrationId={RegistrationId}, OrganizerId={OrganizerId}",
            id, registrationId, organizerId);

        var command = new ResendAttendeeConfirmationCommand(id, registrationId, organizerId);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            Logger.LogInformation(
                "[Phase 6A.X] API: Attendee confirmation resent successfully - RegistrationId={RegistrationId}",
                registrationId);
            return Ok(new { message = "Confirmation email resent successfully" });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.61: Get email notification history for an event
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>List of notification history records</returns>
    [HttpGet("{id:guid}/notification-history")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(List<EventNotificationHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEventNotificationHistory(Guid id)
    {
        Logger.LogInformation("[Phase 6A.61] API: Getting notification history for event {EventId}", id);

        var query = new GetEventNotificationHistoryQuery(id);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.76: Get event reminder history
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <returns>List of reminder history records aggregated by type and date</returns>
    [HttpGet("{id:guid}/reminder-history")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(List<EventReminderHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEventReminderHistory(Guid id)
    {
        Logger.LogInformation("[Phase 6A.76] API: Getting reminder history for event {EventId}", id);

        var query = new GetEventReminderHistoryQuery(id);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    // ==================== ADD ATTENDEES (DELTA PAYMENT) ====================
    // Phase: Add-Only Attendees with Delta Payment Feature

    /// <summary>
    /// Calculate the price for adding new attendees to an existing paid registration.
    /// Returns the delta amount to charge.
    /// Part of the Add-Only Attendees with Delta Payment feature.
    /// </summary>
    [HttpPost("registrations/{registrationId:guid}/calculate-addition")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdditionPriceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateAdditionPrice(
        Guid registrationId,
        [FromBody] CalculateAdditionPriceRequest request)
    {
        Logger.LogInformation(
            "[AddOnlyAttendees] API: Calculating addition price for registration {RegistrationId}, NewAttendeesCount={Count}",
            registrationId, request.NewAttendees?.Count ?? 0);

        var query = new CalculateAdditionPriceQuery(
            registrationId,
            request.NewAttendees?.Select(a => new NewAttendeeDto(a.Name, a.AgeCategory, a.Gender)).ToList()
                ?? new List<NewAttendeeDto>());

        var result = await Mediator.Send(query);

        if (result.IsFailure)
        {
            Logger.LogWarning(
                "[AddOnlyAttendees] API: Calculate addition price failed - RegistrationId={RegistrationId}, Error={Error}",
                registrationId, result.Error);
            return BadRequest(new ProblemDetails
            {
                Title = "Price Calculation Failed",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Initiate adding attendees to an existing paid registration.
    /// Creates a pending addition and returns a Stripe checkout URL.
    /// Part of the Add-Only Attendees with Delta Payment feature.
    /// </summary>
    /// <summary>
    /// Phase 7F-D (architect-approved 2026-04-30): initiate adding head-count attendees
    /// to an existing paid Mode-B registration. Mirrors /add-attendees but operates on
    /// the head-count axis. For free events the merge happens immediately + returns
    /// success with no Stripe URL; for paid events returns a Stripe checkout URL.
    /// </summary>
    [HttpPost("registrations/{registrationId:guid}/add-headcount")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddAttendees.InitiateAddAttendeesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiateAddHeadCount(
        Guid registrationId,
        [FromBody] InitiateAddHeadCountRequest request)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        Logger.LogInformation(
            "[7F-D] API: InitiateAddHeadCount RegId={RegId} UserId={UserId} Total={Total}",
            registrationId, userId, request.HeadCountDelta?.Total);

        if (request.HeadCountDelta == null)
            return BadRequest(new ProblemDetails { Title = "Missing body", Detail = "headCountDelta is required", Status = 400 });

        var command = new LankaConnect.Products.LankaEvents.Application.Commands.InitiateAddHeadCount.InitiateAddHeadCountCommand(
            RegistrationId: registrationId,
            HeadCountDelta: request.HeadCountDelta,
            SuccessUrl: request.SuccessUrl,
            CancelUrl: request.CancelUrl,
            UserId: userId);

        var result = await Mediator.Send(command);
        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Title = "Failed", Detail = result.Error, Status = 400 });

        if (!result.Value.Success)
            return BadRequest(new ProblemDetails { Title = "Validation", Detail = result.Value.ErrorMessage, Status = 400 });

        return Ok(result.Value);
    }

    [HttpPost("registrations/{registrationId:guid}/add-attendees")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InitiateAddAttendeesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiateAddAttendees(
        Guid registrationId,
        [FromBody] InitiateAddAttendeesRequest request)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;

        Logger.LogInformation(
            "[AddOnlyAttendees] API: Initiating add attendees for registration {RegistrationId}, UserId={UserId}, NewAttendeesCount={Count}",
            registrationId, userId, request.NewAttendees?.Count ?? 0);

        var command = new InitiateAddAttendeesCommand(
            registrationId,
            request.NewAttendees?.Select(a => new NewAttendeeDto(a.Name, a.AgeCategory, a.Gender)).ToList()
                ?? new List<NewAttendeeDto>(),
            request.SuccessUrl,
            request.CancelUrl,
            userId);

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            Logger.LogWarning(
                "[AddOnlyAttendees] API: Initiate add attendees failed - RegistrationId={RegistrationId}, Error={Error}",
                registrationId, result.Error);
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to Initiate Attendee Addition",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!result.Value.Success)
        {
            Logger.LogWarning(
                "[AddOnlyAttendees] API: Initiate add attendees business rule failure - RegistrationId={RegistrationId}, Error={Error}",
                registrationId, result.Value.ErrorMessage);
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot Add Attendees",
                Detail = result.Value.ErrorMessage,
                Status = StatusCodes.Status400BadRequest
            });
        }

        Logger.LogInformation(
            "[AddOnlyAttendees] API: Initiate add attendees succeeded - RegistrationId={RegistrationId}, AdditionId={AdditionId}",
            registrationId, result.Value.RegistrationAdditionId);

        return Ok(result.Value);
    }

    /// <summary>
    /// Get the pending attendee addition for a registration, if any.
    /// Part of the Add-Only Attendees with Delta Payment feature.
    /// </summary>
    [HttpGet("registrations/{registrationId:guid}/pending-addition")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PendingAdditionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPendingAddition(Guid registrationId)
    {
        Logger.LogInformation(
            "[AddOnlyAttendees] API: Getting pending addition for registration {RegistrationId}",
            registrationId);

        var query = new GetPendingAdditionQuery(registrationId);
        var result = await Mediator.Send(query);

        if (result.IsFailure)
        {
            Logger.LogWarning(
                "[AddOnlyAttendees] API: Get pending addition failed - RegistrationId={RegistrationId}, Error={Error}",
                registrationId, result.Error);
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to Get Pending Addition",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (result.Value == null)
        {
            return NoContent();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Cancel a pending attendee addition before payment completes.
    /// Part of the Add-Only Attendees with Delta Payment feature.
    /// </summary>
    [HttpDelete("registrations/{registrationId:guid}/pending-addition")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CancelPendingAdditionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelPendingAddition(Guid registrationId)
    {
        var userId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;

        Logger.LogInformation(
            "[AddOnlyAttendees] API: Cancelling pending addition for registration {RegistrationId}, UserId={UserId}",
            registrationId, userId);

        var command = new CancelPendingAdditionCommand(registrationId, userId);
        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            Logger.LogWarning(
                "[AddOnlyAttendees] API: Cancel pending addition failed - RegistrationId={RegistrationId}, Error={Error}",
                registrationId, result.Error);
            return BadRequest(new ProblemDetails
            {
                Title = "Failed to Cancel Pending Addition",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (!result.Value.Success)
        {
            Logger.LogWarning(
                "[AddOnlyAttendees] API: Cancel pending addition business rule failure - RegistrationId={RegistrationId}, Error={Error}",
                registrationId, result.Value.ErrorMessage);
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot Cancel Pending Addition",
                Detail = result.Value.ErrorMessage,
                Status = StatusCodes.Status400BadRequest
            });
        }

        Logger.LogInformation(
            "[AddOnlyAttendees] API: Cancel pending addition succeeded - RegistrationId={RegistrationId}, CancelledAdditionId={AdditionId}",
            registrationId, result.Value.CancelledAdditionId);

        return Ok(result.Value);
    }

    #endregion
}

// Request DTOs
public record CancelEventRequest(string Reason);

/// <summary>
/// Phase 7F-B: request body for <see cref="EventsController.ConvertRegistrationMode"/>.
/// </summary>
public record ConvertRegistrationModeRequest(
    LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode TargetMode,
    bool DryRun = false,
    bool NotifyAttendees = false);
public record PostponeEventRequest(string Reason);
// Phase 6A.11: Updated to support multi-attendee registrations with detailed attendee information
public record RsvpRequest(
    Guid UserId,
    // Legacy format (backward compatibility)
    int Quantity = 1,
    // New format (Session 21 - multi-attendee)
    List<LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent.AttendeeDto>? Attendees = null,
    // Contact information (new format only)
    string? Email = null,
    string? PhoneNumber = null,
    string? Address = null,
    // Session 23: Payment integration - URLs for Stripe Checkout redirect
    string? SuccessUrl = null,
    string? CancelUrl = null,
    // Donation Feature: Optional donation during registration (combined checkout)
    decimal? DonationAmount = null,
    string? DonorName = null,
    string? DonorPhone = null,
    string? DonorNotes = null,
    // Phase 6A.137F: Add-on, collection, and sponsor fields for bundled checkout
    List<LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent.AddOnSelectionDto>? AddOnSelections = null,
    decimal? CollectionAmount = null,
    string? CollectionNotes = null,
    decimal? SponsorAmount = null,
    string? SponsorOrganization = null,
    string? SponsorNotes = null,
    // Phase 6A.151 C5: pre-staged sponsor logo from POST /sponsors/staging-image
    string? SponsorStagingBlobName = null,
    string? SponsorStagingBlobUrl = null,
    // Phase 6A.148.W5.D10.c: optional sponsor-contact override fields. When the
    // bundled-at-registration flow sends these, they override the registering
    // user's defaults (parity with the standalone /sponsors flow which collects
    // sponsor name + email + phone explicitly). All three optional — blank fields
    // fall back to Attendees[0].Name + request.Email + request.PhoneNumber.
    string? SponsorName = null,
    string? SponsorEmail = null,
    string? SponsorPhone = null,
    // Phase 7A.6D: WhatsApp opt-in during registration
    string? WhatsAppPhoneNumber = null,
    // Phase 7E.3a: Head-count payload for B-mode events (mutually exclusive with Attendees;
    // handler dispatches by event.RegistrationMode).
    string? LeadAttendeeName = null,
    LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent.HeadCountDto? HeadCount = null,
    // Phase 8 S8.2.B: Assigned-seating fields. Required when the event's
    // SeatingMode == AssignedSeating; rejected for GeneralAdmission events.
    List<Guid>? SeatIds = null,
    string? SeatSessionId = null
);

// Phase 6A.11: AttendeeDto is imported from Application layer (RsvpToEvent namespace)

/// <summary>
/// Phase 6A.43: Updated to support multi-attendee format with AgeCategory/Gender
/// Supports both legacy format (Name/Age) and new format (Attendees array)
/// </summary>
/// <summary>
/// Phase 6A.44: Updated to include SuccessUrl and CancelUrl for Stripe Checkout
/// </summary>
public record AnonymousRegistrationRequest(
    // Legacy format fields (backward compatibility)
    string? Name = null,
    int? Age = null,
    // New format (Phase 6A.43 - multi-attendee with AgeCategory/Gender)
    List<AnonymousAttendeeDto>? Attendees = null,
    // Contact information (required)
    string Address = "",
    string Email = "",
    string PhoneNumber = "",
    // Quantity for multiple attendees
    int Quantity = 1,
    // Phase 6A.44: Stripe checkout URLs (required for paid events)
    string? SuccessUrl = null,
    string? CancelUrl = null,
    // Donation Feature: Optional donation during registration (combined checkout)
    decimal? DonationAmount = null,
    string? DonorName = null,
    string? DonorPhone = null,
    string? DonorNotes = null,
    // Phase 6A.137F: Add-on, collection, and sponsor fields for bundled checkout
    List<LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent.AddOnSelectionDto>? AddOnSelections = null,
    decimal? CollectionAmount = null,
    string? CollectionNotes = null,
    decimal? SponsorAmount = null,
    string? SponsorOrganization = null,
    string? SponsorNotes = null,
    // Phase 6A.151 C5: pre-staged sponsor logo from POST /sponsors/staging-image
    string? SponsorStagingBlobName = null,
    string? SponsorStagingBlobUrl = null,
    // Phase 6A.148.W5.D10.c: optional sponsor-contact override fields. When the
    // bundled-at-registration flow sends these, they override the registering
    // user's defaults (parity with the standalone /sponsors flow which collects
    // sponsor name + email + phone explicitly). All three optional — blank fields
    // fall back to Attendees[0].Name + request.Email + request.PhoneNumber.
    string? SponsorName = null,
    string? SponsorEmail = null,
    string? SponsorPhone = null,
    // Phase 7A.6D: WhatsApp opt-in during registration
    string? WhatsAppPhoneNumber = null,
    // Phase 7E.3a: Head-count payload for B-mode events (anonymous flow).
    string? LeadAttendeeName = null,
    LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent.HeadCountDto? HeadCount = null,
    // Phase 8 S8.2.B: Assigned-seating fields. Required when the event's
    // SeatingMode == AssignedSeating; rejected for GeneralAdmission events.
    List<Guid>? SeatIds = null,
    string? SeatSessionId = null);

/// <summary>
/// Attendee DTO for anonymous registration.
/// Phase 8 S8.2.D: Optional TicketTierId so anonymous buyers can register for
/// tiered events (mirrors auth-side RsvpRequest.AttendeeDto.TicketTierId).
/// </summary>
public record AnonymousAttendeeDto(
    string Name,
    LankaConnect.Products.LankaEvents.Domain.Enums.AgeCategory AgeCategory,
    LankaConnect.Products.LankaEvents.Domain.Enums.Gender? Gender = null,
    Guid? TicketTierId = null);

/// <summary>
/// Phase 6A.44: Response from anonymous registration
/// </summary>
public record AnonymousRegistrationResponse(
    bool Success,
    string? CheckoutUrl,
    string Message);

public record UpdateRsvpRequest(Guid UserId, int NewQuantity);
/// <summary>
/// Phase 6A.14: Request to update registration details
/// </summary>
public record UpdateRegistrationRequest(
    List<UpdateRegistrationAttendeeDto>? Attendees,
    string Email,
    string PhoneNumber,
    string? Address = null);
/// <summary>
/// Phase 6A.14: Attendee DTO for registration update
/// Phase 6A.43: Updated to use AgeCategory and Gender instead of Age
/// </summary>
public record UpdateRegistrationAttendeeDto(
    string Name,
    LankaConnect.Products.LankaEvents.Domain.Enums.AgeCategory AgeCategory,
    LankaConnect.Products.LankaEvents.Domain.Enums.Gender? Gender = null);
public record ApproveEventRequest(Guid ApprovedByAdminId);
public record RejectEventRequest(Guid RejectedByAdminId, string Reason);
public record EventReorderImagesRequest(Dictionary<Guid, int> NewOrders); // Epic 2 Phase 2
public record RecordShareRequest(string? Platform = null); // Epic 2: Social sharing tracking
// W5.2.a-fix (2026-06-28): AddPassRequest removed -- EventPass feature deleted.

public record AddSignUpListRequest(
    string Category,
    string Description,
    SignUpType SignUpType,
    List<string>? PredefinedItems = null); // Sign-up list management

// Category-Based Sign-Up Requests
public record CreateSignUpListRequest(
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems,
    List<SignUpItemRequestDto> Items,
    bool HasOpenItems = false,                // Phase 6A.28: Open Items support
    SignUpKind Kind = SignUpKind.Items);      // Phase 7D.1: Items (default) or Volunteers

public record CheckRegistrationRequest(string Email); // Phase 6A.15: Email validation for sign-ups

// Phase 6A.141: Paid-event ticket scanner request DTOs
public record ScanTicketQrRequest(string QrPayload);
public record ScanTicketByCodeRequest(string TicketCode);
public record UnmarkScannedRequest(string Reason);

public record UpdateSignUpListRequest(
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems,
    bool HasOpenItems = false); // Phase 6A.28: Open Items support

/// <summary>
/// Phase 6A.131: Updated to support both quantity-based and slot-based items in batch creation.
/// ItemType defaults to Quantity for backward compatibility.
/// </summary>
public record SignUpItemRequestDto(
    string ItemDescription,
    SignUpItemType ItemType = SignUpItemType.Quantity,
    SignUpItemCategory ItemCategory = SignUpItemCategory.Mandatory,
    int? TargetQuantity = null,
    int? AvailableSlots = null,
    int? SuggestedPerSlot = null,
    string? Notes = null);

/// <summary>
/// Phase 6A.121: Request to add a sign-up item with dual-field support.
/// Use ItemType=Quantity with TargetQuantity, or ItemType=Slot with AvailableSlots.
/// </summary>
public record AddSignUpItemRequest(
    string ItemDescription,
    SignUpItemType ItemType,
    SignUpItemCategory ItemCategory,
    int? TargetQuantity = null,
    int? AvailableSlots = null,
    int? SuggestedPerSlot = null,
    string? Notes = null);

/// <summary>
/// Phase 6A.132: Request body for reordering sign-up items. Must contain the complete ordered list
/// of item IDs — the aggregate rejects missing, extra, duplicate, or unknown IDs with HTTP 400.
/// </summary>
public record ReorderSignUpItemsRequest(IReadOnlyList<Guid> OrderedItemIds);

/// <summary>
/// Request to update a sign-up item.
/// Phase 6A.14: Edit Sign-Up Item feature
/// Phase 6A.131: Supports both quantity-based and slot-based items.
/// Send <see cref="TargetQuantity"/> for quantity-based items and <see cref="AvailableSlots"/>
/// (optionally with <see cref="SuggestedPerSlot"/>) for slot-based items. The server loads the
/// item and uses its type as the authority; sending the wrong field returns HTTP 400 with an
/// explicit message so the client can correct its payload.
/// </summary>
public record UpdateSignUpItemRequest(
    string ItemDescription,
    int? TargetQuantity = null,
    int? AvailableSlots = null,
    int? SuggestedPerSlot = null,
    string? Notes = null);

/// <summary>
/// Request to commit to bringing an item
/// Phase 2: Added optional contact information
/// </summary>
/// <summary>
/// Phase 6A.125: Added PhysicalQuantity and SlotsClaimed for dual-field support.
/// - Quantity: legacy field, used as PhysicalQuantity for quantity-based items if PhysicalQuantity not set.
/// - PhysicalQuantity: explicit for quantity-based items (e.g., "5 plates")
/// - SlotsClaimed: for slot-based items (e.g., "2 slots")
/// At least one of Quantity, PhysicalQuantity, or SlotsClaimed is required.
/// </summary>
public record CommitToSignUpItemRequest(
    Guid UserId,
    int Quantity = 1,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null,
    int? PhysicalQuantity = null,           // Phase 6A.125: For quantity-based items
    int? SlotsClaimed = null);              // Phase 6A.125: For slot-based items

/// <summary>
/// Request for anonymous user to commit to bringing an item
/// Phase 6A.23: Supports anonymous sign-up workflow
/// Phase 6A.125: Added PhysicalQuantity and SlotsClaimed for slot-based items.
/// Email is used to verify event registration and identify the anonymous user.
/// </summary>
public record CommitToSignUpItemAnonymousRequest(
    string ContactEmail,
    int Quantity = 1,
    string? Notes = null,
    string? ContactName = null,
    string? ContactPhone = null,
    int? PhysicalQuantity = null,           // Phase 6A.125: For quantity-based items
    int? SlotsClaimed = null);

/// <summary>
/// Request to add a user-submitted Open item to a sign-up list
/// Phase 6A.27: Open sign-up items feature
/// </summary>
public record AddOpenSignUpItemRequest(
    string ItemName,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null);

/// <summary>
/// Request to add a user-submitted Open item to a sign-up list for anonymous users
/// Phase 6A.44: Anonymous users can add Open items if registered for the event
/// </summary>
public record AddOpenSignUpItemAnonymousRequest(
    string ContactEmail,
    string ItemName,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactPhone = null);

/// <summary>
/// Request to update a user-submitted Open item
/// Phase 6A.27: Open sign-up items feature
/// </summary>
public record UpdateOpenSignUpItemRequest(
    string ItemName,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null);

/// <summary>
/// Issue #51: Request to update max attendees per registration
/// </summary>
public record UpdateMaxAttendeesPerRegistrationRequest(int MaxAttendeesPerRegistration);

// ==================== PHASE 6A.133: CO-ORGANIZER REQUEST DTOS ====================

/// <summary>
/// Phase 6A.133: Batch link co-organizers request body
/// </summary>
public record BatchLinkRequest
{
    public List<BatchLinkItem>? Links { get; init; }
}

public record BatchLinkItem
{
    public Guid ContactId { get; init; }
    public Guid UserId { get; init; }
}

// ==================== ADD ATTENDEES (DELTA PAYMENT) REQUEST DTOS ====================

/// <summary>
/// Request to calculate the price for adding new attendees.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public record CalculateAdditionPriceRequest(
    List<AddAttendeeDto>? NewAttendees);

/// <summary>
/// Request to initiate adding attendees to a paid registration.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
/// <summary>
/// Phase 7F-D request body. <c>HeadCountDelta</c> uses the same shape as RSVP so the
/// frontend's existing head-count form components can be reused.
/// </summary>
public record InitiateAddHeadCountRequest(
    LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent.HeadCountDto HeadCountDelta,
    string SuccessUrl,
    string CancelUrl);

public record InitiateAddAttendeesRequest(
    List<AddAttendeeDto>? NewAttendees,
    string SuccessUrl,
    string CancelUrl);

/// <summary>
/// DTO for a new attendee being added.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public record AddAttendeeDto(
    string Name,
    LankaConnect.Products.LankaEvents.Domain.Enums.AgeCategory AgeCategory,
    LankaConnect.Products.LankaEvents.Domain.Enums.Gender? Gender = null);

// ==================== CUSTOM FORMS REQUEST DTOS ====================

/// <summary>
/// Request to create a new custom form with initial questions
/// </summary>
public record CreateEventFormRequest(
    string Title,
    string? Description,
    bool AllowMultipleResponses,
    DateTime? ResponseDeadline,
    int? MaxResponses,
    List<CreateFormQuestionRequest>? Questions,
    // Phase 6A.146: organizer-controlled toggle for public response visibility.
    // Optional with default false so existing clients/Swagger requests unchanged.
    bool AllowAttendeesToViewResponses = false);

public record CreateFormQuestionRequest(
    string QuestionText,
    LankaConnect.Modules.Forms.Domain.Enums.FormQuestionType QuestionType,
    bool IsRequired,
    int SortOrder,
    string? HelpText,
    List<QuestionOptionRequest>? Options);

public record QuestionOptionRequest(string Text, int SortOrder);

/// <summary>
/// Request to update form details
/// </summary>
public record UpdateEventFormRequest(
    string Title,
    string? Description,
    bool AllowMultipleResponses,
    DateTime? ResponseDeadline,
    int? MaxResponses,
    // Phase 6A.146: nullable so a request that omits the field leaves the
    // domain flag unchanged. UI sends the explicit user choice.
    bool? AllowAttendeesToViewResponses = null);

/// <summary>
/// Request to add a question to a form
/// </summary>
public record AddFormQuestionRequest(
    string QuestionText,
    LankaConnect.Modules.Forms.Domain.Enums.FormQuestionType QuestionType,
    bool IsRequired,
    int SortOrder,
    string? HelpText,
    List<QuestionOptionRequest>? Options);

/// <summary>
/// Request to update a question
/// </summary>
public record UpdateFormQuestionRequest(
    string QuestionText,
    LankaConnect.Modules.Forms.Domain.Enums.FormQuestionType QuestionType,
    bool IsRequired,
    int SortOrder,
    string? HelpText,
    List<UpdateQuestionOptionRequest>? Options);

public record UpdateQuestionOptionRequest(Guid? Id, string Text, int SortOrder);

/// <summary>
/// Request to reorder questions
/// </summary>
public record ReorderFormQuestionsRequest(List<Guid> QuestionIdsInOrder);

/// <summary>
/// Request to submit a form response
/// </summary>
public record SubmitFormResponseRequest(
    string? RespondentEmail,
    string? RespondentName,
    List<SubmitFormAnswerRequest> Answers);

public record SubmitFormAnswerRequest(
    Guid QuestionId,
    string? TextValue,
    List<Guid>? SelectedOptionIds,
    bool? BooleanValue);

/// <summary>
/// Request to update a form response
/// </summary>
public record UpdateFormResponseRequest(
    List<UpdateFormAnswerRequest> Answers);

public record UpdateFormAnswerRequest(
    Guid QuestionId,
    string? TextValue,
    List<Guid>? SelectedOptionIds,
    bool? BooleanValue);

// Phase 8: Ticket Tier Management request DTOs

public record SetTicketingModeRequest(TicketingMode TicketingMode);

// Seating Redesign Slice 1: Set seating mode request DTO
public record SetSeatingModeRequest(SeatingMode SeatingMode);

public record AddTicketTierRequest(
    string Name,
    string? Description,
    decimal AdultPriceAmount,
    Currency AdultPriceCurrency,
    decimal? ChildPriceAmount,
    Currency? ChildPriceCurrency,
    int? ChildAgeLimit,
    int Capacity,
    int MaxPerUser = 10,
    int SortOrder = 0);

public record UpdateTicketTierRequest(
    string Name,
    string? Description,
    decimal AdultPriceAmount,
    Currency AdultPriceCurrency,
    decimal? ChildPriceAmount,
    Currency? ChildPriceCurrency,
    int? ChildAgeLimit,
    int Capacity,
    int MaxPerUser = 10,
    int SortOrder = 0);
