using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventDonations;

/// <summary>
/// Handles retrieving all donations for an event with summary statistics.
/// </summary>
public class GetEventDonationsQueryHandler : IQueryHandler<GetEventDonationsQuery, EventDonationsResponse>
{
    private readonly IEventRepository _eventRepository;
    private readonly IDonationRepository _donationRepository;
    private readonly ILogger<GetEventDonationsQueryHandler> _logger;

    public GetEventDonationsQueryHandler(
        IEventRepository eventRepository,
        IDonationRepository donationRepository,
        ILogger<GetEventDonationsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _donationRepository = donationRepository;
        _logger = logger;
    }

    public async Task<Result<EventDonationsResponse>> Handle(
        GetEventDonationsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventDonations"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventDonations START: EventId={EventId}",
                request.EventId);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<EventDonationsResponse>.Failure("Event not found");

                var donations = await _donationRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                var donationDtos = donations.Select(d => new DonationDto
                {
                    Id = d.Id,
                    EventId = d.EventId,
                    RegistrationId = d.RegistrationId,
                    DonorUserId = d.DonorUserId,
                    DonorName = d.DonorName,
                    DonorEmail = d.DonorEmail,
                    DonorPhone = d.DonorPhone,
                    DonorNotes = d.DonorNotes,
                    Amount = d.Amount.Amount,
                    Currency = d.Amount.Currency.ToString(),
                    Status = d.Status.ToString(),
                    IsBundled = d.IsBundled,
                    StripeFeeAmount = d.StripeFeeAmount?.Amount,
                    PlatformCommissionAmount = d.PlatformCommissionAmount?.Amount,
                    OrganizerPayoutAmount = d.OrganizerPayoutAmount?.Amount,
                    CreatedAt = d.CreatedAt,
                    PaymentCompletedAt = d.PaymentCompletedAt
                }).ToList();

                var completedDonations = donations.Where(d => d.Status == DonationStatus.Completed).ToList();
                var totalAmount = completedDonations.Sum(d => d.Amount.Amount);
                var currency = completedDonations.FirstOrDefault()?.Amount.Currency.ToString() ?? "USD";

                var summary = new DonationSummaryDto
                {
                    TotalDonations = donations.Count,
                    CompletedDonations = completedDonations.Count,
                    TotalAmount = totalAmount,
                    AverageDonation = completedDonations.Count > 0 ? totalAmount / completedDonations.Count : 0,
                    Currency = currency,
                    TotalStripeFees = completedDonations.Sum(d => d.StripeFeeAmount?.Amount ?? 0),
                    TotalPlatformCommission = completedDonations.Sum(d => d.PlatformCommissionAmount?.Amount ?? 0),
                    TotalOrganizerPayout = completedDonations.Sum(d => d.OrganizerPayoutAmount?.Amount ?? 0)
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventDonations COMPLETE: EventId={EventId}, TotalDonations={Total}, CompletedDonations={Completed}, TotalAmount={Amount}, Duration={ElapsedMs}ms",
                    request.EventId, donations.Count, completedDonations.Count, totalAmount, stopwatch.ElapsedMilliseconds);

                return Result<EventDonationsResponse>.Success(new EventDonationsResponse
                {
                    EventId = request.EventId,
                    EventTitle = @event.Title.Value,
                    Donations = donationDtos,
                    Summary = summary
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventDonations FAILED: EventId={EventId}, Duration={ElapsedMs}ms",
                    request.EventId, stopwatch.ElapsedMilliseconds);

                return Result<EventDonationsResponse>.Failure($"Failed to retrieve donations: {ex.Message}");
            }
        }
    }
}
