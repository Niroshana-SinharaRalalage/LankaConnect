using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Commands.UnlinkExternalProvider;

/// <summary>
/// Handler for UnlinkExternalProviderCommand
/// Epic 1 Phase 2: Multi-Provider Social Login
/// Enforces business rule: Users must maintain at least one authentication method
/// </summary>
public class UnlinkExternalProviderHandler : IRequestHandler<UnlinkExternalProviderCommand, Result<UnlinkExternalProviderResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnlinkExternalProviderHandler> _logger;

    public UnlinkExternalProviderHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UnlinkExternalProviderHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<UnlinkExternalProviderResponse>> Handle(
        UnlinkExternalProviderCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unlinking external provider {Provider} for user {UserId}",
            request.Provider.ToDisplayName(), request.UserId);

        // 1. Find user by ID
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return Result<UnlinkExternalProviderResponse>.Failure("User not found");
        }

        // 2. Unlink external provider using domain logic (enforces business rules)
        var unlinkResult = user.UnlinkExternalProvider(request.Provider);

        if (unlinkResult.IsFailure)
        {
            _logger.LogWarning("Failed to unlink {Provider} for user {UserId}: {Error}",
                request.Provider.ToDisplayName(), request.UserId, unlinkResult.Error);
            return Result<UnlinkExternalProviderResponse>.Failure(unlinkResult.Error);
        }

        // 3. Save changes
        var saveResult = await _unitOfWork.CommitAsync(cancellationToken);
        if (saveResult <= 0)
        {
            _logger.LogError("Failed to save external provider unlink for user {UserId}", request.UserId);
            return Result<UnlinkExternalProviderResponse>.Failure("Failed to save external provider unlink");
        }

        _logger.LogInformation("Successfully unlinked {Provider} for user {UserId}",
            request.Provider.ToDisplayName(), request.UserId);

        // 4. Return success response
        var response = new UnlinkExternalProviderResponse(
            request.UserId,
            request.Provider,
            DateTime.UtcNow);

        return Result<UnlinkExternalProviderResponse>.Success(response);
    }
}
