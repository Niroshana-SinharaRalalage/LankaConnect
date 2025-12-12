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

        // 4. Assign badge to event
        var assignResult = @event.AssignBadge(request.BadgeId, _currentUserService.UserId);
        if (!assignResult.IsSuccess)
            return Result<EventBadgeDto>.Failure(assignResult.Errors);

        // 5. Save changes
        _eventRepository.Update(@event);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 6. Return DTO
        var eventBadge = assignResult.Value;
        var dto = new EventBadgeDto
        {
            Id = eventBadge.Id,
            EventId = eventBadge.EventId,
            BadgeId = eventBadge.BadgeId,
            Badge = new BadgeDto
            {
                Id = badge.Id,
                Name = badge.Name,
                ImageUrl = badge.ImageUrl,
                Position = badge.Position,
                IsActive = badge.IsActive,
                IsSystem = badge.IsSystem,
                DisplayOrder = badge.DisplayOrder,
                CreatedAt = badge.CreatedAt
            },
            AssignedAt = eventBadge.AssignedAt,
            AssignedByUserId = eventBadge.AssignedByUserId
        };

        return Result<EventBadgeDto>.Success(dto);
    }
}
