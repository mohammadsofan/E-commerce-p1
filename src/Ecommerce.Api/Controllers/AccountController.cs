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
            return Ok(new { token });
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
            return Ok(new { token });
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
}
