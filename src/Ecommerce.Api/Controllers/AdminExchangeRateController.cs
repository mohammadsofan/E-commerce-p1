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
    [Route("api/admin/exchange-rates")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminExchangeRateController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminExchangeRateController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? fromCurrencyId = null,
            [FromQuery] Guid? toCurrencyId = null)
        {
            var query = new GetAdminExchangeRatesQuery
            {
                Page = page,
                PageSize = pageSize,
                FromCurrencyId = fromCurrencyId,
                ToCurrencyId = toCurrencyId
            };
            var result = await _queryDispatcher.Send<GetAdminExchangeRatesQuery, PagedResult<ExchangeRateDto>>(query);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminExchangeRateByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminExchangeRateByIdQuery, ExchangeRateDto>(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExchangeRateCommand command)
        {
            var rate = await _commandDispatcher.Send<CreateExchangeRateCommand, ExchangeRateDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = rate.Id }, rate);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExchangeRateCommand command)
        {
            command.Id = id;
            var rate = await _commandDispatcher.Send<UpdateExchangeRateCommand, ExchangeRateDto>(command);
            return Ok(rate);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteExchangeRateCommand { Id = id };
            await _commandDispatcher.Send<DeleteExchangeRateCommand, Unit>(command);
            return NoContent();
        }
    }
}