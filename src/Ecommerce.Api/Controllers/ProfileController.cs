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
    }
}