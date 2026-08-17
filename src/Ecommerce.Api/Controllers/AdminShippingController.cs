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
    [Route("api/admin/shipping")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminShippingController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminShippingController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet("zones")]
        public async Task<IActionResult> GetZones(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminShippingZonesQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = search,
                IsActive = isActive
            };
            var result = await _queryDispatcher.Send<GetAdminShippingZonesQuery, PagedResult<AdminShippingZoneDto>>(query);
            return Ok(result);
        }

        [HttpPost("zones")]
        public async Task<IActionResult> CreateZone([FromBody] CreateShippingZoneCommand command)
        {
            var zone = await _commandDispatcher.Send<CreateShippingZoneCommand, AdminShippingZoneDto>(command);
            return CreatedAtAction(nameof(GetZoneById), new { id = zone.Id }, zone);
        }

        [HttpGet("zones/{id:guid}")]
        public async Task<IActionResult> GetZoneById(Guid id)
        {
            var query = new GetAdminShippingZoneByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminShippingZoneByIdQuery, AdminShippingZoneDto>(query);
            return Ok(result);
        }

        [HttpPut("zones/{id:guid}")]
        public async Task<IActionResult> UpdateZone(Guid id, [FromBody] UpdateShippingZoneCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            var zone = await _commandDispatcher.Send<UpdateShippingZoneCommand, AdminShippingZoneDto>(command);
            return Ok(zone);
        }

        [HttpDelete("zones/{id:guid}")]
        public async Task<IActionResult> DeleteZone(Guid id)
        {
            var command = new DeleteShippingZoneCommand { Id = id };
            await _commandDispatcher.Send<DeleteShippingZoneCommand, Unit>(command);
            return NoContent();
        }

        [HttpGet("methods")]
        public async Task<IActionResult> GetMethods(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? zoneId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? search = null)
        {
            var query = new GetAdminShippingMethodsQuery
            {
                Page = page,
                PageSize = pageSize,
                ShippingZoneId = zoneId,
                IsActive = isActive,
                SearchTerm = search
            };
            var result = await _queryDispatcher.Send<GetAdminShippingMethodsQuery, PagedResult<AdminShippingMethodDto>>(query);
            return Ok(result);
        }

        [HttpPost("methods")]
        public async Task<IActionResult> CreateMethod([FromBody] CreateShippingMethodCommand command)
        {
            var method = await _commandDispatcher.Send<CreateShippingMethodCommand, AdminShippingMethodDto>(command);
            return CreatedAtAction(nameof(GetMethodById), new { id = method.Id }, method);
        }

        [HttpGet("methods/{id:guid}")]
        public async Task<IActionResult> GetMethodById(Guid id)
        {
            var query = new GetAdminShippingMethodByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminShippingMethodByIdQuery, AdminShippingMethodDto>(query);
            return Ok(result);
        }

        [HttpPut("methods/{id:guid}")]
        public async Task<IActionResult> UpdateMethod(Guid id, [FromBody] UpdateShippingMethodCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            var method = await _commandDispatcher.Send<UpdateShippingMethodCommand, AdminShippingMethodDto>(command);
            return Ok(method);
        }

        [HttpDelete("methods/{id:guid}")]
        public async Task<IActionResult> DeleteMethod(Guid id)
        {
            var command = new DeleteShippingMethodCommand { Id = id };
            await _commandDispatcher.Send<DeleteShippingMethodCommand, Unit>(command);
            return NoContent();
        }

        [HttpGet("rates")]
        public async Task<IActionResult> GetRates(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? methodId = null)
        {
            var query = new GetAdminShippingRatesQuery
            {
                Page = page,
                PageSize = pageSize,
                ShippingMethodId = methodId
            };
            var result = await _queryDispatcher.Send<GetAdminShippingRatesQuery, PagedResult<AdminShippingRateDto>>(query);
            return Ok(result);
        }

        [HttpPost("rates")]
        public async Task<IActionResult> CreateRate([FromBody] CreateShippingRateOnlyCommand command)
        {
            var rate = await _commandDispatcher.Send<CreateShippingRateOnlyCommand, AdminShippingRateDto>(command);
            return CreatedAtAction(nameof(GetRateById), new { id = rate.Id }, rate);
        }

        [HttpGet("rates/{id:guid}")]
        public async Task<IActionResult> GetRateById(Guid id)
        {
            var query = new GetAdminShippingRateByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminShippingRateByIdQuery, AdminShippingRateDto>(query);
            return Ok(result);
        }

        [HttpPut("rates/{id:guid}")]
        public async Task<IActionResult> UpdateRate(Guid id, [FromBody] UpdateShippingRateOnlyCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            var rate = await _commandDispatcher.Send<UpdateShippingRateOnlyCommand, AdminShippingRateDto>(command);
            return Ok(rate);
        }

        [HttpDelete("rates/{id:guid}")]
        public async Task<IActionResult> DeleteRate(Guid id)
        {
            var command = new DeleteShippingRateCommand { Id = id };
            await _commandDispatcher.Send<DeleteShippingRateCommand, Unit>(command);
            return NoContent();
        }
    }
}