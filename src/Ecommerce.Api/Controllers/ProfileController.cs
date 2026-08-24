using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize(Policy = "AdminOrCustomer")]
    public class ProfileController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public ProfileController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets the current user's profile</summary>
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var result = await _queryDispatcher.Send<GetMyProfileQuery, AdminUserDto>(new GetMyProfileQuery());
            return Ok(result);
        }

        /// <summary>Updates the current user's profile</summary>
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateProfileCommand command)
        {
            var result = await _commandDispatcher.Send<UpdateProfileCommand, AdminUserDto>(command);
            return Ok(result);
        }

        /// <summary>Changes the current user's password</summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var command = new Ecommerce.Application.Commands.Admin.ChangePasswordCommand
            {
                UserId = userId,
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            };

            await _commandDispatcher.Send<Ecommerce.Application.Commands.Admin.ChangePasswordCommand, Ecommerce.Application.Common.Unit>(command);
            return Ok(new { Message = "Password changed successfully." });
        }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
