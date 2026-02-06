using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Queries.GetEventReminderHistory;

/// <summary>
/// Phase 6A.76: Handler for GetEventReminderHistory query
/// </summary>
public class GetEventReminderHistoryQueryHandler : IRequestHandler<GetEventReminderHistoryQuery, Result<List<EventReminderHistoryDto>>>
{
    private readonly IEventReminderRepository _reminderRepository;
    private readonly ILogger<GetEventReminderHistoryQueryHandler> _logger;

    public GetEventReminderHistoryQueryHandler(
        IEventReminderRepository reminderRepository,
        ILogger<GetEventReminderHistoryQueryHandler> logger)
    {
        _reminderRepository = reminderRepository;
        _logger = logger;
    }

    public async Task<Result<List<EventReminderHistoryDto>>> Handle(
        GetEventReminderHistoryQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[Phase 6A.76] Getting reminder history for event {EventId}",
                request.EventId);

            var history = await _reminderRepository.GetReminderHistoryAsync(
                request.EventId,
                cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.76] Retrieved {Count} reminder history records for event {EventId}",
                history.Count, request.EventId);

            return Result<List<EventReminderHistoryDto>>.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.76] Error getting reminder history for event {EventId}",
                request.EventId);

            return Result<List<EventReminderHistoryDto>>.Failure($"Failed to get reminder history: {ex.Message}");
        }
    }
}
