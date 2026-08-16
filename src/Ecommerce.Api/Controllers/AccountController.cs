using System;
using System.IdentityModel.Tokens.Jwt;
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

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var user = new ApplicationUser
            {
                UserName = req.Email,
                Email = req.Email,
                FirstName = string.Empty,
                LastName = string.Empty,
                DisplayName = string.Empty,
                ProfileImageUrl = string.Empty
            };
            var res = await _userManager.CreateAsync(user, req.Password);
            if (!res.Succeeded) return BadRequest(res.Errors);

            return Ok(await IssueTokensAsync(user));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null) return Unauthorized();

            var res = await _signInManager.CheckPasswordSignInAsync(user, req.Password, false);
            if (!res.Succeeded) return Unauthorized();

            return Ok(await IssueTokensAsync(user));
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

            return Ok(new ApplicationUserDto { Id = user.Id, Email = user.Email ?? string.Empty, UserName = user.UserName ?? string.Empty });
        }

        private async Task<object> IssueTokensAsync(ApplicationUser user)
        {
            var dto = new ApplicationUserDto { Id = user.Id, Email = user.Email ?? string.Empty, UserName = user.UserName ?? string.Empty };
            var token = await _tokenService.CreateTokenAsync(dto);
            var (refreshToken, expires) = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);
            return new { token, refreshToken, refreshTokenExpires = expires };
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            userId = default;
            var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return !string.IsNullOrEmpty(sub) && Guid.TryParse(sub, out userId);
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
}
