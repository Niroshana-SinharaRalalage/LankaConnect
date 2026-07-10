using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateSponsorshipPackage;

/// <summary>
/// Phase 6A.156 — handler for creating a sponsorship package. Validates the
/// event exists + is published, then defers to <see cref="SponsorshipPackage.Create"/>
/// for all field validation. Mirrors <c>CreateAddOnDefinitionCommandHandler</c>
/// byte-for-byte where possible.
///
/// Authorization is the controller's responsibility — once the command runs,
/// we trust the EventId belongs to a verified organizer of the current user.
/// </summary>
public class CreateSponsorshipPackageCommandHandler : ICommandHandler<CreateSponsorshipPackageCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly ISponsorshipPackageRepository _packageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateSponsorshipPackageCommandHandler> _logger;

    public CreateSponsorshipPackageCommandHandler(
        IEventRepository eventRepository,
        ISponsorshipPackageRepository packageRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateSponsorshipPackageCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _packageRepository = packageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateSponsorshipPackageCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateSponsorshipPackage"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateSponsorshipPackage START: EventId={EventId}, Name={Name}, Price={Price} {Currency}, QuantityLimit={QuantityLimit}, IncludedTickets={IncludedTickets}",
                request.EventId, request.Name, request.Price, request.Currency, request.QuantityLimit, request.IncludedTicketCount);

            try
            {
                // 1. Validate event exists and is in a publishable state
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result<Guid>.Failure("Event not found");

                if (@event.Status != LankaConnect.Products.LankaEvents.Domain.Enums.EventStatus.Published)
                    return Result<Guid>.Failure("Sponsorship packages can only be created for published events");

                // 2. Parse currency and build Money
                if (!MoneyBuilder.TryParseCurrency(request.Currency, out var currency))
                    return Result<Guid>.Failure($"Invalid currency: {request.Currency}");

                var priceResult = MoneyBuilder.Create(request.Price, currency);
                if (priceResult.IsFailure)
                    return Result<Guid>.Failure(priceResult.Error);

                // 3. Defer to domain factory — all field-level validation happens there
                var packageResult = SponsorshipPackage.Create(
                    request.EventId,
                    request.Name,
                    request.Description,
                    priceResult.Value,
                    request.QuantityLimit,
                    request.SortOrder,
                    request.Tier,
                    request.Perks,
                    request.IncludedTicketCount);

                if (packageResult.IsFailure)
                    return Result<Guid>.Failure(packageResult.Error);

                var package = packageResult.Value;

                // 4. Persist
                await _packageRepository.AddAsync(package, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateSponsorshipPackage COMPLETE: PackageId={PackageId}, EventId={EventId}, Name={Name}, Duration={ElapsedMs}ms",
                    package.Id, request.EventId, request.Name, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(package.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateSponsorshipPackage FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<Guid>.Failure($"Sponsorship package creation failed: {ex.Message}");
            }
        }
    }
}
