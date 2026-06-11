using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using System.Globalization;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using OrganizerContactInfo = LankaConnect.Shared.Email.Helpers.OrganizerContactInfo;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.24: Handles PaymentCompletedEvent to send confirmation email and generate tickets for paid events.
/// Phase 6A.100: Unified to always use ITypedEmailService with typed email parameters.
/// This handler is triggered after successful payment for paid event registrations.
/// </summary>
public class PaymentCompletedEventHandler : INotificationHandler<DomainEventNotification<PaymentCompletedEvent>>
{
    private readonly ITypedEmailService _typedEmailService;
    private readonly ITicketService _ticketService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IDonationRepository _donationRepository;
    // Phase 6A.137F: Add-on, collection, sponsor repos for email financial breakdown
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IFormRepository _eventFormRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<PaymentCompletedEventHandler> _logger;

    public PaymentCompletedEventHandler(
        ITypedEmailService typedEmailService,
        ITicketService ticketService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IDonationRepository donationRepository,
        // Phase 6A.137F: Inject bundling repos
        IAddOnPurchaseRepository addOnPurchaseRepository,
        ICollectionRepository collectionRepository,
        ISponsorRepository sponsorRepository,
        IFormRepository eventFormRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<PaymentCompletedEventHandler> logger)
    {
        _typedEmailService = typedEmailService;
        _ticketService = ticketService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _donationRepository = donationRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _collectionRepository = collectionRepository;
        _sponsorRepository = sponsorRepository;
        _eventFormRepository = eventFormRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var correlationId = Guid.NewGuid(); // Phase 6A.52: Correlation ID for end-to-end tracing

        using (LogContext.PushProperty("Operation", "PaymentCompleted"))
        using (LogContext.PushProperty("EntityType", "Payment"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RegistrationId", domainEvent.RegistrationId))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PaymentCompleted START: CorrelationId={CorrelationId}, EventId={EventId}, RegistrationId={RegistrationId}, Amount={Amount}, Email={Email}, PaymentIntent={PaymentIntent}",
                correlationId, domainEvent.EventId, domainEvent.RegistrationId, domainEvent.AmountPaid, domainEvent.ContactEmail, domainEvent.PaymentIntentId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Phase 6A.52: Step 1 - Retrieve event data
                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-1] Loading event - CorrelationId: {CorrelationId}, EventId: {EventId}",
                    correlationId, domainEvent.EventId);

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "[Phase 6A.52] [PaymentEmail-ERROR] Event not found - CorrelationId: {CorrelationId}, EventId: {EventId}",
                        correlationId, domainEvent.EventId);
                    return;
                }

                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-2] Event loaded successfully - CorrelationId: {CorrelationId}, EventTitle: {EventTitle}, Registrations: {RegistrationCount}",
                    correlationId, @event.Title.Value, @event.Registrations.Count);

                // Phase 6A.52: Step 2 - Find registration
                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-3] Looking for registration in event - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                    correlationId, domainEvent.RegistrationId);

                var registration = @event.Registrations.FirstOrDefault(r => r.Id == domainEvent.RegistrationId);
                if (registration == null)
                {
                    _logger.LogWarning(
                        "[Phase 6A.52] [PaymentEmail-ERROR] Registration not found in event - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, EventId: {EventId}",
                        correlationId, domainEvent.RegistrationId, domainEvent.EventId);
                    return;
                }

                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-4] Registration found - CorrelationId: {CorrelationId}, AttendeeCount: {AttendeeCount}, HasDetailedAttendees: {HasDetailedAttendees}",
                    correlationId, registration.Attendees.Count, registration.HasDetailedAttendees());

                // Determine recipient name and email
                string recipientName;
                string recipientEmail = domainEvent.ContactEmail;

                // Phase 6A.52: Step 3 - Determine recipient
                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-5] Determining recipient - CorrelationId: {CorrelationId}, HasUserId: {HasUserId}, ContactEmail: {ContactEmail}",
                    correlationId, domainEvent.UserId.HasValue, domainEvent.ContactEmail);

                if (domainEvent.UserId.HasValue)
                {
                    // Authenticated user - get user details
                    var user = await _userRepository.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
                    if (user != null)
                    {
                        recipientName = $"{user.FirstName} {user.LastName}";
                        recipientEmail = user.Email.Value;
                        _logger.LogInformation(
                            "[Phase 6A.52] [PaymentEmail-6] User found - CorrelationId: {CorrelationId}, RecipientName: {RecipientName}, RecipientEmail: {RecipientEmail}",
                            correlationId, recipientName, recipientEmail);
                    }
                    else
                    {
                        recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                            ? registration.Attendees.First().Name
                            : "Guest";
                        _logger.LogWarning(
                            "[Phase 6A.52] [PaymentEmail-WARN] User not found, using fallback name - CorrelationId: {CorrelationId}, UserId: {UserId}, FallbackName: {RecipientName}",
                            correlationId, domainEvent.UserId.Value, recipientName);
                    }
                }
                else
                {
                    // Anonymous user - use first attendee name or fallback
                    recipientName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                        ? registration.Attendees.First().Name
                        : "Guest";
                    _logger.LogInformation(
                        "[Phase 6A.52] [PaymentEmail-7] Anonymous user - CorrelationId: {CorrelationId}, RecipientName: {RecipientName}",
                        correlationId, recipientName);
                }

                // Phase 6A.X Issue #29: Validate ContactEmail is not empty/whitespace
                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    _logger.LogWarning(
                        "[Issue29] ContactEmail is empty/whitespace from domain event - CorrelationId: {CorrelationId}, " +
                        "RegistrationId: {RegistrationId}. Attempting recovery from registration entity.",
                        correlationId, domainEvent.RegistrationId);

                    // Recovery: Attempt to get email from registration entity
                    recipientEmail = registration.Contact?.Email
                                     ?? registration.AttendeeInfo?.Email?.Value
                                     ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(recipientEmail))
                    {
                        _logger.LogError(
                            "[Issue29] CRITICAL: Cannot determine recipient email after recovery - " +
                            "CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, " +
                            "HasContact: {HasContact}, HasAttendeeInfo: {HasAttendeeInfo}. " +
                            "Payment completed but NO email will be sent. Manual intervention required.",
                            correlationId, domainEvent.RegistrationId,
                            registration.Contact != null, registration.AttendeeInfo != null);
                        return;
                    }

                    _logger.LogInformation(
                        "[Issue29] Successfully recovered email from registration - CorrelationId: {CorrelationId}, " +
                        "RecoveredEmail: {RecoveredEmail}",
                        correlationId, recipientEmail);
                }

                // Phase 6A.43: Format attendee details - names only (no age) to match free event template
                var attendeeDetailsHtml = new System.Text.StringBuilder();

                if (registration.HasDetailedAttendees() && registration.Attendees.Any())
                {
                    foreach (var attendee in registration.Attendees)
                    {
                        // Phase 8: Include tier name if present (e.g., "John Smith (VIP)")
                        // Slice 7 S7.7: Append seat label for assigned-seating events
                        var tierSuffix = !string.IsNullOrWhiteSpace(attendee.TicketTierName)
                            ? $" <span style=\"color: #8B1538; font-weight: 600;\">({attendee.TicketTierName})</span>"
                            : "";
                        var seatSuffix = !string.IsNullOrWhiteSpace(attendee.SeatLabel)
                            ? $" <span style=\"color: #2563EB; font-weight: 600;\">(Seat {attendee.SeatLabel})</span>"
                            : "";
                        attendeeDetailsHtml.AppendLine($"<p style=\"margin: 8px 0; font-size: 16px;\">{attendee.Name}{tierSuffix}{seatSuffix}</p>");
                    }
                }

                // Get event's primary image URL
                var primaryImage = @event.Images.FirstOrDefault(i => i.IsPrimary);
                var eventImageUrl = primaryImage?.ImageUrl ?? "";

                // Phase 6A.100: Use typed TicketConfirmationEmailParams
                var typedParams = TicketConfirmationEmailParams.Create(
                    eventId: @event.Id,
                    registrationId: registration.Id,
                    userName: recipientName,
                    contactEmail: recipientEmail,
                    eventTitle: @event.Title.Value,
                    // Phase 8YA-2 TODO: payment can't fire on TBD event today (Register blocks).
                    eventStartDate: @event.StartDate.GetValueOrDefault(),
                    eventStartTime: EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId),
                    eventLocation: GetEventLocationString(@event),
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(@event.Id),
                    amountPaid: domainEvent.AmountPaid,
                    paymentIntentId: domainEvent.PaymentIntentId,
                    paymentDate: domainEvent.PaymentCompletedAt,
                    quantity: registration.Attendees.Count);

                // Phase 7C.2b: emit the 8 decomposed location keys (Venue Name bold +
                // Address + optional Secondary Location block) into the template dict.
                // Keeps the flat `{{EventLocation}}` fallback working for any un-migrated
                // template; the decomposed block is consumed once Chunk 2b rewrites the
                // paid-ticket template body.
                typedParams.WithLocationDetails(@event.ProjectEmailLocation());

                // Phase 6A.97: Set event's timezone for consistent date/time display
                typedParams.TimeZoneId = @event.TimeZoneId;

                // Phase 8: Set ticket type dynamically based on ticketing mode
                if (@event.IsFree())
                {
                    typedParams.TicketType = "Free Entry";
                }
                else if (@event.TicketingMode == TicketingMode.Tiered && registration.Attendees.Any(a => a.TicketTierName != null))
                {
                    // Build tier summary from attendee tier assignments (e.g., "2x VIP, 3x Basic")
                    var tierGroups = registration.Attendees
                        .Where(a => a.TicketTierName != null)
                        .GroupBy(a => a.TicketTierName!)
                        .Select(g => g.Count() > 1 ? $"{g.Count()}x {g.Key}" : g.Key)
                        .ToList();
                    typedParams.TicketType = string.Join(", ", tierGroups);
                    _logger.LogInformation(
                        "[Phase 8] [PaymentEmail-TierType] Set dynamic ticket type from tiers - CorrelationId: {CorrelationId}, TicketType: {TicketType}",
                        correlationId, typedParams.TicketType);
                }
                else
                {
                    typedParams.TicketType = "General Admission";
                }
                typedParams.RegistrationDate = domainEvent.PaymentCompletedAt;

                // Phase 6A.128: Fix - use WithSignUpLists() which sets BOTH URL and HasSignUpLists flag
                if (@event.HasSignUpLists())
                {
                    typedParams.WithSignUpLists($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups");
                }

                // Phase 6A.112: Check if event has active signup forms
                var forms = await _eventFormRepository.GetByEventIdAsync(@event.Id, cancellationToken);
                var hasActiveForms = forms.Any(f => f.Status == FormStatus.Active);

                if (hasActiveForms)
                {
                    typedParams.WithSignupForms($"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#signup-forms");
                }

                // Set attendee details
                if (registration.HasDetailedAttendees() && registration.Attendees.Any())
                {
                    typedParams.WithAttendees(attendeeDetailsHtml.ToString().TrimEnd());
                }

                // Phase 7F-E.6.B (architect-approved 2026-05-04): render the cross-surface
                // RegistrationBreakdown into HTML and store on TicketConfirmationEmailParams
                // so the paid-event template's {{{RegistrationBreakdownHtml}}} triple-stache
                // gets a real fragment instead of the literal token. Pre-fix the template
                // showed the raw `{{{RegistrationBreakdownHtml}}}` text in the user's
                // inbox because this handler missed wiring up the field in 7F-E.3.
                try
                {
                    var breakdown = TicketPdfRegistrationBreakdownAssembler.Build(registration);
                    var breakdownHtml = breakdown is null
                        ? string.Empty
                        : RegistrationBreakdownEmailRenderer.Render(breakdown, registration.LeadAttendeeName);
                    typedParams.WithRegistrationBreakdownHtml(breakdownHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[Phase 7F-E.6.B] Failed to render registration breakdown HTML for paid-event email. " +
                        "CorrelationId={CorrelationId}, RegistrationId={RegistrationId}. " +
                        "Falling back to empty string — email will send without the breakdown card.",
                        correlationId, registration.Id);
                    typedParams.WithRegistrationBreakdownHtml(string.Empty);
                }

                // Set event image
                typedParams.WithEventImage(eventImageUrl);

                // Set contact information
                if (registration.Contact != null)
                {
                    typedParams.WithContactInfo(registration.Contact.Email, registration.Contact.PhoneNumber);
                }
                else if (registration.AttendeeInfo != null)
                {
                    typedParams.WithContactInfo(registration.AttendeeInfo.Email.Value, null);
                }
                else
                {
                    typedParams.HasContactInfo = false;
                }

                // Set organizer contact
                if (@event.HasOrganizerContact())
                {
                    typedParams.WithOrganizerContacts(
                        @event.OrganizerContacts
                            .OrderBy(c => c.SortOrder)
                            .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                            .ToList());
                }

                // Phase 6A.137C/F: Load all bundled items for comprehensive financial breakdown
                try
                {
                    decimal donationTotal = 0m;
                    decimal addOnTotal = 0m;
                    decimal collectionTotal = 0m;
                    decimal sponsorTotal = 0m;

                    // Load bundled donation
                    var bundledDonation = await _donationRepository.GetByRegistrationIdAsync(
                        registration.Id, cancellationToken);
                    if (bundledDonation != null && bundledDonation.Amount.Amount > 0)
                    {
                        donationTotal = bundledDonation.Amount.Amount;
                    }

                    // Phase 6A.137F-Fix2: Load add-ons by user+event (catches bundled + standalone),
                    // fall back to checkout session ID for anonymous users
                    try
                    {
                        IReadOnlyList<AddOnPurchase>? addOnPurchases = null;
                        if (domainEvent.UserId.HasValue && domainEvent.UserId.Value != Guid.Empty)
                        {
                            addOnPurchases = await _addOnPurchaseRepository.GetByUserIdAndEventIdAsync(
                                domainEvent.UserId.Value, domainEvent.EventId, cancellationToken);
                        }
                        else if (!string.IsNullOrEmpty(registration.StripeCheckoutSessionId))
                        {
                            addOnPurchases = await _addOnPurchaseRepository.GetAllByCheckoutSessionIdAsync(
                                registration.StripeCheckoutSessionId, cancellationToken);
                        }
                        // Phase 6A.137F-Fix5: Scope add-ons to current registration only.
                        // Previous filter included Completed add-ons from ALL registrations (including
                        // old cancelled ones), contaminating the email with $0.00 entries.
                        // Now filters by RegistrationId first, then includes both Completed and Pending.
                        var completedAddOns = addOnPurchases?
                            .Where(p => p.RegistrationId == registration.Id
                                     && (p.Status == Domain.Events.Enums.AddOnPurchaseStatus.Completed
                                         || p.Status == Domain.Events.Enums.AddOnPurchaseStatus.Pending))
                            .ToList();

                        if (completedAddOns != null && completedAddOns.Count > 0)
                        {
                            addOnTotal = completedAddOns.Sum(p => p.TotalAmount.Amount);

                            // Build HTML rows for add-on breakdown
                            var addOnHtml = string.Join("", completedAddOns.Select((p, i) =>
                                $"<tr><td style=\"padding:4px 0;\">Add-on {i + 1} (x{p.Quantity})</td>" +
                                $"<td style=\"padding:4px 0;text-align:right;\">${p.TotalAmount.Amount:F2}</td></tr>"));

                            typedParams.WithAddOnBreakdown(addOnHtml, addOnTotal);

                            _logger.LogInformation(
                                "[Phase 6A.137F] Add-on breakdown loaded - CorrelationId: {CorrelationId}, Count={Count}, Total={Total}",
                                correlationId, completedAddOns.Count, addOnTotal);
                        }
                    }
                    catch (Exception addOnEx)
                    {
                        _logger.LogWarning(addOnEx,
                            "[Phase 6A.137F] Failed to load bundled add-ons - CorrelationId: {CorrelationId}",
                            correlationId);
                    }

                    // Phase 6A.137F: Load bundled collection by checkout session ID
                    try
                    {
                        if (!string.IsNullOrEmpty(registration.StripeCheckoutSessionId))
                        {
                            var bundledCollection = await _collectionRepository.GetByCheckoutSessionIdAsync(
                                registration.StripeCheckoutSessionId, cancellationToken);

                            if (bundledCollection != null && bundledCollection.Amount.Amount > 0)
                            {
                                collectionTotal = bundledCollection.Amount.Amount;
                                typedParams.WithCollectionBreakdown(collectionTotal);

                                _logger.LogInformation(
                                    "[Phase 6A.137F] Collection breakdown loaded - CorrelationId: {CorrelationId}, Amount={Amount}",
                                    correlationId, collectionTotal);
                            }
                        }
                    }
                    catch (Exception collectionEx)
                    {
                        _logger.LogWarning(collectionEx,
                            "[Phase 6A.137F] Failed to load bundled collection - CorrelationId: {CorrelationId}",
                            correlationId);
                    }

                    // Phase 6A.137F: Load bundled sponsor by checkout session ID
                    try
                    {
                        if (!string.IsNullOrEmpty(registration.StripeCheckoutSessionId))
                        {
                            var bundledSponsor = await _sponsorRepository.GetByCheckoutSessionIdAsync(
                                registration.StripeCheckoutSessionId, cancellationToken);

                            if (bundledSponsor != null && bundledSponsor.Amount != null && bundledSponsor.Amount.Amount > 0)
                            {
                                sponsorTotal = bundledSponsor.Amount.Amount;
                                typedParams.WithSponsorBreakdown(sponsorTotal);

                                _logger.LogInformation(
                                    "[Phase 6A.137F] Sponsor breakdown loaded - CorrelationId: {CorrelationId}, Amount={Amount}",
                                    correlationId, sponsorTotal);
                            }
                        }
                    }
                    catch (Exception sponsorEx)
                    {
                        _logger.LogWarning(sponsorEx,
                            "[Phase 6A.137F] Failed to load bundled sponsor - CorrelationId: {CorrelationId}",
                            correlationId);
                    }

                    // Phase 6A.137F-Fix: domainEvent.AmountPaid IS the ticket subtotal
                    // (sourced from Registration.TotalPrice.Amount which is ticket-only).
                    // Do NOT subtract bundled items — they are separate line items, not included in AmountPaid.
                    var ticketSubtotal = domainEvent.AmountPaid;
                    var grandTotal = ticketSubtotal + donationTotal + addOnTotal + collectionTotal + sponsorTotal;

                    if (donationTotal > 0 || addOnTotal > 0 || collectionTotal > 0 || sponsorTotal > 0)
                    {
                        typedParams.WithFinancialBreakdown(
                            ticketSubtotal: ticketSubtotal,
                            donationAmount: donationTotal,
                            currency: bundledDonation?.Amount.Currency.ToString() ?? "USD");

                        // Override AmountPaid to show the grand total (ticket + all bundled items)
                        // so the email header reflects the actual Stripe charge
                        typedParams.AmountPaid = grandTotal;

                        _logger.LogInformation(
                            "[Phase 6A.137F-Fix] [PaymentEmail-BREAKDOWN] Full financial breakdown - CorrelationId: {CorrelationId}, TicketSubtotal={TicketSubtotal}, Donation={Donation}, AddOns={AddOns}, Collection={Collection}, Sponsor={Sponsor}, GrandTotal={GrandTotal}",
                            correlationId, ticketSubtotal, donationTotal, addOnTotal, collectionTotal, sponsorTotal, grandTotal);
                    }
                }
                catch (Exception breakdownEx)
                {
                    _logger.LogWarning(breakdownEx,
                        "[Phase 6A.137F] [PaymentEmail-BREAKDOWN-WARN] Failed to load financial breakdown - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}",
                        correlationId, registration.Id);
                    // Non-critical: email still sends without breakdown
                }

                // Phase 6A.100: Validate parameters
                if (!typedParams.Validate(out var validationErrors))
                {
                    _logger.LogWarning(
                        "[Phase 6A.100] [PaymentEmail-VALIDATION-WARNING] Parameter validation failed - CorrelationId: {CorrelationId}, Errors: {Errors}",
                        correlationId, string.Join(", ", validationErrors));
                    // Continue anyway - validation is advisory
                }

                _logger.LogInformation(
                    "[Phase 6A.100] [PaymentEmail-TYPED] Built typed parameters - CorrelationId: {CorrelationId}",
                    correlationId);

                // Phase 6A.52: Step 4 - Generate ticket with QR code
                _logger.LogInformation(
                    "[Phase 6A.52] [PaymentEmail-8] Starting ticket generation - CorrelationId: {CorrelationId}, RegistrationId: {RegistrationId}, EventId: {EventId}",
                    correlationId, registration.Id, @event.Id);

                var ticketResult = await _ticketService.GenerateTicketAsync(
                    registration.Id,
                    @event.Id,
                    cancellationToken);

                byte[]? pdfAttachment = null;
                string ticketCode = "";

                if (ticketResult.IsSuccess)
                {
                    _logger.LogInformation(
                        "[Phase 6A.52] [PaymentEmail-9] Ticket generated successfully - CorrelationId: {CorrelationId}, TicketCode: {TicketCode}, TicketId: {TicketId}",
                        correlationId, ticketResult.Value.TicketCode, ticketResult.Value.TicketId);

                    ticketCode = ticketResult.Value.TicketCode;

                    // Phase 6A.100: Update typed params with ticket info using fluent method
                    // Phase 8YA-2 TODO: render expiry as "TBD" when EndDate is null on TBD events.
                    var ticketExpiryText = @event.EndDate.HasValue
                        ? @event.EndDate.Value.AddDays(1).ToString("MMMM dd, yyyy")
                        : "TBD";
                    typedParams.WithTicket(
                        ticketResult.Value.TicketCode,
                        ticketExpiryText,
                        _emailUrlHelper.BuildTicketViewUrl(ticketResult.Value.TicketId));

                    // Get PDF bytes for email attachment
                    _logger.LogInformation(
                        "[Phase 6A.52] [PaymentEmail-10] Retrieving ticket PDF - CorrelationId: {CorrelationId}, TicketId: {TicketId}",
                        correlationId, ticketResult.Value.TicketId);

                    var pdfResult = await _ticketService.GetTicketPdfAsync(ticketResult.Value.TicketId, cancellationToken);
                    if (pdfResult.IsSuccess)
                    {
                        pdfAttachment = pdfResult.Value;
                        _logger.LogInformation(
                            "[Phase 6A.52] [PaymentEmail-11] Ticket PDF retrieved successfully - CorrelationId: {CorrelationId}, Size: {Size} bytes",
                            correlationId, pdfAttachment.Length);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[Phase 6A.52] [PaymentEmail-WARN] Failed to retrieve ticket PDF - CorrelationId: {CorrelationId}, Errors: {Errors}",
                            correlationId, string.Join(", ", pdfResult.Errors));
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "[Phase 6A.52] [PaymentEmail-WARN] Failed to generate ticket - CorrelationId: {CorrelationId}, Errors: {Errors}",
                        correlationId, string.Join(", ", ticketResult.Errors));
                    typedParams.HasTicket = false;
                    typedParams.TicketUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);
                }

                // Phase 6A.100: Build attachment list
                var attachments = new List<EmailAttachmentDto>();
                if (pdfAttachment != null)
                {
                    attachments.Add(new EmailAttachmentDto
                    {
                        FileName = $"ticket-{ticketCode}.pdf",
                        Content = pdfAttachment,
                        ContentType = "application/pdf"
                    });
                }

                // Phase 6A.100: Send via ITypedEmailService with attachments
                _logger.LogInformation(
                    "[Phase 6A.100] [PaymentEmail-12] Sending email via ITypedEmailService - CorrelationId: {CorrelationId}, Template: {Template}, HasAttachment: {HasAttachment}",
                    correlationId, typedParams.TemplateName, attachments.Count > 0);

                TypedEmailSendResult typedResult;
                if (attachments.Count > 0)
                {
                    typedResult = await _typedEmailService.SendEmailWithAttachmentsAsync(
                        typedParams,
                        attachments,
                        cancellationToken);
                }
                else
                {
                    typedResult = await _typedEmailService.SendEmailAsync(
                        typedParams,
                        cancellationToken);
                }

                stopwatch.Stop();

                if (!typedResult.Success)
                {
                    _logger.LogError(
                        "PaymentCompleted FAILED: Email sending failed - CorrelationId={CorrelationId}, RecipientEmail={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        correlationId, recipientEmail, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "PaymentCompleted COMPLETE: Email sent successfully - CorrelationId={CorrelationId}, RecipientEmail={Email}, RegistrationId={RegistrationId}, AttendeeCount={AttendeeCount}, HasTicket={HasTicket}, Duration={ElapsedMs}ms",
                        correlationId, recipientEmail, domainEvent.RegistrationId, registration.Attendees.Count, typedParams.HasTicket, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "PaymentCompleted CANCELED: Operation was canceled - CorrelationId={CorrelationId}, EventId={EventId}, RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                    correlationId, domainEvent.EventId, domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Phase 6A.52: Enhanced exception logging with full context (fail-silent)
                _logger.LogError(ex,
                    "PaymentCompleted FAILED: Unhandled exception - CorrelationId={CorrelationId}, EventId={EventId}, RegistrationId={RegistrationId}, Email={Email}, Duration={ElapsedMs}ms",
                    correlationId, domainEvent.EventId, domainEvent.RegistrationId, domainEvent.ContactEmail, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Phase 6A.35: Safely extracts event location string with defensive null handling.
    /// Handles data inconsistency where has_location=true but address fields are null.
    /// </summary>
    private static string GetEventLocationString(Event @event)
    {
        // Check if Location or Address is null (defensive against data inconsistency)
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;

        // Handle case where address fields exist but are empty
        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        if (string.IsNullOrWhiteSpace(street))
            return city!;

        if (string.IsNullOrWhiteSpace(city))
            return street;

        return $"{street}, {city}";
    }

    /// <summary>
    /// Phase 6A.43: Formats event date/time range for display.
    /// Matches the format used in RegistrationConfirmedEventHandler.
    /// Examples:
    /// - Same day: "December 24, 2025 from 5:00 PM to 10:00 PM"
    /// - Different days: "December 24, 2025 at 5:00 PM to December 25, 2025 at 10:00 PM"
    /// </summary>
    private static string FormatEventDateTimeRange(DateTime startDate, DateTime endDate)
    {
        if (startDate.Date == endDate.Date)
        {
            // Same day event
            return $"{startDate:MMMM dd, yyyy} from {startDate:h:mm tt} to {endDate:h:mm tt}";
        }
        else
        {
            // Multi-day event
            return $"{startDate:MMMM dd, yyyy} at {startDate:h:mm tt} to {endDate:MMMM dd, yyyy} at {endDate:h:mm tt}";
        }
    }
}
