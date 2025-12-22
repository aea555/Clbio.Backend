using Clbio.Abstractions.Interfaces.Services;
using Clbio.API.Extensions;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.DTOs.V1.Auth.External;
using Clbio.Application.DTOs.V1.User;
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

            return Ok(ApiResponse<ReadUserDto>.Ok(result.Value));
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

            var userIdClaim = User.FindFirst("sub")?.Value 
               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(ApiResponse<object>.Fail("Invalid token claims."));

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
        // POST /api/auth/verify-email
        // ------------------------------------------------------------
        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailOtpRequestDto dto, CancellationToken ct)
        {
            var result = await _emailVerificationService.VerifyEmailOtpAsync(dto.Email, dto.Code, GetUserAgent(), GetIp(), ct);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));

            return Ok(ApiResponse<object>.Ok("Email verified successfully. You can log into your account now."));
        }

        // ------------------------------------------------------------
        // POST /api/auth/resend-verification-otp
        // ------------------------------------------------------------
        [HttpPost("resend-verification-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerificationOtp([FromBody] ResendEmailVerificationDto dto, CancellationToken ct)
        {
            var result = await _emailVerificationService.ResendVerificationOtpEmailAsync(dto.Email, ct);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));

            return Ok(ApiResponse<object>.Ok("If an account with that email exists and is not already verified, a verification code has been resent successfully."));
        }

        // !!! DEV ENDPOINT
        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail([FromServices] IEmailSender sender)
        {
            return Forbid();
            // await sender.SendEmailAsync(
            //     "akarahmet2002@gmail.com",
            //     "Hello World!",
            //     "<h1>Hello</h1><p>This is a test email.</p>"
            // );
            // return Ok("Sent.");
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
            return Forbid();
        }

        [HttpPost("reset-password-with-token")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordWithToken([FromBody] ResetPasswordWithTokenDto dto, CancellationToken ct)
        {
            var result = await _authService.ResetPasswordWithTokenAsync(dto.Token, dto.NewPassword, GetIp()!, ct);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));
            
            return Ok(ApiResponse.Ok("Password reset successful."));
        }

        [HttpPost("reset-password-validate-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordValidateOtp([FromBody] ValidateOtpOnlyDto dto, CancellationToken ct)
        {
            var result = await _authService.ValidateOtpOnlyAsync(dto.Email, dto.Code, ct);

            if (!result.Success)
                return BadRequest(ApiResponse<object>.Fail(result.Error!, result.Code));

            return Ok(ApiResponse<string>.Ok(result.Value!));
        }
    }
}
