using System;
using System.Collections.Generic;
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
    [Route("api/admin/coupons")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminCouponController : ControllerBase
    {
        private readonly CommandDispatcher _commandDispatcher;
        private readonly QueryDispatcher _queryDispatcher;

        public AdminCouponController(CommandDispatcher commandDispatcher, QueryDispatcher queryDispatcher)
        {
            _commandDispatcher = commandDispatcher;
            _queryDispatcher = queryDispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? type = null)
        {
            var query = new GetAdminCouponsQuery
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = search,
                IsActive = isActive,
                Type = type
            };
            var result = await _queryDispatcher.Send<GetAdminCouponsQuery, PagedResult<AdminCouponDto>>(query);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var query = new GetAdminCouponByIdQuery { Id = id };
            var result = await _queryDispatcher.Send<GetAdminCouponByIdQuery, AdminCouponDto>(query);
            return Ok(result);
        }

        [HttpGet("by-code/{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var query = new GetAdminCouponByCodeQuery { Code = code };
            var result = await _queryDispatcher.Send<GetAdminCouponByCodeQuery, AdminCouponDto>(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCouponCommand command)
        {
            var coupon = await _commandDispatcher.Send<CreateCouponCommand, AdminCouponDto>(command);
            return CreatedAtAction(nameof(GetById), new { id = coupon.Id }, coupon);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCouponCommand command)
        {
            command.Id = id;
            var coupon = await _commandDispatcher.Send<UpdateCouponCommand, AdminCouponDto>(command);
            return Ok(coupon);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var command = new DeleteCouponCommand { Id = id };
            await _commandDispatcher.Send<DeleteCouponCommand, Unit>(command);
            return NoContent();
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateCouponRequest request)
        {
            var query = new ValidateCouponQuery
            {
                Code = request.Code,
                UserId = request.UserId,
                OrderTotal = request.OrderTotal,
                ProductIds = request.ProductIds,
                CategoryIds = request.CategoryIds
            };
            var result = await _queryDispatcher.Send<ValidateCouponQuery, ValidateCouponResponse>(query);
            return Ok(result);
        }
    }

    public class ValidateCouponRequest
    {
        public string Code { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public decimal OrderTotal { get; set; }
        public List<Guid> ProductIds { get; set; } = new();
        public List<Guid> CategoryIds { get; set; } = new();
    }
}