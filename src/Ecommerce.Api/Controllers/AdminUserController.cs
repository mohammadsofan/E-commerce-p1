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
using CreateUserCmd = Ecommerce.Application.Commands.Admin.CreateUserCommand;
using UpdateUserCmd = Ecommerce.Application.Commands.Admin.UpdateUserCommand;
using DeleteUserCmd = Ecommerce.Application.Commands.Admin.DeleteUserCommand;
using ChangePasswordCmd = Ecommerce.Application.Commands.Admin.ChangePasswordCommand;
using SetUserRolesCmd = Ecommerce.Application.Commands.Admin.SetUserRolesCommand;

namespace Ecommerce.Api.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminUserController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminUserController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        /// <summary>Gets all users (admin view with filtering)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool includeDeleted = false)
        {
            var query = new GetAdminUsersQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                Role = role,
                IsActive = isActive,
                IncludeDeleted = includeDeleted
            };

            var result = await _queryDispatcher.Send<GetAdminUsersQuery, PagedResult<AdminUserDto>>(query);
            return Ok(result);
        }

        /// <summary>Gets a specific user by ID (admin view with all details)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminUserByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminUserByIdQuery, AdminUserDto>(query);
            return Ok(result);
        }

        /// <summary>Creates a new user</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserCmd command)
        {
            var user = await _commandDispatcher.Send<CreateUserCmd, AdminUserDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        /// <summary>Updates an existing user</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCmd command)
        {
            if (id != command.Id)
                return BadRequest("ID mismatch");

            var user = await _commandDispatcher.Send<UpdateUserCmd, AdminUserDto>(command);
            return Ok(user);
        }

        /// <summary>Deletes a user (soft delete by default)</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, [FromQuery] bool hardDelete = false)
        {
            var command = new DeleteUserCmd { Id = id, HardDelete = hardDelete };
            await _commandDispatcher.Send<DeleteUserCmd, Unit>(command);
            return NoContent();
        }

        /// <summary>Changes a user's password</summary>
        [HttpPost("{id:guid}/change-password")]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordCmd command)
        {
            command.UserId = id;
            await _commandDispatcher.Send<ChangePasswordCmd, Unit>(command);
            return NoContent();
        }

        /// <summary>Sets a user's roles</summary>
        [HttpPost("{id:guid}/roles")]
        public async Task<IActionResult> SetRoles(Guid id, [FromBody] SetUserRolesCmd command)
        {
            command.UserId = id;
            await _commandDispatcher.Send<SetUserRolesCmd, Unit>(command);
            return NoContent();
        }
    }
}