using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Queries.CheckVanitySlugAvailability;

public class CheckVanitySlugAvailabilityQueryHandler : IQueryHandler<CheckVanitySlugAvailabilityQuery, VanitySlugAvailabilityResult>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<CheckVanitySlugAvailabilityQueryHandler> _logger;

    public CheckVanitySlugAvailabilityQueryHandler(
        IEventRepository eventRepository,
        ILogger<CheckVanitySlugAvailabilityQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<VanitySlugAvailabilityResult>> Handle(
        CheckVanitySlugAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Shape + reserved-words check via the VO factory. Reuses the
            // exact same validation the command handlers apply on save —
            // no risk of FE seeing "available" then submit failing.
            var voResult = EventVanitySlug.Create(request.Slug);
            if (voResult.IsFailure)
            {
                var reason = voResult.Error.Contains("reserved")
                    ? "reserved"
                    : "invalid";
                return Result<VanitySlugAvailabilityResult>.Success(
                    new VanitySlugAvailabilityResult(false, reason, voResult.Error));
            }

            // Null VO from a valid call would mean empty input; treat as
            // available (the organizer hasn't typed anything yet).
            if (voResult.Value == null)
            {
                return Result<VanitySlugAvailabilityResult>.Success(
                    new VanitySlugAvailabilityResult(true, null, ""));
            }

            // Uniqueness check via partial unique index lookup.
            var exists = await _eventRepository.VanitySlugExistsAsync(
                voResult.Value.Value, cancellationToken);
            if (exists)
            {
                return Result<VanitySlugAvailabilityResult>.Success(
                    new VanitySlugAvailabilityResult(false, "taken",
                        $"'{voResult.Value.Value}' is already taken by another event."));
            }

            return Result<VanitySlugAvailabilityResult>.Success(
                new VanitySlugAvailabilityResult(true, null, "Available"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CheckVanitySlugAvailability: unexpected error - Slug={Slug}, Error={Error}",
                request.Slug, ex.Message);
            throw;
        }
    }
}
