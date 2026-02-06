using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.CreateEmailGroup;

/// <summary>
/// Handler for CreateEmailGroupCommand
/// Phase 6A.25: Creates a new email group for the current user
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class CreateEmailGroupCommandHandler : IRequestHandler<CreateEmailGroupCommand, Result<EmailGroupDto>>
{
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateEmailGroupCommandHandler> _logger;

    public CreateEmailGroupCommandHandler(
        IEmailGroupRepository emailGroupRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CreateEmailGroupCommandHandler> logger)
    {
        _emailGroupRepository = emailGroupRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<EmailGroupDto>> Handle(CreateEmailGroupCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateEmailGroup"))
        using (LogContext.PushProperty("EntityType", "EmailGroup"))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        using (LogContext.PushProperty("GroupName", request.Name))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateEmailGroup START: User={UserId}, Name={Name}",
                _currentUserService.UserId,
                request.Name);

            try
            {
                // Validation: User is authenticated
                var userId = _currentUserService.UserId;
                if (userId == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateEmailGroup FAILED: User not authenticated - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailGroupDto>.Failure("User must be authenticated to create an email group");
                }

                // Check for duplicate name for this owner
                _logger.LogInformation(
                    "CreateEmailGroup: Checking for duplicate name - Name={Name}, UserId={UserId}",
                    request.Name,
                    userId);

                var nameExists = await _emailGroupRepository.NameExistsForOwnerAsync(userId, request.Name, null, cancellationToken);
                if (nameExists)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateEmailGroup FAILED: Duplicate name - Name={Name}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.Name,
                        userId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailGroupDto>.Failure($"An email group with the name '{request.Name}' already exists");
                }

                // Create email group via domain factory method (validates emails)
                _logger.LogInformation(
                    "CreateEmailGroup: Creating email group aggregate - Name={Name}",
                    request.Name);

                var createResult = EmailGroup.Create(
                    request.Name,
                    userId,
                    request.EmailAddresses,
                    request.Description);

                if (!createResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateEmailGroup FAILED: Email group creation failed - Errors={Errors}, Duration={ElapsedMs}ms",
                        string.Join("; ", createResult.Errors),
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailGroupDto>.Failure(createResult.Errors);
                }

                var emailGroup = createResult.Value;

                using (LogContext.PushProperty("EmailGroupId", emailGroup.Id))
                {
                    _logger.LogInformation(
                        "CreateEmailGroup: Email group aggregate created - EmailGroupId={EmailGroupId}, EmailCount={EmailCount}",
                        emailGroup.Id,
                        emailGroup.GetEmailCount());

                    // Save to repository
                    await _emailGroupRepository.AddAsync(emailGroup, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    // Get owner name for DTO
                    var owner = await _userRepository.GetByIdAsync(userId, cancellationToken);
                    var ownerName = owner?.FullName ?? "Unknown";

                    _logger.LogInformation(
                        "CreateEmailGroup: Owner information retrieved - OwnerName={OwnerName}",
                        ownerName);

                    // Return DTO
                    var dto = new EmailGroupDto
                    {
                        Id = emailGroup.Id,
                        Name = emailGroup.Name,
                        Description = emailGroup.Description,
                        OwnerId = emailGroup.OwnerId,
                        OwnerName = ownerName,
                        EmailAddresses = emailGroup.EmailAddresses,
                        EmailCount = emailGroup.GetEmailCount(),
                        IsActive = emailGroup.IsActive,
                        CreatedAt = emailGroup.CreatedAt,
                        UpdatedAt = emailGroup.UpdatedAt
                    };

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "CreateEmailGroup COMPLETE: EmailGroupId={EmailGroupId}, User={UserId}, Name={Name}, EmailCount={EmailCount}, Duration={ElapsedMs}ms",
                        emailGroup.Id,
                        userId,
                        emailGroup.Name,
                        emailGroup.GetEmailCount(),
                        stopwatch.ElapsedMilliseconds);

                    return Result<EmailGroupDto>.Success(dto);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "CreateEmailGroup FAILED: Unexpected error - User={UserId}, Name={Name}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    _currentUserService.UserId,
                    request.Name,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}
