using System.Threading.Tasks;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/settings")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminSettingsController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminSettingsController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var query = new GetStoreSettingsQuery();
            var result = await _queryDispatcher.Send<GetStoreSettingsQuery, StoreSettingsDto>(query);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateStoreSettingsCommand command)
        {
            var result = await _commandDispatcher.Send<UpdateStoreSettingsCommand, StoreSettingsDto>(command);
            return Ok(result);
        }
    }
}
