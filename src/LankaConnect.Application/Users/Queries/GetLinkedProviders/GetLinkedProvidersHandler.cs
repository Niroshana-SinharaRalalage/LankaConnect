using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Users.Queries.GetLinkedProviders;

/// <summary>
/// Handler for GetLinkedProvidersQuery
/// Epic 1 Phase 2: Multi-Provider Social Login
/// Returns all external providers linked to a user's account
/// </summary>
public class GetLinkedProvidersHandler : IRequestHandler<GetLinkedProvidersQuery, Result<GetLinkedProvidersResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetLinkedProvidersHandler> _logger;

    public GetLinkedProvidersHandler(
        IUserRepository userRepository,
        ILogger<GetLinkedProvidersHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<GetLinkedProvidersResponse>> Handle(
        GetLinkedProvidersQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving linked providers for user {UserId}", request.UserId);

        // 1. Find user by ID
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return Result<GetLinkedProvidersResponse>.Failure("User not found");
        }

        // 2. Map external logins to DTOs
        var linkedProviders = user.ExternalLogins
            .Select(login => new LinkedProviderDto(
                login.Provider,
                login.Provider.ToDisplayName(),
                login.ExternalProviderId,
                login.ProviderEmail,
                login.LinkedAt))
            .ToList();

        _logger.LogInformation("Found {Count} linked providers for user {UserId}",
            linkedProviders.Count, request.UserId);

        // 3. Return response
        var response = new GetLinkedProvidersResponse(
            request.UserId,
            linkedProviders);

        return Result<GetLinkedProvidersResponse>.Success(response);
    }
}
