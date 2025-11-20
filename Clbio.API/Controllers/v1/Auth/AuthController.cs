using Clbio.Abstractions.Interfaces.Services;
using Clbio.API.Extensions;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.DTOs.V1.Auth.External;
using Clbio.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Clbio.API.Controllers.v1.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController(IAuthService authService, IEmailVerificationService emailVerificationService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;
        private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;

        // Helpers
        private string? GetUserAgent() => Request.Headers.UserAgent.ToString();
        private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        // ------------------------------------------------------------
        // POST /api/auth/register
        // ------------------------------------------------------------
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.RegisterAsync(dto, GetUserAgent(), GetIp(), ct);

            if (!result.Success)
                return BadRequest(ApiResponse<TokenResponseDto>.Fail(result.Error!, result.Code));

            return Ok(ApiResponse<TokenResponseDto>.Ok(result.Value));
        }

        // ------------------------------------------------------------
        // POST /api/auth/login
        // ------------------------------------------------------------
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.LoginAsync(dto, GetUserAgent(), GetIp(), ct);

            if (!result.Success)
                return Unauthorized(ApiResponse<TokenResponseDto>.Fail(result.Error!, result.Code));

            return Ok(ApiResponse<TokenResponseDto>.Ok(result.Value));
        }

        // ------------------------------------------------------------
        // POST /api/auth/google
        // ------------------------------------------------------------
        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithGoogle(
            [FromBody] GoogleLoginRequestDto dto,
            CancellationToken ct)
        {
            var result = await _authService.LoginWithGoogleAsync(dto, GetUserAgent(), GetIp(), ct);
            if (!result.Success)
                return Unauthorized(ApiResponse<TokenResponseDto>.Fail(result.Error!, result.Code));

            return Ok(ApiResponse<TokenResponseDto>.Ok(result.Value));
        }

        // ------------------------------------------------------------
        // POST /api/auth/refresh
        // Body: { "refreshToken": "..." }
        // ------------------------------------------------------------
        public sealed class RefreshRequest
        {
            public string RefreshToken { get; set; } = default!;
        }

        [HttpPost("refresh")]
        [AllowAnonymous] // Refresh token is proof of authentication
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return BadRequest(ApiResponse<TokenResponseDto>.Fail("Refresh token is required."));

            var result = await _authService.RefreshAsync(req.RefreshToken, GetUserAgent(), GetIp(), ct);

            if (!result.Success)
                return Unauthorized(ApiResponse<TokenResponseDto>.Fail(result.Error!, result.Code));

            return Ok(ApiResponse<TokenResponseDto>.Ok(result.Value));
        }

        // ------------------------------------------------------------
        // POST /api/auth/logout
        // Requires Authorization header
        // Body: { "refreshToken": "..." }
        // ------------------------------------------------------------
        public sealed class LogoutRequest
        {
            public string RefreshToken { get; set; } = default!;
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return BadRequest(ApiResponse<object>.Fail("Refresh token is required."));

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

            var userId = Guid.Parse(userIdClaim);

            var result = await _authService.LogoutAsync(userId, req.RefreshToken, ct);

            return result.Success
                ? NoContent()
                : BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));
        }

        // ------------------------------------------------------------
        // POST /api/auth/logout-all
        // Invalidates every active refresh token for the user
        // ------------------------------------------------------------
        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll(CancellationToken ct)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid token."));

            var userId = Guid.Parse(userIdClaim);

            var result = await _authService.LogoutAllAsync(userId, ct);

            return result.Success
                ? NoContent()
                : BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));
        }

        // ------------------------------------------------------------
        // GET /api/auth/verify-email?token=token123
        // Verify email by hashing the token from the URL, find the stored record with that hash, check if it not expired or used
        // ------------------------------------------------------------
        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
        {
            var result = await _emailVerificationService.VerifyEmailAsync(token, ct);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));

            return Ok(ApiResponse<object>.Ok("Email verified."));
        }

        // !!! DEV ENDPOINT
        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail([FromServices] IEmailSender sender)
        {
            await sender.SendEmailAsync(
                "akarahmet2002@gmail.com",
                "Hello World!",
                "<h1>Hello</h1><p>This is a test email.</p>"
            );
            return Ok("Sent.");
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto, CancellationToken ct)
        {
            _ = await _authService.ForgotPasswordAsync(dto, GetIp(), ct);

            // Always return OK
            return Ok(ApiResponse<object>.Ok("If an account with that email exists, a reset link has been sent."));
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto, CancellationToken ct)
        {
            var result = await _authService.ResetPasswordAsync(dto, GetIp(), ct);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));

            return Ok(ApiResponse<object>.Ok("Password has been reset successfully."));
        }
    }
}
