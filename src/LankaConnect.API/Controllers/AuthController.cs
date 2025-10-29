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
using LankaConnect.API.Filters;

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

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
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

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            
            // Set refresh token as HttpOnly cookie for security
            SetRefreshTokenCookie(result.Value.RefreshToken);
            
            return Ok(new
            {
                user = new
                {
                    result.Value.UserId,
                    result.Value.Email,
                    result.Value.FullName,
                    result.Value.Role
                },
                result.Value.AccessToken,
                result.Value.TokenExpiresAt
            });
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

            return Ok(new
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
            });
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

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Use HTTPS in production
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7), // Should match refresh token expiry
            Path = "/"
        };

        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private void ClearRefreshTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(-1),
            Path = "/"
        };

        Response.Cookies.Append("refreshToken", "", cookieOptions);
    }

    #endregion
}