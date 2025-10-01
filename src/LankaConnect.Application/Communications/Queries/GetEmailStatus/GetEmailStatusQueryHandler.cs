using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using DomainEmailMessage = LankaConnect.Domain.Communications.Entities.EmailMessage;
using LankaConnect.Domain.Shared.ValueObjects;
using IUserRepository = LankaConnect.Domain.Users.IUserRepository;

namespace LankaConnect.Application.Communications.Queries.GetEmailStatus;

/// <summary>
/// Handler for retrieving email status information
/// </summary>
public class GetEmailStatusQueryHandler : IRequestHandler<GetEmailStatusQuery, Result<GetEmailStatusResponse>>
{
    private readonly IEmailStatusRepository _emailStatusRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetEmailStatusQueryHandler> _logger;

    public GetEmailStatusQueryHandler(
        IEmailStatusRepository emailStatusRepository,
        IUserRepository userRepository,
        ILogger<GetEmailStatusQueryHandler> logger)
    {
        _emailStatusRepository = emailStatusRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<GetEmailStatusResponse>> Handle(GetEmailStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation at the start
            cancellationToken.ThrowIfCancellationRequested();
            
            // Validate user exists if UserId is provided
            if (request.UserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(request.UserId.Value, cancellationToken);
                if (user == null)
                {
                    return Result<GetEmailStatusResponse>.Failure("User not found");
                }
            }

            // Validate date range
            if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate > request.ToDate)
            {
                return Result<GetEmailStatusResponse>.Failure("From date cannot be later than to date");
            }

            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return Result<GetEmailStatusResponse>.Failure("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result<GetEmailStatusResponse>.Failure("Page size must be between 1 and 100");
            }

            // Get email statuses with filters
            var emailStatuses = await _emailStatusRepository.GetEmailStatusAsync(
                request.UserId,
                request.EmailAddress,
                ConvertEmailType(request.EmailType),
                ConvertEmailStatus(request.Status),
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            // Get total count for pagination
            var totalCount = await _emailStatusRepository.GetEmailStatusCountAsync(
                request.UserId,
                request.EmailAddress,
                ConvertEmailType(request.EmailType),
                ConvertEmailStatus(request.Status),
                request.FromDate,
                request.ToDate,
                cancellationToken);

            // Map to DTOs
            var emailStatusDtos = emailStatuses.Select(status => MapToDto(status)).ToList();

            var response = new GetEmailStatusResponse(
                emailStatusDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation("Retrieved {Count} email statuses for query with filters: UserId={UserId}, Type={EmailType}, Status={Status}", 
                emailStatusDtos.Count, request.UserId, request.EmailType, request.Status);

            return Result<GetEmailStatusResponse>.Success(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving email status for query: UserId={UserId}, Type={EmailType}", 
                request.UserId, request.EmailType);
            return Result<GetEmailStatusResponse>.Failure("An error occurred while retrieving email status");
        }
    }

    private static EmailStatusDto MapToDto(DomainEmailMessage emailStatus)
    {
        return new EmailStatusDto
        {
            EmailId = emailStatus.Id,
            ToEmail = emailStatus.ToEmails.FirstOrDefault() ?? string.Empty,
            Subject = emailStatus.Subject.Value,
            Status = (EmailStatus)emailStatus.Status,
            Type = (EmailType)emailStatus.Type,
            CreatedAt = emailStatus.CreatedAt,
            SentAt = emailStatus.SentAt,
            DeliveredAt = emailStatus.DeliveredAt,
            FailedAt = emailStatus.FailedAt,
            FailureReason = emailStatus.ErrorMessage,
            RetryCount = emailStatus.RetryCount,
            NextRetryAt = emailStatus.NextRetryAt
        };
    }

    private static LankaConnect.Domain.Communications.Enums.EmailType? ConvertEmailType(Communications.Common.EmailType? appEmailType)
    {
        if (!appEmailType.HasValue) return null;
        
        return appEmailType.Value switch
        {
            Communications.Common.EmailType.EmailVerification => LankaConnect.Domain.Communications.Enums.EmailType.EmailVerification,
            Communications.Common.EmailType.PasswordReset => LankaConnect.Domain.Communications.Enums.EmailType.PasswordReset,
            Communications.Common.EmailType.Welcome => LankaConnect.Domain.Communications.Enums.EmailType.Welcome,
            Communications.Common.EmailType.BusinessNotification => LankaConnect.Domain.Communications.Enums.EmailType.BusinessNotification,
            Communications.Common.EmailType.SystemAlert => LankaConnect.Domain.Communications.Enums.EmailType.EventNotification,
            Communications.Common.EmailType.Marketing => LankaConnect.Domain.Communications.Enums.EmailType.Marketing,
            _ => LankaConnect.Domain.Communications.Enums.EmailType.Transactional
        };
    }

    private static LankaConnect.Domain.Communications.Enums.EmailStatus? ConvertEmailStatus(Communications.Common.EmailStatus? appEmailStatus)
    {
        if (!appEmailStatus.HasValue) return null;
        
        return appEmailStatus.Value switch
        {
            Communications.Common.EmailStatus.Pending => LankaConnect.Domain.Communications.Enums.EmailStatus.Pending,
            Communications.Common.EmailStatus.Queued => LankaConnect.Domain.Communications.Enums.EmailStatus.Queued,
            Communications.Common.EmailStatus.Sending => LankaConnect.Domain.Communications.Enums.EmailStatus.Sending,
            Communications.Common.EmailStatus.Sent => LankaConnect.Domain.Communications.Enums.EmailStatus.Sent,
            Communications.Common.EmailStatus.Delivered => LankaConnect.Domain.Communications.Enums.EmailStatus.Delivered,
            Communications.Common.EmailStatus.Failed => LankaConnect.Domain.Communications.Enums.EmailStatus.Failed,
            Communications.Common.EmailStatus.Bounced => LankaConnect.Domain.Communications.Enums.EmailStatus.Bounced,
            Communications.Common.EmailStatus.Cancelled => LankaConnect.Domain.Communications.Enums.EmailStatus.Rejected,
            _ => LankaConnect.Domain.Communications.Enums.EmailStatus.Failed
        };
    }
}