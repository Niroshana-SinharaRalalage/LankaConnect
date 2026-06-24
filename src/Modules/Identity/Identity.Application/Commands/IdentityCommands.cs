using System.Security.Cryptography;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Modules.Identity.Contracts;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Modules.Identity.Application.Commands;

/// <summary>
/// Implementation of <see cref="IIdentityCommands"/>. Wave 4.6.d.1 (2026-06-24).
/// Architect-mandated semantic mutators per Risk #3 Option A ruling: this adapter
/// owns password hashing + token lifetime + 5-minute resend throttle + refresh-token
/// revocation. Callers (Communications.SendPasswordReset + ResetPassword handlers)
/// only orchestrate email side-effects.
/// </summary>
public sealed class IdentityCommands : IIdentityCommands
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IdentityCommands> _logger;

    public IdentityCommands(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IUnitOfWork unitOfWork,
        ILogger<IdentityCommands> logger)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PasswordResetInitiatedDto?> InitiatePasswordResetAsync(
        string email,
        TimeSpan tokenLifetime,
        bool forceResend,
        CancellationToken ct = default)
    {
        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            _logger.LogWarning("InitiatePasswordReset: invalid email format {Email}", email);
            return null; // Communications handler returns generic "we have sent if exists" to avoid enumeration leak
        }

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, ct);
        if (user is null)
        {
            _logger.LogInformation("InitiatePasswordReset: user not found for {Email}", email);
            return null;
        }

        // Architect-specified 5-min throttle: derive request time as TokenExpiresAt.AddHours(-1)
        // (per Architect Additional Finding #2 -- the User aggregate does NOT have a
        // PasswordResetRequestedAt field; the 1-hour token lifetime is fixed).
        var wasThrottled = false;
        string token;
        DateTime expiresAt;
        if (!forceResend
            && !string.IsNullOrEmpty(user.PasswordResetToken)
            && user.PasswordResetTokenExpiresAt is { } existingExpiry
            && existingExpiry > DateTime.UtcNow
            && existingExpiry.AddHours(-1).AddMinutes(5) > DateTime.UtcNow)
        {
            // Re-use existing token (within 5 minutes of original request)
            token = user.PasswordResetToken!;
            expiresAt = existingExpiry;
            wasThrottled = true;
            _logger.LogInformation("InitiatePasswordReset: re-using existing token (throttled) for UserId={UserId}", user.Id);
        }
        else
        {
            token = GenerateSecureToken();
            expiresAt = DateTime.UtcNow.Add(tokenLifetime);
            var setResult = user.SetPasswordResetToken(token, expiresAt);
            if (setResult.IsFailure)
            {
                _logger.LogWarning("InitiatePasswordReset: SetPasswordResetToken failed for UserId={UserId}: {Error}", user.Id, setResult.Error);
                return null;
            }
            _userRepository.Update(user);
            await _unitOfWork.CommitAsync(ct);
        }

        return new PasswordResetInitiatedDto(
            UserId: user.Id,
            Email: user.Email.Value,
            DisplayName: user.FullName,
            PasswordResetToken: token,
            TokenExpiresAt: expiresAt,
            WasThrottled: wasThrottled);
    }

    public async Task<PasswordResetCompletedDto> CompletePasswordResetAsync(
        string token,
        string? emailFallback,
        string newPlaintextPassword,
        CancellationToken ct = default)
    {
        // Lookup by token (primary) or by email fallback (post-expiry recovery flow)
        User? user = await _userRepository.GetByPasswordResetTokenAsync(token, ct);
        if (user is null && !string.IsNullOrWhiteSpace(emailFallback))
        {
            var emailResult = Email.Create(emailFallback);
            if (emailResult.IsSuccess)
            {
                user = await _userRepository.GetByEmailAsync(emailResult.Value, ct);
            }
        }

        if (user is null)
        {
            throw new InvalidOperationException("Invalid or expired reset token");
        }

        if (!user.IsPasswordResetTokenValid(token))
        {
            throw new InvalidOperationException("Invalid or expired reset token");
        }

        // Architect ruling: Identity adapter owns hashing + validation.
        var passwordValidation = _passwordHashingService.ValidatePasswordStrength(newPlaintextPassword);
        if (passwordValidation.IsFailure)
        {
            throw new InvalidOperationException($"Password does not meet strength requirements: {passwordValidation.Error}");
        }

        var hashResult = _passwordHashingService.HashPassword(newPlaintextPassword);
        if (hashResult.IsFailure)
        {
            throw new InvalidOperationException($"Password hashing failed: {hashResult.Error}");
        }

        var setResult = user.SetPassword(hashResult.Value);
        if (setResult.IsFailure)
        {
            throw new InvalidOperationException($"SetPassword failed: {setResult.Error}");
        }

        // Security policy: password change invalidates all active sessions.
        user.RevokeAllRefreshTokens();

        _userRepository.Update(user);
        await _unitOfWork.CommitAsync(ct);

        _logger.LogInformation("CompletePasswordReset: password reset for UserId={UserId}", user.Id);

        return new PasswordResetCompletedDto(
            UserId: user.Id,
            Email: user.Email.Value,
            DisplayName: user.FullName);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
