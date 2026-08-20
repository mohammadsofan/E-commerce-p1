using System;
using System.Collections.Generic;
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
    [Route("api/addresses")]
    [Authorize(Policy = "AdminOrCustomer")]
    public class AddressesController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AddressesController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets the current user's addresses</summary>
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var result = await _queryDispatcher.Send<GetMyAddressesQuery, List<AddressDto>>(new GetMyAddressesQuery());
            return Ok(result);
        }

        /// <summary>Gets an address by ID (scoped to the current user)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAddressByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAddressByIdQuery, AddressDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new address for the current user</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAddressCommand command)
        {
            var result = await _commandDispatcher.Send<CreateAddressCommand, AddressDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>Updates an address (scoped to the current user)</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAddressCommand command)
        {
            command.Id = id;
            var result = await _commandDispatcher.Send<UpdateAddressCommand, AddressDto>(command);
            return Ok(result);
        }

        /// <summary>Soft-deletes an address (scoped to the current user)</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteAddressCommand { Id = id };
            await _commandDispatcher.Send<DeleteAddressCommand, Unit>(command);
            return NoContent();
        }
    }
}