using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Security.Claims;

namespace LankaConnect.API.Security;

/// <summary>
/// Diagnostic authorization handler that logs authorization attempts for debugging
/// Phase 6A.10: Added to diagnose authorization policy failures
/// </summary>
public class LoggingAuthorizationHandler : IAuthorizationHandler
{
    private readonly ILogger<LoggingAuthorizationHandler> _logger;

    public LoggingAuthorizationHandler(ILogger<LoggingAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var user = context.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ANONYMOUS";
        var userEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? "NO EMAIL";
        var roleClaim = user.FindFirst(ClaimTypes.Role);

        _logger.LogInformation(
            "Authorization evaluation - UserId: {UserId}, Email: {Email}, Role: {Role}, IsAuthenticated: {IsAuthenticated}",
            userId,
            userEmail,
            roleClaim?.Value ?? "NO ROLE CLAIM",
            user.Identity?.IsAuthenticated ?? false);

        // Log all claims for debugging
        _logger.LogInformation("User claims: {Claims}",
            string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}")));

        // Log all requirements being checked
        foreach (var requirement in context.Requirements)
        {
            var requirementType = requirement.GetType().Name;
            _logger.LogInformation(
                "Authorization requirement: {RequirementType} - {Requirement}",
                requirementType,
                requirement.ToString());

            // Log specific details for role requirements
            if (requirement is RolesAuthorizationRequirement rolesRequirement)
            {
                _logger.LogInformation(
                    "Required roles: {RequiredRoles}, User has role: {UserRole}",
                    string.Join(", ", rolesRequirement.AllowedRoles),
                    roleClaim?.Value ?? "NO ROLE");
            }
        }

        // Log pending requirements (not yet satisfied)
        if (context.PendingRequirements.Any())
        {
            _logger.LogWarning(
                "Pending (unsatisfied) requirements: {PendingRequirements}",
                string.Join(", ", context.PendingRequirements.Select(r => r.GetType().Name)));
        }

        // Don't actually authorize - just observe
        return Task.CompletedTask;
    }
}
