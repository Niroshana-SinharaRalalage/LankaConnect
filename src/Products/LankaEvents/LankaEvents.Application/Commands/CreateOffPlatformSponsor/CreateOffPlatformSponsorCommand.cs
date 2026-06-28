using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateOffPlatformSponsor;

/// <summary>
/// Phase 6A.145 — organizer-add sponsor flow. Records a sponsorship that happened
/// OUTSIDE the platform — cash money handed directly to the organizer, or in-kind
/// items donated without a Stripe transaction.
///
/// Dual-mode like the public flow:
///   - Money: <see cref="Amount"/> + <see cref="Currency"/> required; creates a
///     Sponsor with SponsorType.Money and immediately completes it via
///     <see cref="Sponsor.CompleteAsOrganizerCash"/> (bypasses Stripe entirely).
///   - Item: <see cref="ItemName"/> required; creates a Sponsor with SponsorType.Item
///     via the existing <see cref="Sponsor.CreateItemSponsor"/> factory.
///
/// Optional image upload — Phase 6A.145 Commit 6 removed the threshold gate;
/// any sponsor (organizer-added or public) can attach an image regardless of amount.
///
/// Authorization: organizer-only (enforced by controller).
/// </summary>
public record CreateOffPlatformSponsorCommand : IRequest<Result<CreateOffPlatformSponsorResult>>
{
    public Guid EventId { get; init; }
    public SponsorType Type { get; init; }
    public string SponsorName { get; init; } = string.Empty;
    public string SponsorEmail { get; init; } = string.Empty;
    public string? SponsorPhone { get; init; }
    public string? SponsorOrganization { get; init; }
    public string? SponsorNotes { get; init; }
    // Money branch
    public decimal? Amount { get; init; }
    public Currency? Currency { get; init; }
    // Item branch
    public string? ItemName { get; init; }
    public string? ItemDescription { get; init; }
    public decimal? EstimatedValue { get; init; }
    // Optional image
    public byte[]? ImageData { get; init; }
    public string? ImageFileName { get; init; }
}

public record CreateOffPlatformSponsorResult(Guid SponsorId, string? ImageUrl);

public class CreateOffPlatformSponsorCommandHandler
    : IRequestHandler<CreateOffPlatformSponsorCommand, Result<CreateOffPlatformSponsorResult>>
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOffPlatformSponsorCommandHandler> _logger;

    public CreateOffPlatformSponsorCommandHandler(
        ISponsorRepository sponsorRepository,
        IEventRepository eventRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<CreateOffPlatformSponsorCommandHandler> logger)
    {
        _sponsorRepository = sponsorRepository;
        _eventRepository = eventRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CreateOffPlatformSponsorResult>> Handle(
        CreateOffPlatformSponsorCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateOffPlatformSponsor"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("Type", request.Type))
        {
            _logger.LogInformation(
                "CreateOffPlatformSponsor START: Type={Type}, Email={Email}, HasImage={HasImage}",
                request.Type, request.SponsorEmail, request.ImageData?.Length > 0);

            try
            {
                // 1. Load event for SponsorConfiguration enable check.
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event is null)
                    return Result<CreateOffPlatformSponsorResult>.NotFound("Event not found.");

                if (@event.SponsorConfig is null || !@event.SponsorConfig.IsEnabled)
                    return Result<CreateOffPlatformSponsorResult>.Failure(
                        "Sponsorships are not enabled for this event.");

                // 2. Build the Sponsor based on type.
                Sponsor sponsor;
                if (request.Type == SponsorType.Money)
                {
                    if (!request.Amount.HasValue || !request.Currency.HasValue)
                        return Result<CreateOffPlatformSponsorResult>.Failure(
                            "Money sponsors require amount and currency.");

                    var moneyResult = Money.Create(request.Amount.Value, request.Currency.Value);
                    if (!moneyResult.IsSuccess)
                        return Result<CreateOffPlatformSponsorResult>.Failure(moneyResult.Errors);

                    var createResult = Sponsor.CreateMoneySponsor(
                        request.EventId,
                        sponsorUserId: null,
                        request.SponsorName,
                        request.SponsorEmail,
                        request.SponsorPhone,
                        request.SponsorOrganization,
                        request.SponsorNotes,
                        moneyResult.Value);
                    if (!createResult.IsSuccess)
                        return Result<CreateOffPlatformSponsorResult>.Failure(createResult.Errors);

                    sponsor = createResult.Value;

                    // Immediately transition to Completed (bypasses Stripe).
                    var completeResult = sponsor.CompleteAsOrganizerCash();
                    if (!completeResult.IsSuccess)
                        return Result<CreateOffPlatformSponsorResult>.Failure(completeResult.Errors);
                }
                else if (request.Type == SponsorType.Item)
                {
                    if (string.IsNullOrWhiteSpace(request.ItemName))
                        return Result<CreateOffPlatformSponsorResult>.Failure(
                            "Item sponsors require an item name.");

                    var createResult = Sponsor.CreateItemSponsor(
                        request.EventId,
                        sponsorUserId: null,
                        request.SponsorName,
                        request.SponsorEmail,
                        request.SponsorPhone,
                        request.SponsorOrganization,
                        request.SponsorNotes,
                        request.ItemName!,
                        request.ItemDescription,
                        request.EstimatedValue);
                    if (!createResult.IsSuccess)
                        return Result<CreateOffPlatformSponsorResult>.Failure(createResult.Errors);

                    sponsor = createResult.Value;
                }
                else
                {
                    return Result<CreateOffPlatformSponsorResult>.Failure(
                        $"Unsupported sponsor type: {request.Type}");
                }

                // 3. Optional image upload. Organizer is the caller, so threshold is
                //    BYPASSED per architect E-1 organizer override.
                string? imageUrl = null;
                if (request.ImageData is not null && request.ImageData.Length > 0)
                {
                    var validation = _imageService.ValidateImage(
                        request.ImageData, request.ImageFileName ?? "sponsor-image");
                    if (!validation.IsSuccess)
                        return Result<CreateOffPlatformSponsorResult>.Failure(validation.Errors);

                    var upload = await _imageService.UploadImageAsync(
                        request.ImageData,
                        request.ImageFileName ?? "sponsor-image",
                        request.EventId,
                        cancellationToken);
                    if (!upload.IsSuccess)
                        return Result<CreateOffPlatformSponsorResult>.Failure(upload.Errors);

                    var setImg = sponsor.SetImage(upload.Value.Url, upload.Value.BlobName);
                    if (!setImg.IsSuccess)
                    {
                        // Rollback the uploaded blob if domain rejects.
                        await _imageService.DeleteImageAsync(upload.Value.Url, cancellationToken);
                        return Result<CreateOffPlatformSponsorResult>.Failure(setImg.Errors);
                    }
                    imageUrl = upload.Value.Url;
                }

                // 4. Persist (single transaction via UnitOfWork — the Sponsor was created
                //    in memory and is being added to the context here).
                await _sponsorRepository.AddAsync(sponsor, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "CreateOffPlatformSponsor SUCCESS: SponsorId={SponsorId}, ImageUrl={ImageUrl}",
                    sponsor.Id, imageUrl);

                return Result<CreateOffPlatformSponsorResult>.Success(
                    new CreateOffPlatformSponsorResult(sponsor.Id, imageUrl));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateOffPlatformSponsor FAILED");
                throw;
            }
        }
    }
}
