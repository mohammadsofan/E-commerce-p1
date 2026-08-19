using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var now = DateTimeOffset.UtcNow;
            var user = new ApplicationUser
            {
                UserName = req.Email,
                Email = req.Email,
                FirstName = string.Empty,
                LastName = string.Empty,
                DisplayName = string.Empty,
                ProfileImageUrl = string.Empty,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            var res = await _userManager.CreateAsync(user, req.Password);
            if (!res.Succeeded) return BadRequest(res.Errors);
            
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _userManager.AddToRoleAsync(user,"Customer");
            // Send verification email
            var verifyUrl = $"{Request.Scheme}://{Request.Host}/api/account/verify-email?token={Uri.EscapeDataString(emailToken)}&email={Uri.EscapeDataString(user.Email)}";
            var message = BuildVerificationEmail(user.Email, verifyUrl);
            await _emailService.SendAsync(message);

            return Ok(new { message = "Registration successful. Verification email sent." });
        }

[HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null) return Unauthorized();

            if (!user.IsEmailVerified)
                return Unauthorized("Email not verified. Please verify your email before logging in.");

            if (!user.IsActive)
                return Unauthorized("Account is deactivated. Please contact support.");

            var res = await _signInManager.CheckPasswordSignInAsync(user, req.Password, false);
            if (!res.Succeeded) return Unauthorized();

            user.LastLoginAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            return Ok(await IssueTokensAsync(user));
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("Token and email are required.");

            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null) return BadRequest("Invalid request.");

            var result = await _userManager.ConfirmEmailAsync(user, req.Token);
            if (!result.Succeeded) return BadRequest(result.Errors);

            user.IsEmailVerified = true;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Email verified successfully." });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmailGet([FromQuery] string token, [FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
                return BadRequest("Token and email are required.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return BadRequest("Invalid request.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded) return BadRequest(result.Errors);

            user.IsEmailVerified = true;
            await _userManager.UpdateAsync(user);

            return Content("<html><body><h2>Email verified successfully!</h2><p>You can now log in to your account.</p></body></html>", "text/html");
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("Email is required.");

            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null) return Ok(new { message = "If the email exists, a verification email has been sent." });

            if (user.IsEmailVerified) return BadRequest("Email is already verified.");

            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            
            var verifyUrl = $"{Request.Scheme}://{Request.Host}/api/account/verify-email?token={Uri.EscapeDataString(emailToken)}&email={Uri.EscapeDataString(user.Email)}";
            var message = new EmailMessage
            {
                To = user.Email,
                Subject = "Verify your email address",
                Body = $"<p>Please click the link below to verify your email address:</p><p><a href=\"{verifyUrl}\">{verifyUrl}</a></p><p>This link will expire in 24 hours.</p>",
                IsHtml = true
            };
            await _emailService.SendAsync(message);

            return Ok(new { message = "Verification email sent." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email))
                return BadRequest("Email is required.");

            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null) return Ok(new { message = "If the email exists, a password reset link has been sent." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            var resetUrl = $"{Request.Scheme}://{Request.Host}/api/account/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";
            var message = new EmailMessage
            {
                To = user.Email,
                Subject = "Reset your password",
                Body = $"<p>Click the link below to reset your password:</p><p><a href=\"{resetUrl}\">{resetUrl}</a></p><p>This link will expire in 1 hour.</p>",
                IsHtml = true
            };
            await _emailService.SendAsync(message);

            return Ok(new { message = "Password reset email sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest("Email, token, and new password are required.");

            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null) return BadRequest("Invalid request.");

            var result = await _userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            // Revoke all refresh tokens for security
            await _refreshTokenService.RevokeAllAsync(user.Id);

            return Ok(new { message = "Password reset successfully." });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest();

            var (success, accessToken, refreshToken, expires) = await _refreshTokenService.RefreshAsync(req.RefreshToken);
            if (!success) return Unauthorized();

            return Ok(new { token = accessToken, refreshToken, refreshTokenExpires = expires });
        }

        [Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] RefreshRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest();

            var revoked = await _refreshTokenService.RevokeAsync(req.RefreshToken);
            if (!revoked) return NotFound();

            return NoContent();
        }

        [Authorize]
        [HttpPost("revoke-all")]
        public async Task<IActionResult> RevokeAll()
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            await _refreshTokenService.RevokeAllAsync(userId);
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new ApplicationUserDto { Id = user.Id, Email = user.Email ?? string.Empty, UserName = user.UserName ?? string.Empty, Roles = roles.ToList() });
        }

        private async Task<object> IssueTokensAsync(ApplicationUser user)
        {
            var roles = (await _userManager.GetRolesAsync(user)).ToList();
            var dto = new ApplicationUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                Roles = roles
            };
            var token = await _tokenService.CreateTokenAsync(dto);
            var (refreshToken, expires) = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);
            return new { token, refreshToken, refreshTokenExpires = expires };
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            userId = default;
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(sub) && Guid.TryParse(sub, out userId);
        }

        private EmailMessage BuildVerificationEmail(string toEmail, string verifyUrl)
        {
            return new EmailMessage
            {
                To = toEmail,
                Subject = "Verify your email address",
                Body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Verify your email</title>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; background-color: #f5f5f5;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <tr>
            <td style='background-color: #ffffff; border-radius: 8px; padding: 40px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                <table role='presentation' width='100%' cellspacing='0' cellpadding='0'>
                    <tr>
                        <td style='text-align: center; padding-bottom: 30px; border-bottom: 1px solid #eaeaea;'>
                            <h1 style='margin: 0; color: #1a1a2e; font-size: 28px; font-weight: 600;'>Welcome to Ecommerce</h1>
                            <p style='margin: 10px 0 0; color: #6b7280; font-size: 16px;'>Verify your email address</p>
                        </td>
                    </tr>
                </table>
                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='margin-top: 30px;'>
                    <tr>
                        <td style='color: #374151; font-size: 16px; line-height: 1.6;'>
                            <p style='margin: 0 0 16px;'>Hi there,</p>
                            <p style='margin: 0 0 16px;'>Thanks for signing up! Please verify your email address to get started.</p>
                            <p style='margin: 0 0 24px;'>Click the button below to verify your email address:</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='text-align: center; padding: 24px 0;'>
                            <a href='{verifyUrl}' style='display: inline-block; background-color: #2563eb; color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-weight: 600; font-size: 16px;'>
                                Verify Email Address
                            </a>
                        </td>
                    </tr>
                    <tr>
                        <td style='color: #6b7280; font-size: 14px; line-height: 1.6; padding-top: 24px; border-top: 1px solid #eaeaea;'>
                            <p style='margin: 0 0 8px;'>If the button doesn't work, copy and paste this link into your browser:</p>
                            <p style='margin: 0; word-break: break-all; color: #2563eb;'><a href='{verifyUrl}' style='color: #2563eb;'>{verifyUrl}</a></p>
                            <p style='margin: 16px 0 0; font-size: 13px; color: #9ca3af;'>This link will expire in 24 hours for security.</p>
                        </td>
                    </tr>
                </table>
                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eaeaea;'>
                    <tr>
                        <td style='color: #9ca3af; font-size: 13px; text-align: center;'>
                            <p style='margin: 0 0 8px;'>If you didn't create an account, you can safely ignore this email.</p>
                            <p style='margin: 0;'>&copy; {DateTime.UtcNow.Year} Ecommerce. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>",
                IsHtml = true
            };
        }

        private EmailMessage BuildResetPasswordEmail(string toEmail, string resetUrl)
        {
            return new EmailMessage
            {
                To = toEmail,
                Subject = "Reset your password",
                Body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset your password</title>
</head>
<body style='margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif; background-color: #f5f5f5;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <tr>
            <td style='background-color: #ffffff; border-radius: 8px; padding: 40px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                <table role='presentation' width='100%' cellspacing='0' cellpadding='0'>
                    <tr>
                        <td style='text-align: center; padding-bottom: 30px; border-bottom: 1px solid #eaeaea;'>
                            <h1 style='margin: 0; color: #1a1a2e; font-size: 28px; font-weight: 600;'>Ecommerce</h1>
                            <p style='margin: 10px 0 0; color: #6b7280; font-size: 16px;'>Reset your password</p>
                        </td>
                    </tr>
                </table>
                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='margin-top: 30px;'>
                    <tr>
                        <td style='color: #374151; font-size: 16px; line-height: 1.6;'>
                            <p style='margin: 0 0 16px;'>Hi there,</p>
                            <p style='margin: 0 0 16px;'>You requested to reset your password. Click the button below to create a new password:</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='text-align: center; padding: 24px 0;'>
                            <a href='{resetUrl}' style='display: inline-block; background-color: #dc2626; color: #ffffff; text-decoration: none; padding: 14px 32px; border-radius: 6px; font-weight: 600; font-size: 16px;'>
                                Reset Password
                            </a>
                        </td>
                    </tr>
                    <tr>
                        <td style='color: #6b7280; font-size: 14px; line-height: 1.6; padding-top: 24px; border-top: 1px solid #eaeaea;'>
                            <p style='margin: 0 0 8px;'>If you didn't request this, you can safely ignore this email.</p>
                            <p style='margin: 0 0 8px;'>If the button doesn't work, copy and paste this link into your browser:</p>
                            <p style='margin: 0; word-break: break-all; color: #dc2626;'><a href='{resetUrl}' style='color: #dc2626;'>{resetUrl}</a></p>
                            <p style='margin: 16px 0 0; font-size: 13px; color: #9ca3af;'>This link will expire in 1 hour for security.</p>
                        </td>
                    </tr>
                </table>
                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eaeaea;'>
                    <tr>
                        <td style='color: #9ca3af; font-size: 13px; text-align: center;'>
                            <p style='margin: 0;'>&copy; {DateTime.UtcNow.Year} Ecommerce. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>",
                IsHtml = true
            };
        }

        private EmailMessage BuildResendVerificationEmail(string toEmail, string verifyUrl)
        {
            return BuildVerificationEmail(toEmail, verifyUrl);
        }

        private EmailMessage BuildForgotPasswordEmail(string toEmail, string resetUrl)
        {
            return BuildResetPasswordEmail(toEmail, resetUrl);
}
}
    public class RegisterRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class RefreshRequest
    {
        public required string RefreshToken { get; set; }
    }

    public class VerifyEmailRequest
    {
        public required string Email { get; set; }
        public required string Token { get; set; }
    }

    public class ResendVerificationRequest
    {
        public required string Email { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public required string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public required string Email { get; set; }
        public required string Token { get; set; }
        public required string NewPassword { get; set; }
    }
}
