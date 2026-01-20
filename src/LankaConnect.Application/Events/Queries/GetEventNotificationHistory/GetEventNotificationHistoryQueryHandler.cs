using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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
        using (LogContext.PushProperty("Operation", "GetEventNotificationHistory"))
        using (LogContext.PushProperty("EntityType", "NotificationHistory"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEventNotificationHistory START: EventId={EventId}",
                request.EventId);

            try
            {
                // Validate request
                if (request.EventId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventNotificationHistory FAILED: Invalid EventId - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<List<EventNotificationHistoryDto>>.Failure("Event ID is required");
                }

                var historyRecords = await _historyRepository.GetByEventIdAsync(request.EventId, cancellationToken);

                _logger.LogInformation(
                    "GetEventNotificationHistory: History records loaded - EventId={EventId}, RecordCount={RecordCount}",
                    request.EventId, historyRecords.Count);

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

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventNotificationHistory COMPLETE: EventId={EventId}, RecordCount={RecordCount}, TotalRecipients={TotalRecipients}, TotalSuccessful={TotalSuccessful}, TotalFailed={TotalFailed}, Duration={ElapsedMs}ms",
                    request.EventId, dtos.Count,
                    dtos.Sum(d => d.RecipientCount),
                    dtos.Sum(d => d.SuccessfulSends),
                    dtos.Sum(d => d.FailedSends),
                    stopwatch.ElapsedMilliseconds);

                return Result<List<EventNotificationHistoryDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventNotificationHistory FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                // Provide detailed error message to help debugging
                var errorMessage = $"Failed to retrieve notification history. Error: {ex.Message}";
                return Result<List<EventNotificationHistoryDto>>.Failure(errorMessage);
            }
        }
    }
}
