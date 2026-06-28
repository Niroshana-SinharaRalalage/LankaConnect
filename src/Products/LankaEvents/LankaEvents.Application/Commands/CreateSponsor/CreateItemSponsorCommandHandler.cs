using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateSponsor;

/// <summary>
/// Handles item-based sponsorship creation. No Stripe checkout needed — the entity
/// is immediately recorded with Status = RecordedItem.
/// Flow: validate event + sponsors enabled + item sponsors accepted -> create Sponsor entity -> save -> return ID
/// </summary>
public class CreateItemSponsorCommandHandler : ICommandHandler<CreateItemSponsorCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateItemSponsorCommandHandler> _logger;

    public CreateItemSponsorCommandHandler(
        IEventRepository eventRepository,
        ISponsorRepository sponsorRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateItemSponsorCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _sponsorRepository = sponsorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateItemSponsorCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateItemSponsor"))
        using (LogContext.PushProperty("EntityType", "Sponsor"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateItemSponsor START: EventId={EventId}, SponsorEmail={SponsorEmail}, ItemName={ItemName}, Organization={Organization}, IsAnonymous={IsAnonymous}",
                request.EventId, request.SponsorEmail, request.ItemName, request.SponsorOrganization, request.UserId == null);

            try
            {
                // 1. Validate event exists and is published
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<Guid>.Failure("Event not found");

                // 2. Validate event is Published
                if (@event.Status != LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published)
                    return Result<Guid>.Failure("Sponsorships are only available for published events");

                // 3. Validate sponsors are enabled AND item sponsors are accepted
                if (!@event.AreSponsorsEnabled())
                    return Result<Guid>.Failure("Sponsorships are not enabled for this event");

                if (!@event.SponsorConfig!.AcceptItemSponsors)
                    return Result<Guid>.Failure("Item sponsorships are not accepted for this event");

                // 4. Create Sponsor entity (immediately RecordedItem status)
                var sponsorResult = Sponsor.CreateItemSponsor(
                    request.EventId,
                    request.UserId,
                    request.SponsorName,
                    request.SponsorEmail,
                    request.SponsorPhone,
                    request.SponsorOrganization,
                    request.SponsorNotes,
                    request.ItemName,
                    request.ItemDescription,
                    request.EstimatedValue);

                if (sponsorResult.IsFailure)
                    return Result<Guid>.Failure(sponsorResult.Error);

                var sponsor = sponsorResult.Value;

                // 5. Save sponsor
                await _sponsorRepository.AddAsync(sponsor, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateItemSponsor COMPLETE: SponsorId={SponsorId}, EventId={EventId}, ItemName={ItemName}, Duration={ElapsedMs}ms",
                    sponsor.Id, request.EventId, request.ItemName, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(sponsor.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateItemSponsor FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<Guid>.Failure($"Item sponsorship creation failed: {ex.Message}");
            }
        }
    }
}
