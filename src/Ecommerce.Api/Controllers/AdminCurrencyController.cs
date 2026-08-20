using System;
using System.Threading.Tasks;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Commands;
using Ecommerce.Application.Common.Queries;
using Ecommerce.Application.Commands.Admin;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Queries.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/currencies")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminCurrencyController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminCurrencyController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = new GetAdminCurrenciesQuery { Page = page, PageSize = pageSize };
            var result = await _queryDispatcher.Send<GetAdminCurrenciesQuery, PagedResult<CurrencyDto>>(query);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminCurrencyByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminCurrencyByIdQuery, CurrencyDto>(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCurrencyCommand command)
        {
            var currency = await _commandDispatcher.Send<CreateCurrencyCommand, CurrencyDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = currency.Id }, currency);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCurrencyCommand command)
        {
            command.Id = id;
            var currency = await _commandDispatcher.Send<UpdateCurrencyCommand, CurrencyDto>(command);
            return Ok(currency);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteCurrencyCommand { Id = id };
            await _commandDispatcher.Send<DeleteCurrencyCommand, Unit>(command);
            return NoContent();
        }
    }
}