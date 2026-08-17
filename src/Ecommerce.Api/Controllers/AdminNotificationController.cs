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
    [Route("api/admin/notifications")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminNotificationController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminNotificationController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? type = null,
            [FromQuery] string? channel = null,
            [FromQuery] string? status = null,
            [FromQuery] Guid? userId = null)
        {
            var query = new GetAdminNotificationsQuery
            {
                Page = page,
                PageSize = pageSize,
                Type = type,
                Channel = channel,
                Status = status,
                UserId = userId
            };
            var result = await _queryDispatcher.Send<GetAdminNotificationsQuery, PagedResult<AdminNotificationDto>>(query);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminNotificationByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminNotificationByIdQuery, AdminNotificationDto>(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationCommand command)
        {
            var notification = await _commandDispatcher.Send<CreateNotificationCommand, AdminNotificationDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = notification.Id }, notification);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNotificationCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            var notification = await _commandDispatcher.Send<UpdateNotificationCommand, AdminNotificationDto>(command);
            return Ok(notification);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteNotificationCommand { Id = id };
            await _commandDispatcher.Send<DeleteNotificationCommand, Unit>(command);
            return NoContent();
        }

        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? channel = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminNotificationTemplatesQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = search,
                Channel = channel,
                IsActive = isActive
            };
            var result = await _queryDispatcher.Send<GetAdminNotificationTemplatesQuery, PagedResult<AdminNotificationTemplateDto>>(query);
            return Ok(result);
        }

        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateNotificationTemplateCommand command)
        {
            var template = await _commandDispatcher.Send<CreateNotificationTemplateCommand, AdminNotificationTemplateDto>(command);
            return CreatedAtAction(nameof(GetTemplateById), new { id = template.Id }, template);
        }

        [HttpGet("templates/{id:guid}")]
        public async Task<IActionResult> GetTemplateById(Guid id)
        {
            var query = new GetAdminNotificationTemplateByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminNotificationTemplateByIdQuery, AdminNotificationTemplateDto>(query);
            return Ok(result);
        }

        [HttpPut("templates/{id:guid}")]
        public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateNotificationTemplateCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            var template = await _commandDispatcher.Send<UpdateNotificationTemplateCommand, AdminNotificationTemplateDto>(command);
            return Ok(template);
        }

        [HttpDelete("templates/{id:guid}")]
        public async Task<IActionResult> DeleteTemplate(Guid id)
        {
            var command = new DeleteNotificationTemplateCommand { Id = id };
            await _commandDispatcher.Send<DeleteNotificationTemplateCommand, Unit>(command);
            return NoContent();
        }

        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? userId = null,
            [FromQuery] string? notificationType = null)
        {
            var query = new GetAdminNotificationPreferencesQuery
            {
                Page = page,
                PageSize = pageSize,
                UserId = userId,
                NotificationType = notificationType
            };
            var result = await _queryDispatcher.Send<GetAdminNotificationPreferencesQuery, PagedResult<AdminNotificationPreferenceDto>>(query);
            return Ok(result);
        }

        [HttpPut("preferences/{id:guid}")]
        public async Task<IActionResult> UpdatePreferences(Guid id, [FromBody] UpdateNotificationPreferenceCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            var preference = await _commandDispatcher.Send<UpdateNotificationPreferenceCommand, AdminNotificationPreferenceDto>(command);
            return Ok(preference);
        }

        [HttpGet("channels")]
        public async Task<IActionResult> GetChannels(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAdminNotificationChannelsQuery
            {
                Page = page,
                PageSize = pageSize,
                IsActive = isActive
            };
            var result = await _queryDispatcher.Send<GetAdminNotificationChannelsQuery, PagedResult<AdminNotificationChannelDto>>(query);
            return Ok(result);
        }

        [HttpPost("channels")]
        public async Task<IActionResult> CreateChannel([FromBody] CreateNotificationChannelCommand command)
        {
            var channel = await _commandDispatcher.Send<CreateNotificationChannelCommand, AdminNotificationChannelDto>(command);
            return CreatedAtAction(nameof(GetChannelById), new { id = channel.Id }, channel);
        }

        [HttpGet("channels/{id:guid}")]
        public async Task<IActionResult> GetChannelById(Guid id)
        {
            var query = new GetAdminNotificationChannelByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminNotificationChannelByIdQuery, AdminNotificationChannelDto>(query);
            return Ok(result);
        }

        [HttpPut("channels/{id:guid}")]
        public async Task<IActionResult> UpdateChannel(Guid id, [FromBody] UpdateNotificationChannelCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatch");
            var channel = await _commandDispatcher.Send<UpdateNotificationChannelCommand, AdminNotificationChannelDto>(command);
            return Ok(channel);
        }

        [HttpDelete("channels/{id:guid}")]
        public async Task<IActionResult> DeleteChannel(Guid id)
        {
            var command = new DeleteNotificationChannelCommand { Id = id };
            await _commandDispatcher.Send<DeleteNotificationChannelCommand, Unit>(command);
            return NoContent();
        }
    }
}