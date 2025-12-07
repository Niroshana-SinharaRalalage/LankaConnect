using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using System.Security.Claims;
using LankaConnect.Application.Auth.Commands.RegisterUser;
using LankaConnect.Application.Auth.Commands.LoginUser;
using LankaConnect.Application.Auth.Commands.RefreshToken;
using LankaConnect.Application.Auth.Commands.LogoutUser;
using LankaConnect.Application.Auth.Commands.LoginWithEntra;
using LankaConnect.Application.Communications.Commands.SendPasswordReset;
using LankaConnect.Application.Communications.Commands.ResetPassword;
using LankaConnect.Application.Communications.Commands.VerifyEmail;
using LankaConnect.Application.Communications.Commands.SendEmailVerification;
using LankaConnect.API.Filters;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Common;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Authentication endpoints for user registration, login, and token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;

    public AuthController(
        IMediator mediator,
        ILogger<AuthController> logger,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment env)
    {
        _mediator = mediator;
        _logger = logger;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _env = env;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result with user details</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            return CreatedAtAction(nameof(Register), new { id = result.Value.UserId }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
            return StatusCode(500, new { error = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Authenticate user and return JWT tokens
    /// </summary>
    /// <param name="request">User login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication tokens and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get client IP address for security tracking
            var ipAddress = GetClientIpAddress();
            var loginRequest = request with { IpAddress = ipAddress };

            var result = await _mediator.Send(loginRequest, cancellationToken);

            if (!result.IsSuccess)
            {
                // Check for specific error types
                if (result.Error.Contains("locked"))
                {
                    return StatusCode(423, new { error = result.Error });
                }
                
                if (result.Error.Contains("not active") || result.Error.Contains("not verified"))
                {
                    return Unauthorized(new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("User logged in successfully: {Email} (RememberMe: {RememberMe})",
                request.Email, request.RememberMe);

            // Set refresh token as HttpOnly cookie for security
            // Cookie expiration matches refresh token expiration (7 or 30 days)
            var cookieDays = request.RememberMe ? 30 : 7;
            SetRefreshTokenCookie(result.Value.RefreshToken, cookieDays);

            // Phase 6A.10: In development, include refresh token in response body for localStorage mode
            // In production, refresh token is in HttpOnly cookie (sent by browser automatically)
            var response = new
            {
                user = new
                {
                    result.Value.UserId,
                    result.Value.Email,
                    result.Value.FullName,
                    result.Value.Role,
                    result.Value.PendingUpgradeRole, // Phase 6A.7: Include pending upgrade role for UI display
                    result.Value.UpgradeRequestedAt,  // Phase 6A.7: Include when upgrade was requested
                    result.Value.ProfilePhotoUrl      // Include profile photo URL for header display
                },
                result.Value.AccessToken,
                result.Value.TokenExpiresAt
            };

            // Development: Include refresh token in response body for localStorage storage
            if (_env.IsDevelopment())
            {
                _logger.LogInformation("[Phase 6A.10] Including refreshToken in response body for development mode");
                return Ok(new
                {
                    user = response.user,
                    response.AccessToken,
                    RefreshToken = result.Value.RefreshToken,
                    response.TokenExpiresAt
                });
            }

            // Production: Only access token in response (refresh token in HttpOnly cookie)
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new { error = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access and refresh tokens</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new { error = "Refresh token is required" });
            }

            var ipAddress = GetClientIpAddress();
            var request = new RefreshTokenCommand(refreshToken, ipAddress);

            var result = await _mediator.Send(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return Unauthorized(new { error = result.Error });
            }

            // Set new refresh token as HttpOnly cookie
            SetRefreshTokenCookie(result.Value.RefreshToken);

            // Phase 6A.10: In development, include refresh token in response body for localStorage mode
            if (_env.IsDevelopment())
            {
                _logger.LogInformation("[Phase 6A.10] Including refreshToken in refresh response for development mode");
                return Ok(new
                {
                    result.Value.AccessToken,
                    RefreshToken = result.Value.RefreshToken,
                    result.Value.TokenExpiresAt
                });
            }

            // Production: Only access token in response (refresh token in HttpOnly cookie)
            return Ok(new
            {
                result.Value.AccessToken,
                result.Value.TokenExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { error = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Logout user and invalidate refresh token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new { error = "Refresh token is required" });
            }

            var ipAddress = GetClientIpAddress();
            var request = new LogoutUserCommand(refreshToken, ipAddress);

            var result = await _mediator.Send(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            // Clear refresh token cookie
            ClearRefreshTokenCookie();

            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            _logger.LogInformation("User logged out successfully: {Email}", userEmail);

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { error = "An error occurred during logout" });
        }
    }

    /// <summary>
    /// Get current authenticated user profile
    /// </summary>
    /// <returns>User profile information</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var firstName = User.FindFirst("firstName")?.Value;
            var lastName = User.FindFirst("lastName")?.Value;
            var isActive = User.FindFirst("isActive")?.Value;

            return Ok(new
            {
                userId,
                email,
                name,
                firstName,
                lastName,
                isActive = bool.TryParse(isActive, out var active) && active
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return StatusCode(500, new { error = "An error occurred getting user profile" });
        }
    }

    /// <summary>
    /// Authenticate user with Microsoft Entra External ID
    /// </summary>
    /// <param name="request">Entra access token and IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication tokens and user information</returns>
    [HttpPost("login/entra")]
    [ProducesResponseType(typeof(LoginWithEntraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginWithEntra([FromBody] LoginWithEntraCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get client IP address for security tracking
            var ipAddress = GetClientIpAddress();
            var loginRequest = request with { IpAddress = ipAddress };

            var result = await _mediator.Send(loginRequest, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Entra login failed: {Errors}", string.Join(", ", result.Errors));
                return Unauthorized(new { error = result.Error });
            }

            _logger.LogInformation("User logged in with Entra External ID: {Email} (IsNewUser: {IsNewUser})",
                result.Value.Email, result.Value.IsNewUser);

            // Set refresh token as HttpOnly cookie for security
            SetRefreshTokenCookie(result.Value.RefreshToken);

            // Phase 6A.10: In development, include refresh token in response body for localStorage mode
            var entraResponse = new
            {
                user = new
                {
                    result.Value.UserId,
                    result.Value.Email,
                    result.Value.FullName,
                    result.Value.Role,
                    result.Value.IsNewUser
                },
                result.Value.AccessToken,
                result.Value.TokenExpiresAt
            };

            if (_env.IsDevelopment())
            {
                _logger.LogInformation("[Phase 6A.10] Including refreshToken in Entra response for development mode");
                return Ok(new
                {
                    entraResponse.user,
                    entraResponse.AccessToken,
                    RefreshToken = result.Value.RefreshToken,
                    entraResponse.TokenExpiresAt
                });
            }

            // Production: Only access token in response (refresh token in HttpOnly cookie)
            return Ok(entraResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Entra login");
            return StatusCode(500, new { error = "An error occurred during Entra login" });
        }
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    /// <param name="request">Password reset request with email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success confirmation (always returns success for security)</returns>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] SendPasswordResetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);

            // Always return success for security (don't reveal if email exists)
            // The handler internally logs failed attempts
            if (!result.IsSuccess && !result.Value.UserNotFound)
            {
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

            return Ok(new
            {
                message = "If an account with that email exists, a password reset link has been sent.",
                email = request.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset request for email: {Email}", request.Email);
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    /// <param name="request">Password reset details with token and new password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset confirmation</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Password reset successfully for user: {UserId}", result.Value.UserId);

            return Ok(new
            {
                message = "Password has been reset successfully. Please login with your new password.",
                userId = result.Value.UserId,
                email = result.Value.Email,
                requiresLogin = result.Value.RequiresLogin
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for email: {Email}", request.Email);
            return StatusCode(500, new { error = "An error occurred while resetting your password" });
        }
    }

    /// <summary>
    /// Verify email address using verification token
    /// </summary>
    /// <param name="request">Email verification request with user ID and token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email verification confirmation</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Email verified successfully for user: {UserId}", result.Value.UserId);

            return Ok(new
            {
                message = result.Value.WasAlreadyVerified
                    ? "Email was already verified."
                    : "Email has been verified successfully. You can now login.",
                userId = result.Value.UserId,
                email = result.Value.Email,
                verifiedAt = result.Value.VerifiedAt,
                wasAlreadyVerified = result.Value.WasAlreadyVerified
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification for user: {UserId}", request.UserId);
            return StatusCode(500, new { error = "An error occurred while verifying your email" });
        }
    }

    /// <summary>
    /// Resend email verification link
    /// </summary>
    /// <param name="request">User ID for resending verification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation that email was sent</returns>
    [HttpPost("resend-verification")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);

            if (!result.IsSuccess)
            {
                // Check for rate limiting
                if (result.Error.Contains("recently sent") || result.Error.Contains("rate limit"))
                {
                    return StatusCode(429, new { error = result.Error });
                }

                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Verification email resent for user: {UserId}", result.Value.UserId);

            return Ok(new
            {
                message = result.Value.WasRecentlySent
                    ? "A verification email was recently sent. Please check your inbox."
                    : "Verification email has been sent. Please check your inbox.",
                userId = result.Value.UserId,
                email = result.Value.Email,
                expiresAt = result.Value.TokenExpiresAt,
                wasRecentlySent = result.Value.WasRecentlySent
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email for user: {UserId}", request.UserId);
            return StatusCode(500, new { error = "An error occurred while sending the verification email" });
        }
    }

    /// <summary>
    /// [TEST ONLY] Verify user email without token validation
    /// Only available in Development environment for E2E testing
    /// </summary>
    /// <param name="userId">User ID to verify email for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    [HttpPost("test/verify-user/{userId}")]
    [ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger in production
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestVerifyUser(Guid userId, CancellationToken cancellationToken)
    {
        // IMPORTANT: Only available in non-production environments for E2E testing
        if (_env.IsProduction())
        {
            _logger.LogWarning("Unauthorized attempt to access test endpoint in {Environment}", _env.EnvironmentName);
            return Forbid();
        }

        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Test verify user failed: user {UserId} not found", userId);
                return NotFound(new { error = "User not found" });
            }

            // Verify email without token validation (test-only)
            var result = user.VerifyEmail();
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Test verify user failed: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Test verified email for user: {UserId}", userId);
            return Ok(new
            {
                message = "Email verified successfully (test mode)",
                userId = user.Id,
                email = user.Email.Value,
                verifiedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test verify email endpoint for user: {UserId}", userId);
            return StatusCode(500, new { error = "An error occurred during verification" });
        }
    }

    /// <summary>
    /// Health check endpoint for authentication service
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            service = "Authentication",
            status = "Healthy",
            timestamp = DateTime.UtcNow
        });
    }

    #region Private Helper Methods

    private string GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        // Check for forwarded IP addresses (when behind a proxy/load balancer)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedIps = Request.Headers["X-Forwarded-For"].ToString().Split(',');
            if (forwardedIps.Length > 0)
            {
                ipAddress = forwardedIps[0].Trim();
            }
        }
        else if (Request.Headers.ContainsKey("X-Real-IP"))
        {
            ipAddress = Request.Headers["X-Real-IP"].ToString();
        }

        return ipAddress ?? "unknown";
    }

    private void SetRefreshTokenCookie(string refreshToken, int expirationDays = 7)
    {
        // Determine cookie security based on actual connection type, not just environment
        // Azure Container Apps (staging/prod) always use HTTPS
        // Only local development with HTTP should use Secure=false
        var isHttpOnly = _env.IsDevelopment() && !Request.IsHttps;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            // Secure=true for all HTTPS connections (staging, prod, and local HTTPS)
            // Secure=false only for local development over HTTP
            Secure = !isHttpOnly,
            // SameSite=Lax for same-origin (local dev), None for cross-origin (requires Secure=true)
            SameSite = isHttpOnly ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(expirationDays),
            Path = "/",
            // Domain only in production for subdomain sharing
            Domain = _env.IsProduction() ? ".lankaconnect.com" : null
        };

        _logger.LogDebug(
            "Setting refresh token cookie: Secure={Secure}, SameSite={SameSite}, " +
            "Expires={Expires}, Environment={Environment}, IsHttps={IsHttps}, Path={Path}",
            cookieOptions.Secure, cookieOptions.SameSite, cookieOptions.Expires,
            _env.EnvironmentName, Request.IsHttps, cookieOptions.Path);

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private void ClearRefreshTokenCookie()
    {
        // Use same security settings as SetRefreshTokenCookie
        var isHttpOnly = _env.IsDevelopment() && !Request.IsHttps;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isHttpOnly,
            SameSite = isHttpOnly ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(-1),
            Path = "/",
            Domain = _env.IsProduction() ? ".lankaconnect.com" : null
        };

        _logger.LogDebug(
            "Clearing refresh token cookie: Secure={Secure}, SameSite={SameSite}, Environment={Environment}, IsHttps={IsHttps}",
            cookieOptions.Secure, cookieOptions.SameSite, _env.EnvironmentName, Request.IsHttps);

        Response.Cookies.Append("refreshToken", "", cookieOptions);
    }

    #endregion
}