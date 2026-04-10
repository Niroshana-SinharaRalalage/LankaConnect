using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Application.Communications.WhatsApp.Queries.GetWhatsAppMessageHistory;

/// <summary>
/// Phase 7A.2: Query to get WhatsApp message history, filterable by user or event.
/// Supports pagination.
/// </summary>
public record GetWhatsAppMessageHistoryQuery(
    Guid? UserId,
    Guid? EventId,
    int Page = 1,
    int PageSize = 20) : IQuery<IReadOnlyList<WhatsAppMessageDto>>;

/// <summary>
/// DTO representing a WhatsApp message record in the history.
/// Phone numbers are masked for privacy (e.g., +1***51234).
/// </summary>
public class WhatsAppMessageDto
{
    public Guid Id { get; init; }
    public string ToPhoneNumber { get; init; } = string.Empty;
    public string? TemplateName { get; init; }
    public WhatsAppMessageStatus Status { get; init; }
    public WhatsAppMessageType MessageType { get; init; }
    public string Language { get; init; } = "en";
    public DateTime? SentAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
    public Guid? UserId { get; init; }
    public Guid? EventId { get; init; }
}

/// <summary>
/// Phase 7A.2: Handler for GetWhatsAppMessageHistoryQuery.
/// Queries messages by userId or eventId and maps to DTOs with masked phone numbers.
/// </summary>
public class GetWhatsAppMessageHistoryQueryHandler : IRequestHandler<GetWhatsAppMessageHistoryQuery, Result<IReadOnlyList<WhatsAppMessageDto>>>
{
    private readonly IWhatsAppMessageRepository _messageRepository;
    private readonly ILogger<GetWhatsAppMessageHistoryQueryHandler> _logger;

    public GetWhatsAppMessageHistoryQueryHandler(
        IWhatsAppMessageRepository messageRepository,
        ILogger<GetWhatsAppMessageHistoryQueryHandler> logger)
    {
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<WhatsAppMessageDto>>> Handle(
        GetWhatsAppMessageHistoryQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetWhatsAppMessageHistory"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetWhatsAppMessageHistory START: UserId={UserId}, EventId={EventId}, Page={Page}, PageSize={PageSize}",
                request.UserId, request.EventId, request.Page, request.PageSize);

            try
            {
                if (request.UserId is null && request.EventId is null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetWhatsAppMessageHistory FAILED: No filter provided - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<IReadOnlyList<WhatsAppMessageDto>>.Failure(
                        "At least one filter (UserId or EventId) is required.");
                }

                IReadOnlyList<Domain.Communications.Entities.WhatsAppMessageRecord> messages;

                if (request.UserId.HasValue)
                {
                    messages = await _messageRepository.GetByUserIdAsync(request.UserId.Value, cancellationToken);
                }
                else
                {
                    messages = await _messageRepository.GetByEventIdAsync(request.EventId!.Value, cancellationToken);
                }

                // Apply pagination
                var pagedMessages = messages
                    .OrderByDescending(m => m.SentAt ?? m.CreatedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                var dtos = pagedMessages.Select(m => new WhatsAppMessageDto
                {
                    Id = m.Id,
                    ToPhoneNumber = MaskPhoneNumber(m.ToPhoneNumber),
                    TemplateName = m.TemplateName,
                    Status = m.Status,
                    MessageType = m.MessageType,
                    Language = m.Language,
                    SentAt = m.SentAt,
                    DeliveredAt = m.DeliveredAt,
                    ReadAt = m.ReadAt,
                    FailedAt = m.FailedAt,
                    ErrorMessage = m.ErrorMessage,
                    RetryCount = m.RetryCount,
                    UserId = m.UserId,
                    EventId = m.EventId
                }).ToList() as IReadOnlyList<WhatsAppMessageDto>;

                stopwatch.Stop();
                _logger.LogInformation(
                    "GetWhatsAppMessageHistory COMPLETE: UserId={UserId}, EventId={EventId}, ResultCount={ResultCount}, Duration={ElapsedMs}ms",
                    request.UserId, request.EventId, dtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<WhatsAppMessageDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetWhatsAppMessageHistory FAILED: Unexpected error - UserId={UserId}, EventId={EventId}, Duration={ElapsedMs}ms",
                    request.UserId, request.EventId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    /// <summary>
    /// Masks a phone number for privacy, keeping the country code prefix and last 5 digits.
    /// E.g., +14155551234 becomes +1***51234
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 8)
            return "***";

        // Keep first 2 chars (e.g., +1) and last 5 chars, mask the middle
        var prefix = phoneNumber[..2];
        var suffix = phoneNumber[^5..];
        return $"{prefix}***{suffix}";
    }
}
