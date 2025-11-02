using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Commands.LinkExternalProvider;

/// <summary>
/// Handler for LinkExternalProviderCommand
/// Epic 1 Phase 2: Multi-Provider Social Login
/// Allows users to link additional OAuth providers to their existing account
/// </summary>
public class LinkExternalProviderHandler : IRequestHandler<LinkExternalProviderCommand, Result<LinkExternalProviderResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LinkExternalProviderHandler> _logger;

    public LinkExternalProviderHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<LinkExternalProviderHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<LinkExternalProviderResponse>> Handle(
        LinkExternalProviderCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Linking external provider {Provider} for user {UserId}",
            request.Provider.ToDisplayName(), request.UserId);

        // 1. Find user by ID
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return Result<LinkExternalProviderResponse>.Failure("User not found");
        }

        // 2. Link external provider using domain logic
        var linkResult = user.LinkExternalProvider(
            request.Provider,
            request.ExternalProviderId,
            request.ProviderEmail);

        if (linkResult.IsFailure)
        {
            _logger.LogWarning("Failed to link {Provider} for user {UserId}: {Error}",
                request.Provider.ToDisplayName(), request.UserId, linkResult.Error);
            return Result<LinkExternalProviderResponse>.Failure(linkResult.Error);
        }

        // 3. Save changes
        var saveResult = await _unitOfWork.CommitAsync(cancellationToken);
        if (saveResult <= 0)
        {
            _logger.LogError("Failed to save external provider link for user {UserId}", request.UserId);
            return Result<LinkExternalProviderResponse>.Failure("Failed to save external provider link");
        }

        _logger.LogInformation("Successfully linked {Provider} for user {UserId}",
            request.Provider.ToDisplayName(), request.UserId);

        // 4. Return success response
        var response = new LinkExternalProviderResponse(
            request.UserId,
            request.Provider,
            request.ProviderEmail,
            DateTime.UtcNow);

        return Result<LinkExternalProviderResponse>.Success(response);
    }
}
