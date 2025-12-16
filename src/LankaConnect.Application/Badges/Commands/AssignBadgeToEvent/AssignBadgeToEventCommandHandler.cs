using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.AssignBadgeToEvent;

/// <summary>
/// Handler for AssignBadgeToEventCommand
/// Phase 6A.25: Assigns a badge to an event
/// Phase 6A.28: Added duration-based expiration support
/// </summary>
public class AssignBadgeToEventCommandHandler : IRequestHandler<AssignBadgeToEventCommand, Result<EventBadgeDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IBadgeRepository _badgeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AssignBadgeToEventCommandHandler(
        IEventRepository eventRepository,
        IBadgeRepository badgeRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _badgeRepository = badgeRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EventBadgeDto>> Handle(AssignBadgeToEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<EventBadgeDto>.Failure($"Event with ID {request.EventId} not found");

        // 2. Get badge
        var badge = await _badgeRepository.GetByIdAsync(request.BadgeId, cancellationToken);
        if (badge == null)
            return Result<EventBadgeDto>.Failure($"Badge with ID {request.BadgeId} not found");

        // 3. Check if badge is active
        if (!badge.IsActive)
            return Result<EventBadgeDto>.Failure("Cannot assign an inactive badge to an event");

        // 4. Determine effective duration (Phase 6A.28)
        // If DurationDays not specified in request, use badge's DefaultDurationDays
        int? effectiveDuration = request.DurationDays ?? badge.DefaultDurationDays;

        // 5. Assign badge to event with duration
        var assignResult = @event.AssignBadge(
            request.BadgeId,
            _currentUserService.UserId,
            effectiveDuration,
            badge.DefaultDurationDays); // Pass max duration for validation

        if (!assignResult.IsSuccess)
            return Result<EventBadgeDto>.Failure(assignResult.Errors);

        // 6. Save changes
        _eventRepository.Update(@event);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 7. Return DTO with expiration info
        var eventBadge = assignResult.Value;
        // Phase 6A.31a: Use ToBadgeDto() extension method which handles obsolete property mapping
        var dto = new EventBadgeDto
        {
            Id = eventBadge.Id,
            EventId = eventBadge.EventId,
            BadgeId = eventBadge.BadgeId,
            Badge = badge.ToBadgeDto(),
            AssignedAt = eventBadge.AssignedAt,
            AssignedByUserId = eventBadge.AssignedByUserId,
            DurationDays = eventBadge.DurationDays,
            ExpiresAt = eventBadge.ExpiresAt,
            IsExpired = eventBadge.IsExpired()
        };

        return Result<EventBadgeDto>.Success(dto);
    }
}
