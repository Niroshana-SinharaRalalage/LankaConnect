using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateSponsor;

/// <summary>
/// Phase 6A.151 — handles the PATCH /sponsors/{id} flow.
///
/// Responsibilities:
///   1. Load sponsor + parent event
///   2. Resolve actor role: organizer vs sponsor-self vs forbidden
///   3. Re-validate MinSponsorAmount IF Amount is in the patch
///   4. Apply each non-null patch field via the corresponding granular domain
///      mutator (state-matrix enforcement lives in the aggregate)
///   5. Persist + project to SponsorDto for the response
///
/// Observability:
///   - PushProperty(Operation/EntityType/SponsorId/EventId/Actor/IsOrganizer)
///   - One structured info log per allowed edit (FieldsChanged[] enumerated)
///   - One warn per rule rejection with the domain error verbatim
///   - try/catch envelope so a thrown exception surfaces as a Result.Failure
///     with the message + stack ID instead of bubbling 500s
///
/// Cache invalidation audit (architect H5): a repo-wide grep for
/// IDistributedCache in src/LankaConnect.Application returned zero hits, so
/// there is no aggregate cache to invalidate on amount edits. Documented in
/// the C2 commit message; revisit if a cache layer is introduced later.
/// </summary>
public class UpdateSponsorCommandHandler : ICommandHandler<UpdateSponsorCommand, SponsorDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateSponsorCommandHandler> _logger;

    public UpdateSponsorCommandHandler(
        IEventRepository eventRepository,
        ISponsorRepository sponsorRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateSponsorCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _sponsorRepository = sponsorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SponsorDto>> Handle(UpdateSponsorCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateSponsor"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("SponsorId", request.SponsorId))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("ActorUserId", request.ActingUserId))
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "UpdateSponsor START: SponsorId={SponsorId}, ActorUserId={ActorUserId}",
                request.SponsorId, request.ActingUserId);

            try
            {
                // 1. Load sponsor + parent event
                var sponsor = await _sponsorRepository.GetByIdAsync(request.SponsorId, cancellationToken);
                if (sponsor == null)
                {
                    _logger.LogWarning("UpdateSponsor: sponsor not found: {SponsorId}", request.SponsorId);
                    return Result<SponsorDto>.NotFound("Sponsor not found");
                }

                if (sponsor.EventId != request.EventId)
                {
                    _logger.LogWarning(
                        "UpdateSponsor: event id mismatch — request {RequestEventId} vs sponsor {SponsorEventId}",
                        request.EventId, sponsor.EventId);
                    return Result<SponsorDto>.NotFound("Sponsor not found");
                }

                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    _logger.LogWarning("UpdateSponsor: parent event not found: {EventId}", request.EventId);
                    return Result<SponsorDto>.NotFound("Event not found");
                }

                // 2. Resolve actor role
                var isOrganizer = @event.IsOrganizer(request.ActingUserId);
                var isSelf = sponsor.SponsorUserId.HasValue && sponsor.SponsorUserId.Value == request.ActingUserId;

                if (!isOrganizer && !isSelf)
                {
                    _logger.LogWarning(
                        "UpdateSponsor FORBIDDEN: Actor {ActorUserId} is neither the sponsor owner ({SponsorUserId}) nor an organizer of event {EventId}",
                        request.ActingUserId, sponsor.SponsorUserId, request.EventId);
                    return Result<SponsorDto>.Forbidden("You are not authorized to edit this sponsorship");
                }

                using (LogContext.PushProperty("IsOrganizerActor", isOrganizer))
                {
                    var fieldsChanged = new List<string>();

                    // 3. MinSponsorAmount re-validation gate (Architect H3)
                    //    Only validate when Amount is in the patch AND new value
                    //    falls below the current event-level minimum. No-change
                    //    and notes-only edits bypass this entirely so a stale
                    //    minimum doesn't block legitimate typo fixes.
                    if (request.Amount.HasValue && @event.SponsorConfig?.MinSponsorAmount.HasValue == true)
                    {
                        if (request.Amount.Value < @event.SponsorConfig.MinSponsorAmount.Value)
                        {
                            _logger.LogWarning(
                                "UpdateSponsor REJECTED MinAmount: requested {Requested} < event min {Min}",
                                request.Amount.Value, @event.SponsorConfig.MinSponsorAmount.Value);
                            return Result<SponsorDto>.Failure(
                                $"Sponsor amount must be at least {@event.SponsorConfig.MinSponsorAmount.Value:C}");
                        }
                    }

                    // 4. Apply each non-null patch field via its granular mutator.
                    //    State-matrix enforcement lives in the aggregate; any
                    //    domain rejection short-circuits the rest of the patch.

                    if (request.Name != null)
                    {
                        var r = sponsor.UpdateName(request.Name, request.ActingUserId, isOrganizer);
                        if (r.IsFailure)
                            return Reject(r.Error, nameof(request.Name));
                        fieldsChanged.Add(nameof(request.Name));
                    }

                    if (request.Notes != null || request.Organization != null)
                    {
                        var r = sponsor.UpdateContactFields(
                            request.Notes,
                            request.Organization,
                            request.ActingUserId,
                            isOrganizer);
                        if (r.IsFailure)
                            return Reject(r.Error, "Contact");
                        if (request.Notes != null) fieldsChanged.Add(nameof(request.Notes));
                        if (request.Organization != null) fieldsChanged.Add(nameof(request.Organization));
                    }

                    if (request.Amount.HasValue)
                    {
                        var currency = ParseCurrency(request.Currency) ?? sponsor.Amount?.Currency ?? Currency.USD;
                        var moneyResult = Money.Create(request.Amount.Value, currency);
                        if (moneyResult.IsFailure)
                            return Reject(moneyResult.Error, nameof(request.Amount));

                        var r = sponsor.UpdateAmount(moneyResult.Value, request.ActingUserId, isOrganizer);
                        if (r.IsFailure)
                            return Reject(r.Error, nameof(request.Amount));
                        fieldsChanged.Add(nameof(request.Amount));
                    }

                    if (request.ItemName != null || request.ItemDescription != null || request.EstimatedValue.HasValue)
                    {
                        var r = sponsor.UpdateItemDetails(
                            request.ItemName,
                            request.ItemDescription,
                            request.EstimatedValue,
                            request.ActingUserId);
                        if (r.IsFailure)
                            return Reject(r.Error, "Item");
                        if (request.ItemName != null) fieldsChanged.Add(nameof(request.ItemName));
                        if (request.ItemDescription != null) fieldsChanged.Add(nameof(request.ItemDescription));
                        if (request.EstimatedValue.HasValue) fieldsChanged.Add(nameof(request.EstimatedValue));
                    }

                    if (fieldsChanged.Count == 0)
                    {
                        _logger.LogInformation(
                            "UpdateSponsor NO-OP: no patch fields supplied — returning current DTO");
                        return Result<SponsorDto>.Success(MapToDto(sponsor));
                    }

                    // 5. Persist
                    _sponsorRepository.Update(sponsor);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "UpdateSponsor OK: SponsorId={SponsorId}, FieldsChanged=[{Fields}], ElapsedMs={Elapsed}",
                        sponsor.Id, string.Join(",", fieldsChanged), stopwatch.ElapsedMilliseconds);

                    return Result<SponsorDto>.Success(MapToDto(sponsor));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "UpdateSponsor FAILED: SponsorId={SponsorId}, ActorUserId={ActorUserId}",
                    request.SponsorId, request.ActingUserId);
                return Result<SponsorDto>.Failure($"Failed to update sponsor: {ex.Message}");
            }
        }
    }

    private Result<SponsorDto> Reject(string error, string field)
    {
        _logger.LogWarning("UpdateSponsor REJECTED {Field}: {Error}", field, error);
        return Result<SponsorDto>.Failure(error);
    }

    private static Currency? ParseCurrency(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return Enum.TryParse<Currency>(raw, ignoreCase: true, out var c) ? c : null;
    }

    private static SponsorDto MapToDto(Sponsor s) => new()
    {
        Id = s.Id,
        EventId = s.EventId,
        SponsorUserId = s.SponsorUserId,
        SponsorName = s.SponsorName,
        SponsorEmail = s.SponsorEmail,
        SponsorPhone = s.SponsorPhone,
        SponsorOrganization = s.SponsorOrganization,
        SponsorNotes = s.SponsorNotes,
        SponsorType = s.Type.ToString(),
        Amount = s.Amount?.Amount,
        Currency = s.Amount?.Currency.ToString(),
        Status = s.Status.ToString(),
        StripeFeeAmount = s.StripeFeeAmount?.Amount,
        PlatformCommissionAmount = s.PlatformCommissionAmount?.Amount,
        OrganizerPayoutAmount = s.OrganizerPayoutAmount?.Amount,
        ItemName = s.ItemName,
        ItemDescription = s.ItemDescription,
        EstimatedValue = s.EstimatedValue,
        ImageUrl = s.ImageUrl,
        ImageBlobName = s.ImageBlobName,
        CreatedAt = s.CreatedAt,
        PaymentCompletedAt = s.PaymentCompletedAt
    };
}
