using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Queries.GetEventNotificationHistory;

/// <summary>
/// Phase 6A.61: Handler for GetEventNotificationHistory query
/// Returns notification history with user names resolved for Communication tab display
/// </summary>
public class GetEventNotificationHistoryQueryHandler : IRequestHandler<GetEventNotificationHistoryQuery, Result<List<EventNotificationHistoryDto>>>
{
    private readonly IEventNotificationHistoryRepository _historyRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetEventNotificationHistoryQueryHandler> _logger;

    public GetEventNotificationHistoryQueryHandler(
        IEventNotificationHistoryRepository historyRepository,
        IUserRepository userRepository,
        ILogger<GetEventNotificationHistoryQueryHandler> logger)
    {
        _historyRepository = historyRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<List<EventNotificationHistoryDto>>> Handle(GetEventNotificationHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[Phase 6A.61] Getting notification history for event {EventId}", request.EventId);

            var historyRecords = await _historyRepository.GetByEventIdAsync(request.EventId, cancellationToken);

            var dtos = new List<EventNotificationHistoryDto>();

            foreach (var record in historyRecords)
            {
                // Resolve user name
                var user = await _userRepository.GetByIdAsync(record.SentByUserId, cancellationToken);
                var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User";

                dtos.Add(new EventNotificationHistoryDto
                {
                    Id = record.Id,
                    SentAt = record.SentAt,
                    SentByUserName = userName,
                    RecipientCount = record.RecipientCount,
                    SuccessfulSends = record.SuccessfulSends,
                    FailedSends = record.FailedSends
                });
            }

            _logger.LogInformation("[Phase 6A.61] Retrieved {Count} notification history records for event {EventId}",
                dtos.Count, request.EventId);

            return Result<List<EventNotificationHistoryDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.61] Error getting notification history for event {EventId}", request.EventId);
            return Result<List<EventNotificationHistoryDto>>.Failure("Failed to retrieve notification history");
        }
    }
}
