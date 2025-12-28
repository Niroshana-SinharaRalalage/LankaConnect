using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.VerifyEmail;

/// <summary>
/// Handler for verifying user email addresses
/// </summary>
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<VerifyEmailResponse>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check for cancellation at the start
            cancellationToken.ThrowIfCancellationRequested();
            
            // Get user
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<VerifyEmailResponse>.Failure("User not found");
            }

            // Check if already verified
            if (user.IsEmailVerified)
            {
                var alreadyVerifiedResponse = new VerifyEmailResponse(
                    user.Id,
                    user.Email.Value,
                    DateTime.UtcNow,
                    wasAlreadyVerified: true);

                return Result<VerifyEmailResponse>.Success(alreadyVerifiedResponse);
            }

            // Phase 6A.53: Verify email with token validation (moved into VerifyEmail method)
            var verifyResult = user.VerifyEmail(request.Token);
            if (!verifyResult.IsSuccess)
            {
                _logger.LogWarning("Email verification failed for user {UserId}: {Error}", request.UserId, verifyResult.Error);
                return Result<VerifyEmailResponse>.Failure(verifyResult.Error);
            }

            // Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Email verified successfully for user {UserId}: {Email}", 
                user.Id, user.Email.Value);

            // Send welcome email asynchronously (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var templateParameters = new Dictionary<string, object>
                    {
                        { "UserName", user.FullName },
                        { "UserEmail", user.Email.Value },
                        { "CompanyName", "LankaConnect" },
                        { "LoginUrl", "https://lankaconnect.com/login" }
                    };

                    await _emailService.SendTemplatedEmailAsync(
                        "welcome-email",
                        user.Email.Value,
                        templateParameters,
                        CancellationToken.None);

                    _logger.LogInformation("Welcome email sent to verified user {UserId}", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send welcome email to verified user {UserId}", user.Id);
                }
            }, cancellationToken);

            var response = new VerifyEmailResponse(
                user.Id,
                user.Email.Value,
                DateTime.UtcNow);

            return Result<VerifyEmailResponse>.Success(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email for user {UserId}", request.UserId);
            return Result<VerifyEmailResponse>.Failure("An error occurred while verifying email");
        }
    }
}