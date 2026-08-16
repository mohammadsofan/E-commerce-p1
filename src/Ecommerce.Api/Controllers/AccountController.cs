using System.Threading.Tasks;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<Ecommerce.Infrastructure.Identity.ApplicationUser> _userManager;
        private readonly SignInManager<Ecommerce.Infrastructure.Identity.ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;

        public AccountController(UserManager<Ecommerce.Infrastructure.Identity.ApplicationUser> userManager,
            SignInManager<Ecommerce.Infrastructure.Identity.ApplicationUser> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var user = new Ecommerce.Infrastructure.Identity.ApplicationUser { UserName = req.Email, Email = req.Email };
            var res = await _userManager.CreateAsync(user, req.Password);
            if (!res.Succeeded) return BadRequest(res.Errors);

            var dto = new ApplicationUserDto { Id = user.Id, Email = user.Email, UserName = user.UserName };
            var token = await _tokenService.CreateTokenAsync(dto);

            // create refresh token
            var refreshService = HttpContext.RequestServices.GetRequiredService<Ecommerce.Application.Interfaces.IRefreshTokenService>();
            var (refreshToken, expires) = await refreshService.CreateRefreshTokenAsync(user.Id);

            return Ok(new { token, refreshToken, refreshTokenExpires = expires });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null) return Unauthorized();

            var res = await _signInManager.CheckPasswordSignInAsync(user, req.Password, false);
            if (!res.Succeeded) return Unauthorized();

            var dto = new ApplicationUserDto { Id = user.Id, Email = user.Email, UserName = user.UserName };
            var token = await _tokenService.CreateTokenAsync(dto);

            var refreshService = HttpContext.RequestServices.GetRequiredService<Ecommerce.Application.Interfaces.IRefreshTokenService>();
            var (refreshToken, expires) = await refreshService.CreateRefreshTokenAsync(user.Id);

            return Ok(new { token, refreshToken, refreshTokenExpires = expires });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest();
            var refreshService = HttpContext.RequestServices.GetRequiredService<Ecommerce.Application.Interfaces.IRefreshTokenService>();
            var (success, accessToken, refreshToken) = await refreshService.RefreshAsync(req.RefreshToken);
            if (!success) return Unauthorized();
            return Ok(new { token = accessToken, refreshToken });
        }

        [Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke([FromBody] RefreshRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest();
            var refreshService = HttpContext.RequestServices.GetRequiredService<Ecommerce.Application.Interfaces.IRefreshTokenService>();
            var revoked = await refreshService.RevokeAsync(req.RefreshToken);
            if (!revoked) return NotFound();
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(sub)) return Unauthorized();

            if (!System.Guid.TryParse(sub, out var userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound();

            var dto = new ApplicationUserDto { Id = user.Id, Email = user.Email, UserName = user.UserName };
            return Ok(dto);
        }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; }
    }
}
